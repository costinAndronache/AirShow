using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using SendGrid.Client;
using Microsoft.Extensions.Configuration;
using SendGrid.Models.Mail;

namespace AirShow.Models.Services
{
    public class SendGridMailService : IMailService
    {
        private IConfigurationRoot _config;

        public SendGridMailService(IConfigurationRoot config)
        {
            _config = config;
        }

        public async Task<OperationStatus> SendMessageToAddress(string message, string address)
        {

            var apiKey = _config["Keys:sendgrid"];
            var conn = new SendGrid.Connections.ApiKeyConnection(apiKey);
            var client = new SendGrid.SendGridClient(conn);

            var email = new Email();
            var content = new Content();
            content.Type = "text/html";
            content.Value = message;

            var fromPers = new Personalization();
            var toDetail = new EmailDetail
            {
                Email = address
            };

            var fromDetail = new EmailDetail
            {
                Email = "noReply@personalairshow.not"
            };

            fromPers.Subject = "Account activation";
            fromPers.To = new List<EmailDetail> { toDetail };

            email.Personalizations = new List<Personalization> { fromPers};
            email.From = fromDetail;
            email.Content = new List<Content> { content };

            await client.MailClient.SendAsync(email);
            return new OperationStatus();
        }
    }
}
