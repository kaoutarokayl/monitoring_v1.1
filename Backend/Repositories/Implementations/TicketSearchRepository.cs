using System.Text;
using KtcWeb.Application.DTOs;
using KtcWeb.Domain.Interfaces;
using KtcWeb.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KtcWeb.Infrastructure.Repositories
{
    public class TicketSearchRepository : ITicketSearchRepository
    {
        private readonly string _connectionString;

        public TicketSearchRepository(KtcDbContext context, IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("KtcDb")!;
        }

        // ── Lookups ───────────────────────────────────────────────────────────

        public async Task<List<TicketTypeLookupDto>> GetTicketTypesAsync()
        {
            var list = new List<TicketTypeLookupDto>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT tickettype_id AS TicketTypeId, typename AS TypeName
                FROM dbo.TroubleTicketTypes
                ORDER BY typename";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TicketTypeLookupDto
                {
                    TicketTypeId = reader.GetInt32(reader.GetOrdinal("TicketTypeId")),
                    TypeName     = reader.GetString(reader.GetOrdinal("TypeName"))
                });
            }

            return list;
        }

        public async Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync()
        {
            var list = new List<ErrorCodeLookupDto>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT errorcodetype_id AS ErrorCodeTypeId,
                       errorcode        AS ErrorCode,
                       errortext        AS ErrorText
                FROM dbo.ErrorCodeTypes
                ORDER BY errorcode";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ErrorCodeLookupDto
                {
                    ErrorCodeTypeId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ErrorCodeTypeId"))),
                    ErrorCode       = reader.IsDBNull(reader.GetOrdinal("ErrorCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ErrorCode")),
                    ErrorText       = reader.IsDBNull(reader.GetOrdinal("ErrorText")) ? string.Empty : reader.GetString(reader.GetOrdinal("ErrorText"))
                });
            }

            return list;
        }

        // ── Search ────────────────────────────────────────────────────────────

        public async Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria)
        {
            var sql = new StringBuilder(@"
                SELECT TOP 500
                    tt.TroubleTicket_id                              AS TicketId,
                    ISNULL(ttt.typename,    'N/A')                   AS TicketType,
                    CASE tt.ticketstatus_id
                        WHEN 1 THEN 'Open'
                        WHEN 2 THEN 'Dispatched'
                        WHEN 3 THEN 'Closed'
                        WHEN 4 THEN 'AutoClosed'
                        WHEN 5 THEN 'Suspended'
                        WHEN 6 THEN 'Planned'
                        WHEN 7 THEN 'Cancelled'
                        ELSE 'Unknown'
                    END                                              AS Status,
                    ISNULL(c.clientname,    '')                      AS AtmName,
                    ISNULL(b.businessname,  '')                      AS BusinessName,
                    ISNULL(br.branchname,   '')                      AS BranchName,
                    ISNULL(g.groupname,     '')                      AS GroupName,
                    ISNULL(dl.dispatchname, '')                      AS DispatchedTo,
                    ISNULL(u.username,      '')                      AS Owner,
                    ISNULL(ect.errorcode,   '')                      AS ErrorCode,
                    ISNULL(ect.errortext,   '')                      AS ErrorText,
                    tt.creationtime                                  AS Created,
                    ISNULL(tt.updatetime, tt.creationtime)           AS LastChangeDate,
                    tt.closedtime                                    AS ClosedDate,
                    CONCAT(
                        CAST(DATEDIFF(HOUR, tt.creationtime,
                             ISNULL(tt.closedtime, GETDATE())) AS varchar(10)),
                        'h')                                         AS Duration,
                    CASE
                        WHEN EXISTS (
                            SELECT 1 FROM dbo.TicketSLAs sla
                            WHERE sla.TroubleTicket_id = tt.TroubleTicket_id
                              AND sla.end_time IS NULL
                              AND sla.expected_end_time < GETDATE())
                        THEN 'Breached'
                        WHEN EXISTS (
                            SELECT 1 FROM dbo.TicketSLAs sla
                            WHERE sla.TroubleTicket_id = tt.TroubleTicket_id
                              AND sla.end_time IS NULL)
                        THEN 'Open SLA'
                        ELSE 'OK'
                    END                                              AS SlaSummary
                FROM dbo.TroubleTickets tt
                LEFT JOIN dbo.TroubleTicketTypes ttt ON ttt.tickettype_id    = tt.tickettype_id
                LEFT JOIN dbo.ErrorCodeTypes    ect  ON ect.errorcodetype_id = ttt.errorcodetype_id
                LEFT JOIN dbo.KTCUsers          u    ON u.user_id            = tt.owner_id
                LEFT JOIN dbo.DispatchList      dl   ON dl.dispatch_id       = tt.dispatch_id
                LEFT JOIN dbo.Clients           c    ON c.client_id          = tt.client_id
                LEFT JOIN dbo.Branches          br   ON br.branch_id         = c.branch_id
                LEFT JOIN dbo.Businesses        b    ON b.business_id        = c.business_id
                LEFT JOIN dbo.Groups            g    ON g.group_id           = tt.group_id
                WHERE tt.TroubleTicket_id > 0
            ");

            var parameters = new List<object>();
            var index = 0;

            void AddClause(string clause, object value)
            {
                sql.Append(" AND ").Append(clause);
                parameters.Add(value);
                index++;
            }

            // ── Filtre ID direct (prioritaire) ────────────────────────────────
            if (criteria.TicketId.HasValue)
            {
                AddClause($"tt.TroubleTicket_id = {{{index}}}", criteria.TicketId.Value);
            }
            else
            {
                // ── Filtre statut ─────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.TicketStatus) && criteria.TicketStatus != "All")
                {
                    if (criteria.TicketStatus == "Open/Dispatched")
                        sql.Append(" AND tt.ticketstatus_id IN (1, 2)");
                    else if (criteria.TicketStatus == "Closed")
                        sql.Append(" AND tt.ticketstatus_id IN (3, 4)");
                }

                // ── Filtre groupe ─────────────────────────────────────────────
                if (criteria.GroupId.HasValue)
                    AddClause($"tt.group_id = {{{index}}}", criteria.GroupId.Value);

                // ── Filtre business ───────────────────────────────────────────
                if (criteria.BusinessId.HasValue)
                    AddClause($"c.business_id = {{{index}}}", criteria.BusinessId.Value);

                // ── Filtre branche ────────────────────────────────────────────
                if (criteria.BranchId.HasValue)
                    AddClause($"c.branch_id = {{{index}}}", criteria.BranchId.Value);

                // ── Filtre nom ATM ────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.AtmName))
                    AddClause($"LOWER(ISNULL(c.clientname, '')) LIKE LOWER({{{index}}})", $"%{criteria.AtmName}%");

                // ── Filtre type de ticket ─────────────────────────────────────
                if (criteria.TicketTypeId.HasValue)
                    AddClause($"tt.tickettype_id = {{{index}}}", criteria.TicketTypeId.Value);

                // ── Filtre code erreur ────────────────────────────────────────
                if (criteria.ErrorCodeTypeId.HasValue)
                    AddClause($"ect.errorcodetype_id = {{{index}}}", criteria.ErrorCodeTypeId.Value);

                // ── Filtre owner ──────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.Owner))
                    AddClause($"LOWER(ISNULL(u.username, '')) LIKE LOWER({{{index}}})", $"%{criteria.Owner}%");

                // ── Filtre dispatché à ────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.DispatchedTo))
                    AddClause($"LOWER(ISNULL(dl.dispatchname, '')) LIKE LOWER({{{index}}})", $"%{criteria.DispatchedTo}%");

                // ── Filtre dates ──────────────────────────────────────────────
                if (criteria.CreatedAfter.HasValue)
                    AddClause($"tt.creationtime >= {{{index}}}", criteria.CreatedAfter.Value);

                if (criteria.CreatedBefore.HasValue)
                    AddClause($"tt.creationtime <= {{{index}}}", criteria.CreatedBefore.Value);

                // ── Filtre SLA ────────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.SlaStatus) && criteria.SlaStatus != "No Filter")
                {
                    switch (criteria.SlaStatus)
                    {
                        case "No Ticket SLAs":
                            sql.Append(" AND NOT EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id)");
                            break;
                        case "Has any open SLAs":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL)");
                            break;
                        case "Has any due in <X hours":
                            if (criteria.SlaHours.HasValue)
                                AddClause($"EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL AND sla.expected_end_time <= DATEADD(hour, {{{index}}}, GETDATE()))", criteria.SlaHours.Value);
                            break;
                        case "Has open exceeded SLAs":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL AND sla.expected_end_time < GETDATE())");
                            break;
                        case "All SLAs are closed":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id) AND NOT EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL)");
                            break;
                    }
                }

                // ── Filtre extra data ─────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(criteria.ExtraDataField) && !string.IsNullOrWhiteSpace(criteria.ExtraDataValue))
                    AddClause($"CONVERT(nvarchar(max), tt.extra_data) LIKE {{{index}}}", $"%<{criteria.ExtraDataField}>{criteria.ExtraDataValue}</{criteria.ExtraDataField}>%");
            }

            sql.Append(" ORDER BY tt.creationtime DESC");

            // ── Exécution ─────────────────────────────────────────────────────
            var results = new List<TicketSearchResultDto>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();

            // Convertir les placeholders {0},{1}... en @p0,@p1,...
            var finalSql = sql.ToString();
            for (int i = 0; i < parameters.Count; i++)
            {
                finalSql = finalSql.Replace($"{{{i}}}", $"@p{i}");
                var param = cmd.CreateParameter();
                param.ParameterName = $"@p{i}";
                param.Value = parameters[i];
                cmd.Parameters.Add(param);
            }

            cmd.CommandText    = finalSql;
            cmd.CommandTimeout = 60;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new TicketSearchResultDto
                {
                    TicketId       = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("TicketId"))),
                    TicketType     = reader.IsDBNull(reader.GetOrdinal("TicketType"))     ? "" : reader.GetString(reader.GetOrdinal("TicketType")),
                    Status         = reader.IsDBNull(reader.GetOrdinal("Status"))         ? "" : reader.GetString(reader.GetOrdinal("Status")),
                    AtmName        = reader.IsDBNull(reader.GetOrdinal("AtmName"))        ? "" : reader.GetString(reader.GetOrdinal("AtmName")),
                    BusinessName   = reader.IsDBNull(reader.GetOrdinal("BusinessName"))   ? "" : reader.GetString(reader.GetOrdinal("BusinessName")),
                    BranchName     = reader.IsDBNull(reader.GetOrdinal("BranchName"))     ? "" : reader.GetString(reader.GetOrdinal("BranchName")),
                    GroupName      = reader.IsDBNull(reader.GetOrdinal("GroupName"))      ? "" : reader.GetString(reader.GetOrdinal("GroupName")),
                    DispatchedTo   = reader.IsDBNull(reader.GetOrdinal("DispatchedTo"))   ? "" : reader.GetString(reader.GetOrdinal("DispatchedTo")),
                    Owner          = reader.IsDBNull(reader.GetOrdinal("Owner"))          ? "" : reader.GetString(reader.GetOrdinal("Owner")),
                    ErrorCode      = reader.IsDBNull(reader.GetOrdinal("ErrorCode"))      ? "" : reader.GetString(reader.GetOrdinal("ErrorCode")),
                    ErrorText      = reader.IsDBNull(reader.GetOrdinal("ErrorText"))      ? "" : reader.GetString(reader.GetOrdinal("ErrorText")),
                    Created        = reader.GetDateTime(reader.GetOrdinal("Created")),
                    LastChangeDate = reader.GetDateTime(reader.GetOrdinal("LastChangeDate")),
                    ClosedDate     = reader.IsDBNull(reader.GetOrdinal("ClosedDate"))     ? null : reader.GetDateTime(reader.GetOrdinal("ClosedDate")),
                    Duration       = reader.IsDBNull(reader.GetOrdinal("Duration"))       ? "" : reader.GetString(reader.GetOrdinal("Duration")),
                    SlaSummary     = reader.IsDBNull(reader.GetOrdinal("SlaSummary"))     ? "" : reader.GetString(reader.GetOrdinal("SlaSummary")),
                });
            }

            return results;
        }
    }
}