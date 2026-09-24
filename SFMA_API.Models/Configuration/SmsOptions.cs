namespace SFMA_API.Models.Configuration
{
    public class SmsOptions
    {
        public const string SectionName = "Sms";

        public string Provider { get; set; } = "Termii";
        public string SenderId { get; set; } = "StFaithMA";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.ng.termii.com";
    }
}
