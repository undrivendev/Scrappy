using System;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ldv.Scrappy.Bll
{
    public class SendGridNotifier : INotifier
    {
        private readonly SendGridNotifierParameters _parameters;

        public SendGridNotifier(SendGridNotifierParameters parameters)
        {
            _parameters = parameters;
        }

        public async Task Send(Rule rule, string oldData, string newData)
        {
            var client = new SendGridClient(_parameters.ApiKey);
            var message =
                $"New data: {newData}, Old data: {oldData ?? string.Empty}";
            var recipients = _parameters
                .Recipients
                .Select((r) => new EmailAddress(r))
                .ToList();

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                new EmailAddress("scrappy@lucadallavalle.com"),
                recipients,
                $"Scrappy alert: rule {rule.Id} triggered!",
                message,
                null);
            var response = await client.SendEmailAsync(msg);
        }
    }
}