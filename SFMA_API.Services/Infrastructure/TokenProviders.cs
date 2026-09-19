using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFMA_API.Services.Infrastructure;
using System;

namespace SFMA_API.Services.Infrastructure
{
    public class TokenProviders
    {
        public static string ImpersonationTokenProvider = "ImpersonationTokenProvider";
        public static string RefreshTokenProvider = "RefreshTokenProvider";
    }

    public class ImpersonationTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
        where TUser : class
    {
        public ImpersonationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<ImpersonationTokenProviderOptions> options,
            ILogger<ImpersonationTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class ImpersonationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public ImpersonationTokenProviderOptions()
        {
            Name = "ImpersonationTokenProvider";
            TokenLifespan = TimeSpan.FromMinutes(10);
        }
    }

    public class RefreshTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
        where TUser : class
    {
        public RefreshTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<RefreshTokenProviderOptions> options,
            ILogger<RefreshTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class RefreshTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public RefreshTokenProviderOptions()
        {
            Name = "RefreshTokenProvider";
            TokenLifespan = TimeSpan.FromDays(30);
        }
    }
}

namespace SFMA_API.Services.Extensions
{
    public static class CustomIdentityBuilderExtensions
    {
        public static IdentityBuilder AddImpersonationTokenProvider(this IdentityBuilder builder)
        {
            Type userType = builder.UserType;
            Type provider = typeof(ImpersonationTokenProvider<>).MakeGenericType(userType);
            return builder.AddTokenProvider(TokenProviders.ImpersonationTokenProvider, provider);
        }

        public static IdentityBuilder AddRefreshTokenProvider(this IdentityBuilder builder)
        {
            Type userType = builder.UserType;
            Type provider = typeof(RefreshTokenProvider<>).MakeGenericType(userType);
            return builder.AddTokenProvider(TokenProviders.RefreshTokenProvider, provider);
        }
    }
}
