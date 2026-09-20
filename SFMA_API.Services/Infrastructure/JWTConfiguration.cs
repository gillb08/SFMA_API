namespace SFMA_API.Services.Infrastructure
{
    public class JWTConfiguration
    {
        public string Secret { get; set; } = "SFMA_SUPER_SECRET_KEY_FOR_JWT_AUTHENTICATION_2026_CHANGE_IN_PRODUCTION";
        public string Issuer { get; set; } = "SFMA";
        public string Audience { get; set; } = "SFMA_Users";
        public double AccessTokenExpirationMinutes { get; set; } = 120;
    }

    public class JWTToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Issued { get; set; }
        public DateTime? Expires { get; set; }
    }
}
