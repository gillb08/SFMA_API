using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos;
using SFMA_API.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class SchoolNotificationService : ISchoolNotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<SchoolNotificationService> _logger;

        public SchoolNotificationService(
            IEmailSender emailSender,
            ISmsSender smsSender,
            IOptions<EmailOptions> emailOptions,
            ILogger<SchoolNotificationService> logger)
        {
            _emailSender = emailSender;
            _smsSender = smsSender;
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        public async Task SendParentAdmissionCredentialsAsync(
            string guardianName,
            string guardianEmail,
            string guardianPhone,
            string studentName,
            string studentCode,
            string classAdmitted,
            string studentPassword,
            string parentPassword)
        {
            try
            {
                string loginUrl = _emailOptions.PortalLoginUrl;
                string schoolName = _emailOptions.FromName;

                string emailSubject = $"Official Admission & Portal Access Credentials for {studentName} - {schoolName}";

                string htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>{emailSubject}</title>
  <style>
    body {{ margin: 0; padding: 0; background-color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1E293B; }}
    .wrapper {{ width: 100%; max-width: 600px; margin: 24px auto; background-color: #FFFFFF; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06); border: 1px solid #E2E8F0; }}
    .header {{ background: linear-gradient(135deg, #0030C2 0%, #1A44D8 60%, #FEAE2C 100%); padding: 32px 24px; text-align: center; color: #FFFFFF; }}
    .header h1 {{ margin: 0; font-size: 22px; font-weight: 800; letter-spacing: -0.5px; }}
    .header p {{ margin: 6px 0 0 0; font-size: 12px; color: #E0E7FF; font-weight: 500; text-transform: uppercase; letter-spacing: 1px; }}
    .content {{ padding: 32px 24px; }}
    .salutation {{ font-size: 16px; font-weight: 700; color: #0F172A; margin-bottom: 12px; }}
    .intro {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 24px; }}
    .card {{ background-color: #F1F5F9; border-radius: 12px; padding: 20px; margin-bottom: 20px; border-left: 4px solid #0030C2; }}
    .card h3 {{ margin: 0 0 12px 0; font-size: 14px; color: #0030C2; text-transform: uppercase; letter-spacing: 0.5px; }}
    .credential-label {{ color: #64748B; font-weight: 600; font-size: 13px; }}
    .credential-value {{ color: #0F172A; font-weight: 700; font-family: Courier, monospace; background: #FFFFFF; padding: 2px 8px; border-radius: 4px; border: 1px solid #CBD5E1; font-size: 13px; }}
    .guide-box {{ background-color: #EFF6FF; border: 1px solid #BFDBFE; border-radius: 12px; padding: 20px; margin: 24px 0; }}
    .guide-box h4 {{ margin: 0 0 14px 0; font-size: 14px; font-weight: 800; color: #1E40AF; }}
    .step-item {{ display: flex; margin-bottom: 12px; font-size: 13px; line-height: 1.5; color: #1E3A8A; }}
    .step-num {{ background-color: #0030C2; color: #FFFFFF; font-weight: 800; font-size: 11px; width: 22px; height: 22px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-right: 12px; flex-shrink: 0; }}
    .cta-container {{ text-align: center; margin: 28px 0; }}
    .btn {{ display: inline-block; background-color: #0030C2; color: #FFFFFF !important; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-weight: 700; font-size: 14px; box-shadow: 0 4px 10px rgba(0, 48, 194, 0.25); }}
    .footer {{ background-color: #0F172A; color: #94A3B8; text-align: center; padding: 24px; font-size: 12px; line-height: 1.5; }}
    .footer a {{ color: #FEAE2C; text-decoration: none; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>ST. FAITH MODEL ACADEMY</h1>
      <p>Motto: Show The Light • Official Admission Notification</p>
    </div>
    <div class=""content"">
      <div class=""salutation"">Dear {guardianName},</div>
      <div class=""intro"">
        We are delighted to congratulate you on the successful admission of <strong>{studentName}</strong> into <strong>{schoolName}</strong>!
        Below are the official portal access credentials for both parent and student accounts.
      </div>

      <!-- Scholar Details Card -->
      <div class=""card"">
        <h3>Scholar Credentials & Profile</h3>
        <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
          <tr><td class=""credential-label"">Scholar Name:</td><td class=""credential-value""><strong>{studentName}</strong></td></tr>
          <tr><td class=""credential-label"">Student Code:</td><td class=""credential-value"">{studentCode}</td></tr>
          <tr><td class=""credential-label"">Class Admitted:</td><td class=""credential-value"">{classAdmitted}</td></tr>
          <tr><td class=""credential-label"">Scholar Initial Password:</td><td class=""credential-value"">{studentPassword}</td></tr>
        </table>
      </div>

      <!-- Parent Portal Details Card -->
      <div class=""card"" style=""border-left-color: #FEAE2C;"">
        <h3 style=""color: #D97706;"">Parent Portal Credentials</h3>
        <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
          <tr><td class=""credential-label"">Login Identifier (Email):</td><td class=""credential-value"">{guardianEmail}</td></tr>
          <tr><td class=""credential-label"">Initial Password:</td><td class=""credential-value"">{parentPassword}</td></tr>
        </table>
      </div>

      <!-- Step-by-Step Guide to Log In -->
      <div class=""guide-box"">
        <h4>📋 STEP-BY-STEP GUIDE TO LOG IN</h4>
        <div class=""step-item"">
          <span class=""step-num"">1</span>
          <div><strong>Open Portal Gateway:</strong> Click the button below or visit <a href=""{loginUrl}"" style=""color: #0030C2; font-weight: bold;"">{loginUrl}</a> on any computer, tablet, or smartphone.</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">2</span>
          <div><strong>Enter Login Identifier:</strong> For Parents, enter your email (<code>{guardianEmail}</code>). For Scholars, enter the Student Code (<code>{studentCode}</code>).</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">3</span>
          <div><strong>Enter Initial Password:</strong> Type in your initial password exactly as shown above (case-sensitive).</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">4</span>
          <div><strong>Sign In &amp; Update Password:</strong> Click <em>'Sign In'</em>. For security, please navigate to your profile settings to set your own personal password.</div>
        </div>
        <div class=""step-item"" style=""margin-bottom: 0;"">
          <span class=""step-num"">5</span>
          <div><strong>Explore Your Portal:</strong> View real-time terminal assessment results, daily attendance, fee statements, and book requirements.</div>
        </div>
      </div>

      <div class=""cta-container"">
        <a href=""{loginUrl}"" class=""btn"" target=""_blank"">Sign In to School Portal</a>
      </div>

      <p style=""font-size: 12px; color: #64748B; text-align: center; margin-top: 20px;"">
        Questions or need assistance? Our admissions support team is always ready to assist.
      </p>
    </div>
    <div class=""footer"">
      &copy; {DateTime.UtcNow.Year} St. Faith Model Academy. All Rights Reserved.<br/>
      Admissions Helpdesk: <a href=""mailto:{_emailOptions.ReplyToEmail}"">{_emailOptions.ReplyToEmail}</a>
    </div>
  </div>
</body>
</html>";

                string textBody = $@"
ST. FAITH MODEL ACADEMY - ADMISSION & PORTAL CREDENTIALS

Dear {guardianName},

Congratulations on the admission of {studentName} into St. Faith Model Academy!

--- SCHOLAR CREDENTIALS ---
Scholar Name: {studentName}
Student Code: {studentCode}
Class: {classAdmitted}
Scholar Initial Password: {studentPassword}

--- PARENT PORTAL CREDENTIALS ---
Login Email: {guardianEmail}
Initial Password: {parentPassword}

--- STEP-BY-STEP GUIDE TO LOG IN ---
1. Open the portal gateway: {loginUrl}
2. Enter your Login Identifier (Email for parent: {guardianEmail} | Student Code for scholar: {studentCode})
3. Enter your initial password (case-sensitive)
4. Click 'Sign In' and change your temporary password on first login
5. Access your student bio-data, academic reports, attendance roll, and bursary ledger

Admissions Office, St. Faith Model Academy
Helpdesk: {_emailOptions.ReplyToEmail}
";

                if (!string.IsNullOrWhiteSpace(guardianEmail))
                {
                    await _emailSender.SendEmailAsync(new EmailMessage
                    {
                        ToEmail = guardianEmail,
                        Subject = emailSubject,
                        HtmlBody = htmlBody,
                        TextBody = textBody
                    });
                }

                if (!string.IsNullOrWhiteSpace(guardianPhone))
                {
                    string smsText = $"St. Faith Model Academy: Welcome {guardianName}! {studentName} admitted. Code: {studentCode}. Parent Login: {guardianEmail}, Pass: {parentPassword}. Login: {loginUrl}";
                    await _smsSender.SendSmsAsync(new SmsMessage
                    {
                        PhoneNumber = guardianPhone,
                        Message = smsText
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-blocking error during parent admission credential notification for scholar {StudentCode}", studentCode);
            }
        }

        public async Task SendStaffCredentialsAsync(
            string staffName,
            string staffEmail,
            string staffPhone,
            string staffCode,
            string department,
            string roleName,
            string tempPassword)
        {
            try
            {
                string loginUrl = _emailOptions.PortalLoginUrl;
                string schoolName = _emailOptions.FromName;

                string emailSubject = $"Staff Account & Portal Access Profile - {staffName} ({staffCode})";

                string htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>{emailSubject}</title>
  <style>
    body {{ margin: 0; padding: 0; background-color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1E293B; }}
    .wrapper {{ width: 100%; max-width: 600px; margin: 24px auto; background-color: #FFFFFF; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06); border: 1px solid #E2E8F0; }}
    .header {{ background: linear-gradient(135deg, #0030C2 0%, #1A44D8 60%, #FEAE2C 100%); padding: 32px 24px; text-align: center; color: #FFFFFF; }}
    .header h1 {{ margin: 0; font-size: 22px; font-weight: 800; }}
    .header p {{ margin: 6px 0 0 0; font-size: 12px; color: #E0E7FF; font-weight: 500; text-transform: uppercase; letter-spacing: 1px; }}
    .content {{ padding: 32px 24px; }}
    .salutation {{ font-size: 16px; font-weight: 700; color: #0F172A; margin-bottom: 12px; }}
    .intro {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 24px; }}
    .card {{ background-color: #F1F5F9; border-radius: 12px; padding: 20px; margin-bottom: 20px; border-left: 4px solid #0030C2; }}
    .card h3 {{ margin: 0 0 12px 0; font-size: 14px; color: #0030C2; text-transform: uppercase; letter-spacing: 0.5px; }}
    .credential-label {{ color: #64748B; font-weight: 600; font-size: 13px; }}
    .credential-value {{ color: #0F172A; font-weight: 700; font-family: Courier, monospace; background: #FFFFFF; padding: 2px 8px; border-radius: 4px; border: 1px solid #CBD5E1; font-size: 13px; }}
    .guide-box {{ background-color: #EFF6FF; border: 1px solid #BFDBFE; border-radius: 12px; padding: 20px; margin: 24px 0; }}
    .guide-box h4 {{ margin: 0 0 14px 0; font-size: 14px; font-weight: 800; color: #1E40AF; }}
    .step-item {{ display: flex; margin-bottom: 12px; font-size: 13px; line-height: 1.5; color: #1E3A8A; }}
    .step-num {{ background-color: #0030C2; color: #FFFFFF; font-weight: 800; font-size: 11px; width: 22px; height: 22px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-right: 12px; flex-shrink: 0; }}
    .cta-container {{ text-align: center; margin: 28px 0; }}
    .btn {{ display: inline-block; background-color: #0030C2; color: #FFFFFF !important; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-weight: 700; font-size: 14px; }}
    .footer {{ background-color: #0F172A; color: #94A3B8; text-align: center; padding: 24px; font-size: 12px; line-height: 1.5; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>ST. FAITH MODEL ACADEMY</h1>
      <p>Staff Portal Authentication Gateway</p>
    </div>
    <div class=""content"">
      <div class=""salutation"">Welcome, {staffName}!</div>
      <div class=""intro"">
        Your official staff account for <strong>{schoolName}</strong> has been provisioned. You can now access your staff portal dashboard using the credentials below:
      </div>

      <div class=""card"">
        <h3>Official Staff Access Profile</h3>
        <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
          <tr><td class=""credential-label"">Staff Name:</td><td class=""credential-value""><strong>{staffName}</strong></td></tr>
          <tr><td class=""credential-label"">Staff Code:</td><td class=""credential-value"">{staffCode}</td></tr>
          <tr><td class=""credential-label"">Department:</td><td class=""credential-value"">{department}</td></tr>
          <tr><td class=""credential-label"">Assigned Role:</td><td class=""credential-value"">{roleName.ToUpperInvariant()}</td></tr>
          <tr><td class=""credential-label"">Login Email:</td><td class=""credential-value"">{staffEmail}</td></tr>
          <tr><td class=""credential-label"">Temporary Password:</td><td class=""credential-value"">{tempPassword}</td></tr>
        </table>
      </div>

      <!-- Step-by-Step Guide to Log In -->
      <div class=""guide-box"">
        <h4>📋 STEP-BY-STEP GUIDE TO LOG IN</h4>
        <div class=""step-item"">
          <span class=""step-num"">1</span>
          <div><strong>Go to Staff Portal:</strong> Visit <a href=""{loginUrl}"" style=""color: #0030C2; font-weight: bold;"">{loginUrl}</a>.</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">2</span>
          <div><strong>Input Email:</strong> Enter your official staff email address (<code>{staffEmail}</code>).</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">3</span>
          <div><strong>Input Temporary Password:</strong> Enter <code>{tempPassword}</code> (case-sensitive).</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">4</span>
          <div><strong>Change Password:</strong> You will be prompted to create your new personal secure password.</div>
        </div>
        <div class=""step-item"" style=""margin-bottom: 0;"">
          <span class=""step-num"">5</span>
          <div><strong>Start Working:</strong> Manage your class register, submit assessment broadsheets, prepare lesson notes, or manage expenditure requisitions.</div>
        </div>
      </div>

      <div class=""cta-container"">
        <a href=""{loginUrl}"" class=""btn"" target=""_blank"">Sign In to Staff Portal</a>
      </div>
    </div>
    <div class=""footer"">
      &copy; {DateTime.UtcNow.Year} St. Faith Model Academy. All Rights Reserved.
    </div>
  </div>
</body>
</html>";

                string textBody = $@"
ST. FAITH MODEL ACADEMY - STAFF ACCOUNT CREATED

Dear {staffName},

Your staff account has been provisioned:
Staff Code: {staffCode}
Department: {department}
Role: {roleName}
Login Email: {staffEmail}
Temporary Password: {tempPassword}

--- STEP-BY-STEP GUIDE TO LOG IN ---
1. Visit the portal gateway: {loginUrl}
2. Enter your email: {staffEmail}
3. Enter your temporary password: {tempPassword}
4. Change your password upon initial sign-in
5. Access your faculty attendance, assessment broadsheets, and lesson tools

Academic Administration, St. Faith Model Academy
";

                if (!string.IsNullOrWhiteSpace(staffEmail))
                {
                    await _emailSender.SendEmailAsync(new EmailMessage
                    {
                        ToEmail = staffEmail,
                        Subject = emailSubject,
                        HtmlBody = htmlBody,
                        TextBody = textBody
                    });
                }

                if (!string.IsNullOrWhiteSpace(staffPhone))
                {
                    string smsText = $"SFMA Staff Alert: Welcome {staffName}! Code: {staffCode}. Login: {staffEmail}, Temp Pass: {tempPassword}. Portal: {loginUrl}";
                    await _smsSender.SendSmsAsync(new SmsMessage
                    {
                        PhoneNumber = staffPhone,
                        Message = smsText
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-blocking error during staff credential notification for staff {StaffCode}", staffCode);
            }
        }

        public async Task SendStaffPasswordResetAsync(
            string staffName,
            string staffEmail,
            string staffPhone,
            string tempPassword)
        {
            try
            {
                string loginUrl = _emailOptions.PortalLoginUrl;

                string emailSubject = "Administrative Password Reset - St. Faith Model Academy";

                string htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>{emailSubject}</title>
  <style>
    body {{ margin: 0; padding: 0; background-color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1E293B; }}
    .wrapper {{ width: 100%; max-width: 600px; margin: 24px auto; background-color: #FFFFFF; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06); border: 1px solid #E2E8F0; }}
    .header {{ background-color: #0030C2; padding: 24px; text-align: center; color: #FFFFFF; }}
    .header h1 {{ margin: 0; font-size: 20px; font-weight: 800; }}
    .content {{ padding: 32px 24px; }}
    .card {{ background-color: #F1F5F9; border-radius: 12px; padding: 20px; margin: 20px 0; border-left: 4px solid #FEAE2C; }}
    .credential-value {{ color: #0F172A; font-weight: 700; font-family: Courier, monospace; background: #FFFFFF; padding: 4px 12px; border-radius: 6px; border: 1px solid #CBD5E1; font-size: 15px; display: inline-block; }}
    .guide-box {{ background-color: #EFF6FF; border: 1px solid #BFDBFE; border-radius: 12px; padding: 20px; margin: 20px 0; }}
    .guide-box h4 {{ margin: 0 0 12px 0; font-size: 13px; font-weight: 800; color: #1E40AF; }}
    .step-item {{ display: flex; margin-bottom: 10px; font-size: 13px; line-height: 1.4; color: #1E3A8A; }}
    .step-num {{ background-color: #0030C2; color: #FFFFFF; font-weight: 800; font-size: 11px; width: 20px; height: 20px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-right: 10px; flex-shrink: 0; }}
    .btn {{ display: inline-block; background-color: #0030C2; color: #FFFFFF !important; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 700; font-size: 14px; }}
    .footer {{ background-color: #0F172A; color: #94A3B8; text-align: center; padding: 20px; font-size: 12px; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>ST. FAITH MODEL ACADEMY</h1>
    </div>
    <div class=""content"">
      <h2>Administrative Password Reset</h2>
      <p>Hello {staffName},</p>
      <p>An administrator has performed a security password reset for your staff account. Use the temporary password below to sign in:</p>
      
      <div class=""card"">
        <p style=""margin: 0 0 8px 0; font-size: 13px; color: #64748B;"">Temporary Password:</p>
        <div class=""credential-value"">{tempPassword}</div>
      </div>

      <!-- Step-by-step Guide -->
      <div class=""guide-box"">
        <h4>📋 HOW TO REGAIN ACCESS:</h4>
        <div class=""step-item"">
          <span class=""step-num"">1</span>
          <div>Visit <a href=""{loginUrl}"" style=""color: #0030C2; font-weight: bold;"">{loginUrl}</a>.</div>
        </div>
        <div class=""step-item"">
          <span class=""step-num"">2</span>
          <div>Enter your email (<code>{staffEmail}</code>) and temporary password.</div>
        </div>
        <div class=""step-item"" style=""margin-bottom: 0;"">
          <span class=""step-num"">3</span>
          <div>Create a new personal password immediately upon logging in.</div>
        </div>
      </div>

      <div style=""text-align: center; margin: 28px 0;"">
        <a href=""{loginUrl}"" class=""btn"" target=""_blank"">Sign In &amp; Change Password</a>
      </div>

      <p style=""font-size: 12px; color: #DC2626;"">
        If you did not request this reset, please notify the School Administration immediately.
      </p>
    </div>
    <div class=""footer"">
      &copy; {DateTime.UtcNow.Year} St. Faith Model Academy. All Rights Reserved.
    </div>
  </div>
</body>
</html>";

                string textBody = $@"
ST. FAITH MODEL ACADEMY - PASSWORD RESET

Hello {staffName},

Your staff account password has been reset by an administrator.
New Temporary Password: {tempPassword}

--- HOW TO REGAIN ACCESS ---
1. Visit: {loginUrl}
2. Enter email ({staffEmail}) and temporary password ({tempPassword})
3. Update your password upon logging in

If you did not request this reset, notify the school administration immediately.
";

                if (!string.IsNullOrWhiteSpace(staffEmail))
                {
                    await _emailSender.SendEmailAsync(new EmailMessage
                    {
                        ToEmail = staffEmail,
                        Subject = emailSubject,
                        HtmlBody = htmlBody,
                        TextBody = textBody
                    });
                }

                if (!string.IsNullOrWhiteSpace(staffPhone))
                {
                    string smsText = $"SFMA Alert: Password reset for {staffName}. New Temp Pass: {tempPassword}. Portal: {loginUrl}";
                    await _smsSender.SendSmsAsync(new SmsMessage
                    {
                        PhoneNumber = staffPhone,
                        Message = smsText
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-blocking error during staff password reset notification for {StaffEmail}", staffEmail);
            }
        }
    }
}
