namespace KtcWeb.Application.DTOs
{
    public class ErrorCodeLookupDto
    {
        public int ErrorCodeTypeId { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorText { get; set; } = string.Empty;
    }
}
