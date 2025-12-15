# ?? Email Configuration Guide

## ? What's Been Implemented

### Email Features:
1. **? Account Registration Email** - Sent when new users register
2. **? Email Confirmation** - Required before users can log in
3. **? Dashboard Invite Emails** - Send dashboard links to clients
4. **? Password Reset Emails** - Ready for future implementation

---

## ?? Email Configuration

### Option 1: Gmail (Easiest for Testing)

#### Step 1: Enable App Passwords in Gmail
1. Go to your Google Account: https://myaccount.google.com/
2. Click on **Security** in the left menu
3. Enable **2-Step Verification** (if not already enabled)
4. Go to **App passwords**
5. Select **Mail** and **Windows Computer** (or Other)
6. Click **Generate**
7. **Copy the 16-character password** (no spaces)

#### Step 2: Update appsettings.Development.json
```json
"Email": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": "587",
  "Username": "your-email@gmail.com",
  "Password": "YOUR_16_CHAR_APP_PASSWORD",
  "FromEmail": "your-email@gmail.com",
  "FromName": "SteadyBooks",
  "EnableSsl": "true"
}
```

---

### Option 2: Outlook/Office 365

```json
"Email": {
  "SmtpServer": "smtp.office365.com",
  "SmtpPort": "587",
  "Username": "your-email@outlook.com",
  "Password": "your-password",
  "FromEmail": "your-email@outlook.com",
  "FromName": "SteadyBooks",
  "EnableSsl": "true"
}
```

---

### Option 3: SendGrid (Recommended for Production)

#### Step 1: Sign Up for SendGrid
1. Go to https://sendgrid.com/
2. Sign up for free account (100 emails/day free)
3. Verify your email
4. Create an API Key:
   - Go to Settings ? API Keys
   - Click "Create API Key"
   - Select "Full Access"
   - Copy the API key

#### Step 2: Update appsettings.json
```json
"Email": {
  "SmtpServer": "smtp.sendgrid.net",
  "SmtpPort": "587",
  "Username": "apikey",
  "Password": "YOUR_SENDGRID_API_KEY",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "SteadyBooks",
  "EnableSsl": "true"
}
```

---

## ?? Testing Email Functionality

### Test 1: Registration Email
1. **Restart your app** (Shift+F5, then F5)
2. **Register a new account** with a real email address
3. **Check your email** for confirmation message
4. **Click the confirmation link**
5. **Log in** with your confirmed account

### Test 2: Dashboard Invite Email
1. **Log in** to your account
2. **Create a dashboard** (or use existing one)
3. **Go to Share page** for the dashboard
4. **Enter client email** and optional message
5. **Click "Send Email Invitation"**
6. **Check the recipient email**

---

## ?? Configuration Settings Explained

### SMTP Settings:
- **SmtpServer**: SMTP server address
- **SmtpPort**: Port number (587 for TLS, 465 for SSL)
- **Username**: Your email or "apikey" for SendGrid
- **Password**: Your password or API key
- **FromEmail**: The "from" address in emails
- **FromName**: Display name (e.g., "SteadyBooks")
- **EnableSsl**: Use SSL/TLS (always true for security)

---

## ?? Security Best Practices

### For Development:
Store email credentials in **User Secrets**:

```powershell
# In terminal
cd SteadyBooks
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"
```

### For Production:
Use **Environment Variables** or **Azure Key Vault**:

#### Environment Variables:
```
Email__Username=your-email@gmail.com
Email__Password=your-app-password
```

#### Azure App Service:
1. Go to your App Service in Azure Portal
2. Click **Configuration** ? **Application settings**
3. Add new application settings:
   - Name: `Email:Username`, Value: `your-email@gmail.com`
   - Name: `Email:Password`, Value: `your-app-password`

---

## ?? Email Templates

### Account Confirmation Email
- Professional gradient header
- Clear call-to-action button
- Next steps guide
- Security note

### Dashboard Invite Email
- Firm branding (name displayed)
- Custom message support
- Dashboard features highlight
- Secure link with explanation

### Password Reset Email (Ready for Implementation)
- Security warnings
- Expiration notice (24 hours)
- Clear reset button

---

## ?? Troubleshooting

### Email Not Sending?

1. **Check Logs**:
   Look for errors in Visual Studio Output window:
   ```
   Failed to send email to [email]
   Email not configured. Skipping email send
   ```

2. **Verify Configuration**:
   - SMTP server correct?
   - Port number correct?
   - Username/password correct?
   - SSL enabled?

3. **Test SMTP Connection**:
   Try sending a test email using the same settings in your email client

4. **Gmail Specific**:
   - Is 2FA enabled?
   - Did you generate an App Password?
   - Did you copy the password correctly (no spaces)?

5. **Firewall/Antivirus**:
   - Is SMTP port 587 blocked?
   - Try disabling antivirus temporarily

---

## ?? Email Behavior

### Registration Flow:
```
1. User registers ? Email sent with confirmation link
2. User clicks link ? Email confirmed
3. User can now log in
```

### Without Email Configured:
- Registration still works
- User will see "Email not configured" message
- Users won't be able to log in until you manually confirm their email in database

### Dashboard Invites:
- If email configured: Email sent automatically
- If not configured: Shows message to copy link manually
- Link always works regardless of email status

---

## ?? Going to Production

### Checklist:
- [ ] Sign up for SendGrid (or other email service)
- [ ] Configure SPF/DKIM records for your domain
- [ ] Test emails from production environment
- [ ] Set up monitoring for failed emails
- [ ] Configure bounce handling
- [ ] Add unsubscribe functionality (if required)

### Recommended: SendGrid
- **Why?** Better deliverability, tracking, analytics
- **Free Tier:** 100 emails/day
- **Paid Tier:** $14.95/month for 40,000 emails

---

## ?? Current Status

### ? Fully Implemented:
- Email service with SMTP support
- Account confirmation emails
- Dashboard invite emails
- Professional HTML email templates
- Error handling and logging
- Configuration via appsettings.json

### ?? Ready to Configure:
- Password reset emails (template ready, just need to wire up)
- Two-factor authentication emails (future enhancement)

---

## ?? Quick Start (TL;DR)

1. **Get Gmail App Password**: Google Account ? Security ? App passwords
2. **Update `appsettings.Development.json`**: Add your email and password
3. **Restart app**: Stop (Shift+F5) and Start (F5)
4. **Test registration**: Register with your email
5. **Check inbox**: Confirm your account
6. **Send dashboard invite**: Test client email

---

## ?? Support

If you encounter issues:
1. Check Visual Studio Output window for errors
2. Verify email configuration settings
3. Test with a different email provider
4. Check spam folder
5. Review firewall/antivirus settings

---

**Email functionality is now ready to use!** ???

Just configure your SMTP settings and emails will start sending automatically! ??
