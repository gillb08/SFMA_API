using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFMA_API.Services.Infrastructure;

namespace SFMA_API.Services.Extensions
{
    public static class ConfigurationBinderExtension
    {
        public static IServiceCollection BindConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            var appConstants = new AppConstants();
            configuration.GetSection("AppConstants").Bind(appConstants);
            services.AddSingleton(appConstants);

            var jwtConfig = new JWTConfiguration();
            configuration.GetSection("JwtConfig").Bind(jwtConfig);
            services.AddSingleton(jwtConfig);

            return services;
        }
    }
}
