namespace KtcWeb.Application.DTOs
{
    public class TicketSearchCriteriaDto
    {
        public int? GroupId { get; set; }
        public int? BusinessId { get; set; }
        public int? BranchId { get; set; }
        public string? AtmName { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int? TicketTypeId { get; set; }
        public int? ErrorCodeTypeId { get; set; }
        public string? Owner { get; set; }
        public string? TicketStatus { get; set; }
        public string? DispatchedTo { get; set; }
        public string? SlaStatus { get; set; }
        public int? SlaHours { get; set; }
        public string? ExtraDataField { get; set; }
        public string? ExtraDataValue { get; set; }
        public int? TicketId { get; set; }
    }
}
