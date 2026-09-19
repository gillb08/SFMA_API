namespace SFMA_API.Services.Infrastructure
{
    public class AppConstants
    {
        public string SchoolName { get; set; } = "St. Faith Model Academy";
        public string SchoolAcronym { get; set; } = "SFMA";
        public string ApiKey { get; set; } = "SFMA-SECURE-API-KEY-2026";
    }

    public class DatabaseConfig
    {
        public string? Server { get; set; }
        public int Port { get; set; } = 5432;
        public string? Database { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public string SslMode { get; set; } = "Require";

        public string BuildConnectionString(string partialConnectionString = "Trust Server Certificate=true")
        {
            if (string.IsNullOrWhiteSpace(Server))
                return partialConnectionString;
            return $"Host={Server};Port={Port};Database={Database};Username={UserId};Password={Password};SSL Mode={SslMode};{partialConnectionString}";
        }
    }
}
