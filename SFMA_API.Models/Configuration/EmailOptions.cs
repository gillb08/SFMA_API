namespace SFMA_API.Models.Configuration
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string FromEmail { get; set; } = "admissions@stfaithacademy.edu.ng";
        public string FromName { get; set; } = "St. Faith Model Academy";
        public string ReplyToEmail { get; set; } = "support@stfaithacademy.edu.ng";
        public string PortalLoginUrl { get; set; } = "https://stfaithacademy.edu.ng/login";
        public ResendOptions Resend { get; set; } = new();
    }

    public class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.resend.com";
    }
}
