using MailKit.Net.Smtp;
using MimeKit;

namespace UserService.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string hoTen);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string hoTen)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HKShop", _configuration["Email:FromAddress"]));
            message.To.Add(new MailboxAddress(hoTen, toEmail));
            message.Subject = "Đặt lại mật khẩu - HKShop";

            var resetUrl = $"{_configuration["App:BaseUrl"]}/reset-password?token={resetToken}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Xin chào {hoTen},</h2>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản HKShop của mình.</p>
                    <p>Nhấn vào liên kết bên dưới để đặt lại mật khẩu:</p>
                    <a href='{resetUrl}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Đặt lại mật khẩu
                    </a>
                    <p>Liên kết này có hiệu lực trong 1 giờ.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
                    <br>
                    <p>Trân trọng,<br>Đội ngũ HKShop</p>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["Email:SmtpHost"],
                int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                false
            );
            await smtp.AuthenticateAsync(_configuration["Email:Username"], _configuration["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            throw;
        }
    }
}
