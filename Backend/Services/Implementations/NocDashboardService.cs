using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using KtcWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KtcWeb.Application.Services
{
    public class NocDashboardService : INocDashboardService
    {
        private readonly KtcDbContext _context;

        public NocDashboardService(KtcDbContext context)
        {
            _context = context;
        }

        public async Task<NocSummaryDto> GetNocSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate   = to   ?? DateTime.UtcNow;

            // ⚠️ EF Core DbContext is NOT thread-safe.
            // Run all queries SEQUENTIALLY — never use Task.WhenAll with the same DbContext.
            var fleet    = await GetFleetHealthAsync();
            var cash     = await GetNetworkCashSummaryAsync();
            var sla      = await GetNetworkSlaAsync(fromDate, toDate);
            var statuses = await GetAtmStatusTableAsync();

            return new NocSummaryDto
            {
                FleetHealth = fleet,
                CashSummary = cash,
                Sla         = sla,
                AtmStatuses = statuses,
                GeneratedAt = DateTime.UtcNow
            };
        }

        // ── Panel 1 : Fleet Health ────────────────────────────────────────────
        private async Task<FleetHealthDto> GetFleetHealthAsync()
        {
            var rows = await _context.Database.SqlQueryRaw<AtmStatusRaw>(@"
                SELECT
                    cs.client_id AS ClientId,
                    UPPER(ISNULL(vl.valuename, '')) AS StatusValue
                FROM dbo.CurrentStatus cs
                INNER JOIN dbo.PropertyList pl ON pl.property_id = cs.property_id
                LEFT  JOIN dbo.ValueList    vl ON vl.value_id    = cs.value_id
                WHERE UPPER(pl.category) = 'DEVICESTATUS'
                   OR UPPER(pl.propertyname) IN ('HEALTH','STATUS','OVERALLSTATUS','DEVICESTATUS')
            ").ToListAsync();

            var byAtm = rows
                .GroupBy(r => r.ClientId)
                .Select(g => new
                {
                    ClientId = g.Key,
                    Status   = WorstStatus(g.Select(x => x.StatusValue))
                })
                .ToList();

            var result = new FleetHealthDto
            {
                TotalAtms    = byAtm.Count,
                OnlineCount  = byAtm.Count(a => a.Status == "ONLINE"),
                OfflineCount = byAtm.Count(a => a.Status == "OFFLINE"),
                WarningCount = byAtm.Count(a => a.Status == "WARNING"),
                UnknownCount = byAtm.Count(a => a.Status == "UNKNOWN")
            };

            // Fallback: if no DeviceStatus properties found, count all active clients
            if (result.TotalAtms == 0)
            {
                result.TotalAtms    = await _context.Clients.AsNoTracking().CountAsync();
                result.UnknownCount = result.TotalAtms;
            }

            return result;
        }

        private static string WorstStatus(IEnumerable<string> values)
        {
            var set = values.ToHashSet();
            if (set.Any(v => v.Contains("FATAL") || v.Contains("ERROR") || v.Contains("OFFLINE") || v.Contains("OUT")))
                return "OFFLINE";
            if (set.Any(v => v.Contains("WARNING") || v.Contains("LOW") || v.Contains("NEAR")))
                return "WARNING";
            if (set.Any(v => v.Contains("ONLINE") || v.Contains("OK") || v.Contains("HEALTHY") || v.Contains("INSERVICE") || v.Contains("ACTIVE")))
                return "ONLINE";
            return "UNKNOWN";
        }

        // ── Panel 3 : Cash Summary ────────────────────────────────────────────
        private async Task<NetworkCashSummaryDto> GetNetworkCashSummaryAsync()
        {
            var cashRows = await _context.Database.SqlQueryRaw<CashRaw>(@"
                SELECT
                    ccu.client_id                  AS ClientId,
                    ISNULL(SUM(ccu.totalvalue), 0) AS TotalValue,
                    MAX(CASE WHEN UPPER(cus.statusname) LIKE '%LOW%'   THEN 1 ELSE 0 END) AS HasLow,
                    MAX(CASE WHEN UPPER(cus.statusname) LIKE '%EMPTY%' THEN 1 ELSE 0 END) AS HasEmpty
                FROM dbo.CurrentCashUnitStatus ccu
                INNER JOIN dbo.CashUnitStatuses cus ON cus.cashunitstatus_id = ccu.status_id
                GROUP BY ccu.client_id
            ").ToListAsync();

            return new NetworkCashSummaryDto
            {
                TotalCashAvailable = cashRows.Sum(r => r.TotalValue),
                AtmsLowCash        = cashRows.Count(r => r.HasLow  == 1),
                AtmsEmptyCash      = cashRows.Count(r => r.HasEmpty == 1),
                TotalAtmsMonitored = cashRows.Count
            };
        }

        // ── Panel 4 : SLA Availability ────────────────────────────────────────
        private async Task<NetworkSlaDto> GetNetworkSlaAsync(DateTime from, DateTime to)
        {
            // Detect which table and timestamp column exist
            var hasP = await TableExistsAsync("OverallAvailability_P");
            var hasS = await TableExistsAsync("OverallAvailability_S");
            var table = hasP ? "OverallAvailability_P" : (hasS ? "OverallAvailability_S" : null);

            if (table == null)
                return new NetworkSlaDto { From = from, To = to };

            var hasTimestmp      = await ColumnExistsAsync(table, "timestmp");
            var hasTimestmpLocal = await ColumnExistsAsync(table, "timestmp_local");
            var tsCol = hasTimestmp ? "timestmp" : (hasTimestmpLocal ? "timestmp_local" : "timestmp");

            // Confirmed real columns: sec_available / sec_unavailable
            var sqlFiltered = $@"
                SELECT
                    ISNULL(SUM(CAST(sec_available   AS bigint)), 0) AS SecAvailable,
                    ISNULL(SUM(CAST(sec_unavailable AS bigint)), 0) AS SecUnavailable
                FROM dbo.{table}
                WHERE {tsCol} >= {{0}} AND {tsCol} <= {{1}}";

            var row = await _context.Database.SqlQueryRaw<SlaRaw2>(sqlFiltered, from, to).FirstOrDefaultAsync();

            // Fallback: date range has no data (e.g. frontend asks for 2026 but data is 2025)
            // → query ALL data and auto-detect the actual period
            if (row == null || (row.SecAvailable + row.SecUnavailable) == 0)
            {
                var sqlAll = $@"
                    SELECT
                        ISNULL(SUM(CAST(sec_available   AS bigint)), 0) AS SecAvailable,
                        ISNULL(SUM(CAST(sec_unavailable AS bigint)), 0) AS SecUnavailable
                    FROM dbo.{table}";

                row = await _context.Database.SqlQueryRaw<SlaRaw2>(sqlAll).FirstOrDefaultAsync();

                // Detect the actual date range stored in the table
#pragma warning disable EF1002 // column/table names derived from internal DB checks — not user input
                var rangeRow = await _context.Database.SqlQueryRaw<DateRangeRaw>(
                    $"SELECT MIN({tsCol}) AS MinDate, MAX({tsCol}) AS MaxDate FROM dbo.{table}"
                ).FirstOrDefaultAsync();
#pragma warning restore EF1002

                if (rangeRow?.MinDate != null)
                {
                    from = rangeRow.MinDate.Value;
                    to   = rangeRow.MaxDate ?? DateTime.UtcNow;
                }
            }

            if (row == null)
                return new NetworkSlaDto { From = from, To = to };

            var uptime = row.SecAvailable;
            var down   = row.SecUnavailable;
            var total  = uptime + down;

            return new NetworkSlaDto
            {
                UptimeSeconds       = (int)Math.Min(uptime, int.MaxValue),
                DowntimeSeconds     = (int)Math.Min(down,   int.MaxValue),
                TotalSeconds        = (int)Math.Min(total,  int.MaxValue),
                AvailabilityPercent = total == 0 ? 0 : Math.Round((decimal)uptime * 100m / total, 2),
                From                = from,
                To                  = to
            };
        }

        // ── Schema helpers (mirrors AtmRepository) ────────────────────────────
        private async Task<bool> TableExistsAsync(string tableName)
        {
            var result = await _context.Database.SqlQueryRaw<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = {0}
                ) THEN 1 ELSE 0 END AS [Value]", tableName).FirstOrDefaultAsync();
            return result == 1;
        }

        private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var result = await _context.Database.SqlQueryRaw<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = {0} AND COLUMN_NAME = {1}
                ) THEN 1 ELSE 0 END AS [Value]", tableName, columnName).FirstOrDefaultAsync();
            return result == 1;
        }

        // ── Live ATM Status Table ─────────────────────────────────────────────
        private async Task<List<AtmStatusRowDto>> GetAtmStatusTableAsync()
        {
            return await _context.Database.SqlQueryRaw<AtmStatusRowDto>(@"
                SELECT
                    c.client_id               AS ClientId,
                    c.clientname              AS ClientName,
                    c.networkaddress          AS NetworkAddress,
                    CAST(c.active AS bit)     AS Active,
                    b.branchname              AS BranchName,
                    ISNULL(
                        (SELECT TOP 1 UPPER(ISNULL(vl.valuename,'UNKNOWN'))
                         FROM dbo.CurrentStatus cs2
                         INNER JOIN dbo.PropertyList pl2 ON pl2.property_id = cs2.property_id
                         LEFT  JOIN dbo.ValueList    vl  ON vl.value_id     = cs2.value_id
                         WHERE cs2.client_id = c.client_id
                           AND (UPPER(pl2.category) = 'DEVICESTATUS'
                             OR UPPER(pl2.propertyname) IN ('HEALTH','STATUS','OVERALLSTATUS'))
                        ), 'UNKNOWN') AS Status,
                    ISNULL(
                        (SELECT TOP 1 ISNULL(vl.valuename,'Unknown')
                         FROM dbo.CurrentStatus cs2
                         INNER JOIN dbo.PropertyList pl2 ON pl2.property_id = cs2.property_id
                         LEFT  JOIN dbo.ValueList    vl  ON vl.value_id     = cs2.value_id
                         WHERE cs2.client_id = c.client_id
                           AND (UPPER(pl2.category) = 'DEVICESTATUS'
                             OR UPPER(pl2.propertyname) IN ('HEALTH','STATUS','OVERALLSTATUS'))
                        ), 'Unknown') AS StatusLabel
                FROM dbo.Clients c
                LEFT JOIN dbo.Branches b ON b.branch_id = c.branch_id
                WHERE c.active = 1
                ORDER BY c.clientname
            ").ToListAsync();
        }

        // ── Private projection classes ────────────────────────────────────────
        private class AtmStatusRaw
        {
            public int    ClientId    { get; set; }
            public string StatusValue { get; set; } = string.Empty;
        }

        private class CashRaw
        {
            public int     ClientId   { get; set; }
            public decimal TotalValue { get; set; }
            public int     HasLow     { get; set; }
            public int     HasEmpty   { get; set; }
        }

        private class SlaRaw2
        {
            // Confirmed real columns from dbo.OverallAvailability_P
            public long SecAvailable   { get; set; }
            public long SecUnavailable { get; set; }
        }

        private class DateRangeRaw
        {
            public DateTime? MinDate { get; set; }
            public DateTime? MaxDate { get; set; }
        }
    }
}
