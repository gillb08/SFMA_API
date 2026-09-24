using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(
            HttpClient httpClient,
            IOptions<EmailOptions> emailOptions,
            ILogger<ResendEmailSender> logger)
        {
            _httpClient = httpClient;
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.ToEmail))
            {
                _logger.LogWarning("Email sending aborted: Recipient email address is empty.");
                return false;
            }

            string apiKey = _emailOptions.Resend?.ApiKey ?? string.Empty;

            // In development or if API key is not yet set, log email contents for debugging
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("<") || apiKey == "re_test_placeholder")
            {
                _logger.LogInformation(
                    "[Resend Mock Sender] Outgoing Email -> To: {ToEmail} | Subject: {Subject}\n[Content Preview]:\n{TextBody}",
                    message.ToEmail, message.Subject, message.TextBody);
                return true;
            }

            try
            {
                string fromAddress = !string.IsNullOrWhiteSpace(_emailOptions.FromName)
                    ? $"{_emailOptions.FromName} <{_emailOptions.FromEmail}>"
                    : _emailOptions.FromEmail;

                var payload = new Dictionary<string, object>
                {
                    { "from", fromAddress },
                    { "to", new[] { message.ToEmail.Trim() } },
                    { "subject", message.Subject },
                    { "html", message.HtmlBody },
                    { "text", message.TextBody }
                };

                if (!string.IsNullOrWhiteSpace(message.ReplyTo) || !string.IsNullOrWhiteSpace(_emailOptions.ReplyToEmail))
                {
                    payload["reply_to"] = message.ReplyTo ?? _emailOptions.ReplyToEmail;
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
                {
                    Content = jsonContent
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Resend API Error (Status {StatusCode}) while sending to {ToEmail}: {ErrorBody}",
                        response.StatusCode, message.ToEmail, errorBody);
                    return false;
                }

                _logger.LogInformation("Email successfully dispatched via Resend to {ToEmail} with Subject: {Subject}", message.ToEmail, message.Subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch email to {ToEmail} via Resend REST API", message.ToEmail);
                return false;
            }
        }
    }
}
