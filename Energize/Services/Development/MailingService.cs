using Energize.Essentials;
using Energize.Interfaces.Services.Development;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Energize.Services.Development
{
    [Service("Mail")]
    public class MailingService : ServiceImplementationBase, IMailingService
    {
        private readonly SmtpClient SmtpClient;
        private readonly string MailAddress;

        public MailingService()
        {
            this.MailAddress = Config.Instance.Mail.MailAddress;
            this.SmtpClient = new SmtpClient(Config.Instance.Mail.ServerAddress)
            {
                Port = Config.Instance.Mail.ServerPort,
                Credentials = new NetworkCredential(this.MailAddress, Config.Instance.Mail.MailPassword),
                EnableSsl = true,
            };
        }

        public async Task SendMailAsync(string toMail, string subject, string body)
        {
            string templatePath = "Data/mail_template.html";
            bool isHtml = File.Exists(templatePath);
            if (isHtml)
            {
                string html = await File.ReadAllTextAsync(templatePath);
                body = html.Replace("%SUBJECT%", subject).Replace("%CONTENT%", body);
            }

            MailMessage mail = new MailMessage
            {
                From = new MailAddress(this.MailAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
            };

            mail.To.Add(toMail);
            await this.SmtpClient.SendMailAsync(mail);
        }

        [DiscordEvent("LoggedOut")]
        public Task OnLoggedOutAsync()
            => this.SendMailAsync(Config.Instance.Mail.DevMailAddress, "Logged out", $"Got disconnected at {DateTime.Now}");
    }
}
