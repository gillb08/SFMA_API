namespace SFMA_API.Models.Configuration
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string FromEmail { get; set; } = "admissions@stfaithmodelacademy.com";
        public string FromName { get; set; } = "St. Faith Model Academy";
        public string ReplyToEmail { get; set; } = "support@stfaithmodelacademy.com";
        public string AppBaseUrl { get; set; } = "https://www.stfaithmodelacademy.com";
        public string PortalLoginUrl { get; set; } = "https://www.stfaithmodelacademy.com/login";
        public string VerificationUrl { get; set; } = "https://www.stfaithmodelacademy.com/verify-email";
        public ResendOptions Resend { get; set; } = new();
    }

    public class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.resend.com";
    }
}
