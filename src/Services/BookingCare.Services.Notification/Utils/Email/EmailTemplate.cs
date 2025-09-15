using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Notification.Utils.Email;

public class EmailTemplate
{
    public string BuildOtpEmailHtml(string otpCode, string purpose)
    {
        var safePurpose = string.IsNullOrWhiteSpace(purpose) ? "xác thực" : purpose;
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>BookingCare OTP</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#0ea5e9; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .otp {{ display:inline-block; padding:12px 20px; background:#f1f5f9; border:1px solid #e2e8f0; border-radius:10px; font-weight:700; font-size:20px; letter-spacing:4px; color:#111827; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>

  </head>
  <body>
    <div class=""card"">
      <div class=""header"">
        <div class=""brand"">BookingCare Security</div>
      </div>
      <div class=""content"">
        <p class=""greeting"">Xin chào,</p>
        <p class=""lead"">Đây là mã xác thực một lần (OTP) của bạn. Vui lòng nhập mã dưới đây để tiếp tục quy trình ""{safePurpose}"".</p>
        <div class=""otp"">{otpCode}</div>
        <p class=""muted"">Mã sẽ hết hạn sau 5 phút. Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>
        <div class=""divider""></div>
        <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
      </div>
      <div class=""footer"">Bạn nhận được email này vì có yêu cầu xác thực từ hệ thống BookingCare.</div>
    </div>
  </body>
</html>";
    }

    public string BuildPasswordResetEmailHtml(string resetUrl)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Đặt lại mật khẩu - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#dc2626; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .reset-button {{ display:inline-block; padding:14px 28px; background:#dc2626; color:#fff; text-decoration:none; border-radius:8px; font-weight:600; font-size:16px; margin:16px 0; }}
    .reset-button:hover {{ background:#b91c1c; }}
    .warning {{ background:#fef2f2; border:1px solid #fecaca; border-radius:8px; padding:16px; margin:20px 0; color:#991b1b; }}
    .warning-icon {{ font-weight:bold; color:#dc2626; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
    .url-fallback {{ word-break:break-all; color:#6b7280; font-size:12px; margin-top:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare Security</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Xin chào,</p>
      <p class=""lead"">Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nếu bạn đã yêu cầu thay đổi này, vui lòng nhấp vào nút bên dưới để tạo mật khẩu mới.</p>
      
      <div style=""text-align:center;"">
        <a href=""{resetUrl}"" class=""reset-button"">Đặt lại mật khẩu</a>
      </div>
      
      <div class=""warning"">
        <p><span class=""warning-icon"">⚠️</span> <strong>Lưu ý bảo mật:</strong></p>
        <ul style=""margin:8px 0; padding-left:20px;"">
          <li>Liên kết này chỉ có hiệu lực trong 24 giờ</li>
          <li>Chỉ sử dụng một lần duy nhất</li>
          <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
          <li>Mật khẩu mới phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu nút không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:</p>
      <div class=""url-fallback"">{resetUrl}</div>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }
}

