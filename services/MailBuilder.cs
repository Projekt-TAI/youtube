using MimeKit.Text;
using MimeKit;

namespace TAIBackend.services;

public class MailBuilder
{
    private readonly string SenderMail;

    public MailBuilder()
    {
        SenderMail = Environment.GetEnvironmentVariable("MAIL_SENDER") ?? throw new InvalidOperationException();
    }

    public MimeMessage BuildMail(List<string> recipients, string subject, string body)
    {
        var mail = new MimeMessage();

        mail.From.Add(MailboxAddress.Parse(SenderMail));

        foreach (var singleRecipient in recipients)
        {
            mail.To.Add(MailboxAddress.Parse(singleRecipient));
        }

        mail.Subject = subject;

        mail.Body = new TextPart(TextFormat.Html)
        {
            Text = body
        };

        return mail;
    }
}
