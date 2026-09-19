using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFMA_API.Logger
{
    public static class LogEnricher
    {
        public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown");

            string? resource = httpContext.GetMetricsCurrentResourceName();
            string? userId = httpContext.User?.GetUserId();
            string? username = httpContext.User?.GetUsername();
            IEnumerable<string> roles = httpContext.User?.GetRoles() ?? Array.Empty<string>();

            if (!string.IsNullOrWhiteSpace(resource))
            {
                diagnosticContext.Set("Resource", resource);
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                diagnosticContext.Set("UserId", userId);
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                diagnosticContext.Set("Username", username);
            }

            if (roles.Any())
            {
                diagnosticContext.Set("Roles", roles);
            }
        }
    }
}
