using System.Net;
using System.Net.Mail;

namespace SteadyBooks.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
    Task<bool> SendAccountConfirmationEmailAsync(string to, string firmName, string confirmationLink);
    Task<bool> SendPasswordResetEmailAsync(string to, string firmName, string resetLink);
    Task<bool> SendDashboardInviteEmailAsync(string to, string clientName, string firmName, string dashboardLink, string? customMessage);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load email settings from configuration
        _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["Email:Username"] ?? "";
        _smtpPassword = _configuration["Email:Password"] ?? "";
        _fromEmail = _configuration["Email:FromEmail"] ?? _smtpUsername;
        _fromName = _configuration["Email:FromName"] ?? "SteadyBooks";
        _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            // Validate configuration
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("Email not configured. Skipping email send to {Email}", to);
                return false;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_smtpServer, _smtpPort);
            client.EnableSsl = _enableSsl;
            client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

            await client.SendMailAsync(message);
            
            _logger.LogInformation("Email sent successfully to {Email}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            return false;
        }
    }

    public async Task<bool> SendAccountConfirmationEmailAsync(string to, string firmName, string confirmationLink)
    {
        var subject = "Confirm Your SteadyBooks Account";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to SteadyBooks!</h1>
        </div>
        <div class='content'>
            <h2>Hi {firmName},</h2>
            <p>Thank you for signing up for SteadyBooks! We're excited to help you share beautiful financial dashboards with your clients.</p>
            
            <p>To get started, please confirm your email address by clicking the button below:</p>
            
            <p style='text-align: center;'>
                <a href='{confirmationLink}' class='button'>Confirm Email Address</a>
            </p>
            
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #667eea;'>{confirmationLink}</p>
            
            <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
            
            <h3>What's Next?</h3>
            <ul>
                <li>Set up your firm branding (logo, colors)</li>
                <li>Connect to QuickBooks Online</li>
                <li>Create your first client dashboard</li>
                <li>Share secure links with clients</li>
            </ul>
            
            <p>If you didn't create a SteadyBooks account, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            <p>© 2025 SteadyBooks. All rights reserved.</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, htmlBody);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string firmName, string resetLink)
    {
        var subject = "Reset Your SteadyBooks Password";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hi {firmName},</h2>
            <p>We received a request to reset your SteadyBooks password.</p>
            
            <p>Click the button below to reset your password:</p>
            
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #667eea;'>{resetLink}</p>
            
            <div class='warning'>
                <strong>?? Security Note:</strong> This link will expire in 24 hours. If you didn't request a password reset, please ignore this email or contact support if you have concerns.
            </div>
            
            <p>If you're having trouble, please contact us at support@steadybooks.com</p>
        </div>
        <div class='footer'>
            <p>© 2025 SteadyBooks. All rights reserved.</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, htmlBody);
    }

    public async Task<bool> SendDashboardInviteEmailAsync(string to, string clientName, string firmName, string dashboardLink, string? customMessage)
    {
        var subject = $"Your Financial Dashboard from {firmName}";
        
        var customMessageHtml = !string.IsNullOrEmpty(customMessage) 
            ? $@"<div style='background: #e3f2fd; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0;'>
                    <p style='margin: 0;'><strong>Message from {firmName}:</strong></p>
                    <p style='margin: 10px 0 0 0;'>{customMessage}</p>
                 </div>"
            : "";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 15px 40px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; font-size: 16px; font-weight: bold; }}
        .features {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .feature {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .feature:last-child {{ border-bottom: none; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Your Financial Dashboard</h1>
        </div>
        <div class='content'>
            <h2>Hi {clientName},</h2>
            <p>{firmName} has created a secure financial dashboard just for you!</p>
            
            {customMessageHtml}
            
            <p>Your dashboard provides a clear view of your financial health, including:</p>
            
            <div class='features'>
                <div class='feature'>?? <strong>Cash Balance</strong> - Your current cash position</div>
                <div class='feature'>?? <strong>Profit & Loss</strong> - Revenue and expenses overview</div>
                <div class='feature'>?? <strong>Tax Liability</strong> - Estimated taxes due</div>
                <div class='feature'>?? <strong>Outstanding Invoices</strong> - Money owed to you</div>
            </div>
            
            <p style='text-align: center;'>
                <a href='{dashboardLink}' class='button'>View Your Dashboard</a>
            </p>
            
            <p style='font-size: 14px; color: #666;'>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #667eea; font-size: 12px;'>{dashboardLink}</p>
            
            <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
            
            <p style='font-size: 14px;'>
                <strong>?? Secure & Read-Only:</strong> This dashboard is completely secure and read-only. 
                You can view your financial information anytime, but no changes can be made through this portal.
            </p>
            
            <p style='font-size: 14px;'>
                <strong>Questions?</strong> Contact {firmName} directly if you need any clarification about your financial data.
            </p>
        </div>
        <div class='footer'>
            <p>This dashboard is provided by {firmName} using SteadyBooks.</p>
            <p>© 2025 SteadyBooks. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, htmlBody);
    }
}
