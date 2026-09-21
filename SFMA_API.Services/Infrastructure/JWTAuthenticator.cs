using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SFMA_API.Models.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SFMA_API.Services.Infrastructure
{
    public interface IJWTAuthenticator
    {
        Task<JWTToken> GenerateJwtToken(ApplicationUser user, string? expires = null, List<Claim>? additionalClaims = null);
        Task<string> GenerateRefreshToken(ApplicationUser user);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public class JWTAuthenticator : IJWTAuthenticator
    {
        private readonly JWTConfiguration _jwtConfiguration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public JWTAuthenticator(IConfiguration configuration, JWTConfiguration jwtConfiguration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _jwtConfiguration = jwtConfiguration;
            _userManager = userManager;
        }

        public async Task<JWTToken> GenerateJwtToken(ApplicationUser user, string? expires = null, List<Claim>? additionalClaims = null)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfiguration.Secret);

            var claims = new List<Claim>
            {
                new Claim("Id", user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = string.IsNullOrWhiteSpace(expires)
                    ? DateTime.UtcNow.AddMinutes(_jwtConfiguration.AccessTokenExpirationMinutes)
                    : DateTime.UtcNow.AddMinutes(double.Parse(expires)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtConfiguration.Issuer,
                Audience = _jwtConfiguration.Audience
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return new JWTToken
            {
                Token = jwtToken,
                Issued = DateTime.UtcNow,
                Expires = tokenDescriptor.Expires
            };
        }

        public async Task<string> GenerateRefreshToken(ApplicationUser user)
        {
            var tokenStoreProvider = _configuration["SchoolSettings:TokenProviderName"] ?? "SFMA";
            await _userManager.RemoveAuthenticationTokenAsync(user, tokenStoreProvider, "RefreshToken");
            string? newRefreshToken = await _userManager.GenerateUserTokenAsync(user, TokenProviders.RefreshTokenProvider, "RefreshToken");
            await _userManager.SetAuthenticationTokenAsync(user, tokenStoreProvider, "RefreshToken", newRefreshToken ?? Guid.NewGuid().ToString());
            return newRefreshToken ?? Guid.NewGuid().ToString();
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes(_jwtConfiguration.Secret);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
