using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class SmsSender : ISmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly SmsOptions _smsOptions;
        private readonly ILogger<SmsSender> _logger;

        public SmsSender(
            HttpClient httpClient,
            IOptions<SmsOptions> smsOptions,
            ILogger<SmsSender> logger)
        {
            _httpClient = httpClient;
            _smsOptions = smsOptions.Value;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(SmsMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.PhoneNumber))
            {
                _logger.LogWarning("SMS dispatch skipped: Phone number is empty.");
                return false;
            }

            string cleanPhone = NormalizeNigerianPhoneNumber(message.PhoneNumber);
            string apiKey = _smsOptions.ApiKey ?? string.Empty;

            // In development or if API key is not configured, log SMS message
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("<") || apiKey == "termii_test_key")
            {
                _logger.LogInformation(
                    "[SMS Gateway (Mock)] Outgoing SMS -> To: {Phone} | Sender: {SenderId}\nMessage:\n{Message}",
                    cleanPhone, _smsOptions.SenderId, message.Message);
                return true;
            }

            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "to", cleanPhone },
                    { "from", _smsOptions.SenderId },
                    { "sms", message.Message },
                    { "type", "plain" },
                    { "channel", "generic" },
                    { "api_key", apiKey }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/sms/send", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "SMS Gateway Error (Status {StatusCode}) sending to {Phone}: {ErrorBody}",
                        response.StatusCode, cleanPhone, errorBody);
                    return false;
                }

                _logger.LogInformation("SMS successfully dispatched to {Phone}", cleanPhone);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch SMS to {Phone}", cleanPhone);
                return false;
            }
        }

        private static string NormalizeNigerianPhoneNumber(string phone)
        {
            string clean = phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (clean.StartsWith("+234")) return clean.Substring(1);
            if (clean.StartsWith("234")) return clean;
            if (clean.StartsWith("0")) return "234" + clean.Substring(1);
            return clean;
        }
    }
}
