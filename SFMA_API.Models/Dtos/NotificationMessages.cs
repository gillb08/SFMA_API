namespace SFMA_API.Models.Dtos
{
    public class EmailMessage
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string TextBody { get; set; } = string.Empty;
        public string? ReplyTo { get; set; }
    }

    public class SmsMessage
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
