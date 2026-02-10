using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Implements.Hangfire;
using Vegetarian.Infrastructure.Options;

namespace Vegetarian.Infrastructure.Services.Email
{
    public class EmailService : IEmailService
    {
        public readonly EmailOptions _options;

        public EmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task EmailSender(string toEmail, string subject, string body)
        {
            try
            {
                var host = _options.SMTP_HOST;
                var port = _options.SMTP_PORT;
                var enableSsl = _options.SMTP_ENABLE_SSL;
                var userName = _options.SMTP_USERNAME;
                var authPassword = _options.SMTP_PASSWORD;
                var senderEmail = _options.SMTP_FROM_EMAIL;
                var senderName = _options.SMTP_FROM_NAME;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(userName, authPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
