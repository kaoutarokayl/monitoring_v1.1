namespace KtcWeb.Application.DTOs
{
    public class UploadFileResultDto
    {
        public long ActionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileLocation { get; set; } = string.Empty;
        public byte FileType { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
