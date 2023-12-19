using MimeKit;
using MailKit.Net.Smtp;

namespace TAIBackend.services;

public class MailSender
{
    private readonly string Host;
    private readonly int Port;
    private readonly string Login;
    private readonly string Password;

    public MailSender()
    {
        Host = Environment.GetEnvironmentVariable("MAIL_HOST") ?? throw new InvalidOperationException();
        Port = int.Parse(Environment.GetEnvironmentVariable("MAIL_PORT") ?? throw new InvalidOperationException());
        Login = Environment.GetEnvironmentVariable("MAIL_LOGIN") ?? throw new InvalidOperationException();
        Password = Environment.GetEnvironmentVariable("MAIL_PASSWORD") ?? throw new InvalidOperationException();
    }
    public async Task SendMail(MimeMessage mail)
    {
        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(Host, Port);
        await smtp.AuthenticateAsync(Login, Password);

        await smtp.SendAsync(mail);

        await smtp.DisconnectAsync(true);
    }
}
