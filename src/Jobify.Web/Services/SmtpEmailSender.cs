using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Jobify.Web.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["EmailSettings:Host"];
            var port = _configuration.GetValue<int>("EmailSettings:Port");
            var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSSL");
            var userName = _configuration["EmailSettings:UserName"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("EmailSettings are not configured. Email will not be sent.");
                return;
            }

            try
            {
                var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(userName, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(userName ?? "noreply@jobify.com", "Jobify"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email successfully sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {email}");
                throw;
            }
        }
    }
}
