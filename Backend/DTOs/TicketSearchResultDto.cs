namespace KtcWeb.Application.DTOs
{
    public class TicketSearchResultDto
    {
        public int TicketId { get; set; }
        public string TicketType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AtmName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string DispatchedTo { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorText { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime LastChangeDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string SlaSummary { get; set; } = string.Empty;
    }
}
