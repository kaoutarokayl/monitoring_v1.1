namespace KtcWeb.Application.DTOs
{
    /// <summary>
    /// Global fleet health KPIs (real-time)
    /// </summary>
    public class FleetHealthDto
    {
        public int TotalAtms       { get; set; }
        public int OnlineCount     { get; set; }
        public int OfflineCount    { get; set; }
        public int WarningCount    { get; set; }
        public int UnknownCount    { get; set; }
        public double OnlinePercent  => TotalAtms == 0 ? 0 : Math.Round((double)OnlineCount  / TotalAtms * 100, 1);
        public double OfflinePercent => TotalAtms == 0 ? 0 : Math.Round((double)OfflineCount / TotalAtms * 100, 1);
        public double WarningPercent => TotalAtms == 0 ? 0 : Math.Round((double)WarningCount / TotalAtms * 100, 1);
    }

    /// <summary>
    /// Network-wide cash summary (real-time)
    /// </summary>
    public class NetworkCashSummaryDto
    {
        public decimal TotalCashAvailable  { get; set; }
        public int     AtmsLowCash         { get; set; }
        public int     AtmsEmptyCash       { get; set; }
        public int     TotalAtmsMonitored  { get; set; }
    }

    /// <summary>
    /// Network-wide SLA availability summary
    /// </summary>
    public class NetworkSlaDto
    {
        public decimal  AvailabilityPercent  { get; set; }
        public int      TotalSeconds         { get; set; }
        public int      UptimeSeconds        { get; set; }
        public int      DowntimeSeconds      { get; set; }
        public DateTime From                 { get; set; }
        public DateTime To                   { get; set; }
    }

    /// <summary>
    /// One ATM status row for the live fleet table
    /// </summary>
    public class AtmStatusRowDto
    {
        public int    ClientId      { get; set; }
        public string ClientName    { get; set; } = string.Empty;
        public string Status        { get; set; } = string.Empty;   // Online | Offline | Warning | Unknown
        public string StatusLabel   { get; set; } = string.Empty;
        public string NetworkAddress{ get; set; } = string.Empty;
        public string? BranchName   { get; set; }
        public bool   Active        { get; set; }
    }

    /// <summary>
    /// Full NOC summary returned by /api/noc/summary
    /// </summary>
    public class NocSummaryDto
    {
        public FleetHealthDto      FleetHealth  { get; set; } = new();
        public NetworkCashSummaryDto CashSummary { get; set; } = new();
        public NetworkSlaDto       Sla          { get; set; } = new();
        public List<AtmStatusRowDto> AtmStatuses { get; set; } = new();
        public DateTime            GeneratedAt  { get; set; } = DateTime.UtcNow;
    }
}
