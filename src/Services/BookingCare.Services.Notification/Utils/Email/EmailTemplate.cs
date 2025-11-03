using BookingCare.Services.Notification.Models.DTOs;
using System.Text.Json;

namespace BookingCare.Services.Notification.Utils.Email;

public static class EmailTemplate
{
    /// <summary>
    /// Parse and build HTML for features from JSON string
    /// Returns HTML for features to be included in the same card
    /// </summary>
    private static string BuildFeaturesHtml(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return "";
        }

        try
        {
            // Try to parse as JSON array
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var features = JsonSerializer.Deserialize<List<FeatureItem>>(featuresJson, options);

            if (features == null || features.Count == 0)
            {
                // If parsing succeeds but list is empty, show debug info
                return $@"
        <div style=""height:1px; background:#e5e7eb; margin:12px 0;""></div>
        <div style=""margin-top:12px;"">
          <strong style=""color:#047857;"">✨ Tính năng của gói:</strong>
        </div>
        <div class=""info-item"" style=""color:#ef4444;"">(Không có tính năng nào được cấu hình)</div>";
            }

            // Build feature items as simple div rows (no list)
            var featuresHtml = string.Join("", features.Select(f =>
            {
                var icon = GetFeatureIcon(f.IconType);
                var text = string.IsNullOrWhiteSpace(f.Text) ? "(Không có mô tả)" : f.Text;
                var subtext = !string.IsNullOrWhiteSpace(f.Subtext)
                    ? $"<div style=\"font-size:11px; color:#6b7280; margin-left:24px; margin-top:2px;\">{f.Subtext}</div>"
                    : "";
                return $@"
        <div class=""info-item"">{icon} {text}{subtext}</div>";
            }));

            // Return as part of the same card with a separator
            return $@"
        <div style=""height:1px; background:#e5e7eb; margin:12px 0;""></div>
        <div style=""margin-top:12px;"">
          <strong style=""color:#047857;"">✨ Tính năng của gói:</strong>
        </div>{featuresHtml}";
        }
        catch (Exception ex)
        {
            // If JSON parsing fails, show the raw data for debugging
            return $@"
        <div style=""height:1px; background:#e5e7eb; margin:12px 0;""></div>
        <div style=""margin-top:12px;"">
          <strong style=""color:#047857;"">✨ Tính năng của gói:</strong>
        </div>
        <div class=""info-item"" style=""color:#6b7280; font-size:12px; word-break:break-all;"">{featuresJson}</div>
        <div class=""info-item"" style=""color:#ef4444; font-size:11px;"">(Lỗi parse: {ex.Message})</div>";
        }
    }

    /// <summary>
    /// Get icon for feature based on iconType
    /// </summary>
    private static string GetFeatureIcon(string? iconType)
    {
        return iconType?.ToLower() switch
        {
            "check" => "✅",
            "plus" => "➕",
            "star" => "⭐",
            "heart" => "❤️",
            "shield" => "🛡️",
            "rocket" => "🚀",
            _ => "✅"
        };
    }

    /// <summary>
    /// Feature item model for JSON parsing
    /// </summary>
    private class FeatureItem
    {
        public string Text { get; set; } = string.Empty;
        public string? IconType { get; set; }
        public string? Subtext { get; set; }
    }

    // Date format constants to avoid code duplication (SonarQube S1192)
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    public static string BuildOtpEmailHtml(string otpCode, string purpose)
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

    /// <summary>
    /// Build email content for doctor credentials (auto-generated password)
    /// </summary>
    public static string BuildDoctorCredentialsEmailHtml(
        string fullName,
        string email,
        string password,
        string loginUrl,
        string? hospitalName = null)
    {
        var hospitalInfo = !string.IsNullOrEmpty(hospitalName)
            ? $"<div class=\"info-item\"><strong>Bệnh viện:</strong> {hospitalName}</div>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Chào mừng đến với BookingCare - Thông tin đăng nhập</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#0ea5e9; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .welcome-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .credentials-box {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .credentials-box strong {{ color:#0369a1; }}
    .info-item {{ margin:8px 0; }}
    .credential-value {{ display:inline-block; padding:8px 12px; background:#ffffff; border:1px solid #e2e8f0; border-radius:6px; font-family:'Courier New', monospace; font-weight:600; color:#111827; margin-left:8px; word-break:break-all; }}
    .password-value {{ background:#fef2f2; border:1px solid #fecaca; color:#991b1b; }}
    .login-button {{ display:inline-block; padding:14px 28px; background:#0ea5e9; color:#fff; text-decoration:none; border-radius:8px; font-weight:600; font-size:16px; margin:16px 0; }}
    .login-button:hover {{ background:#0284c7; }}
    .warning {{ background:#fef2f2; border:1px solid #fecaca; border-radius:8px; padding:16px; margin:20px 0; color:#991b1b; }}
    .warning-icon {{ font-weight:bold; color:#dc2626; }}
    .security-tips {{ background:#f0fdf4; border:1px solid #86efac; border-radius:8px; padding:16px; margin:20px 0; }}
    .security-tips strong {{ color:#166534; }}
    .security-tips ul {{ margin:8px 0; padding-left:20px; }}
    .security-tips li {{ margin:4px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Hệ thống quản lý khám bệnh</div>
    </div>
    <div class=""content"">
      <div class=""welcome-icon"">🎉</div>
      <p class=""greeting"">Kính gửi BS. {fullName},</p>
      <p class=""lead"">Chào mừng bạn đến với hệ thống BookingCare! Tài khoản của bạn đã được tạo thành công.</p>
      
      <div class=""credentials-box"">
        <p><strong>🔐 Thông tin đăng nhập:</strong></p>{hospitalInfo}
        <div class=""info-item"">
          <strong>Email:</strong>
          <span class=""credential-value"">{email}</span>
        </div>
        <div class=""info-item"">
          <strong>Mật khẩu tạm thời:</strong>
          <span class=""credential-value password-value"">{password}</span>
        </div>
      </div>
      
      <div style=""text-align:center;"">
        <a href=""{loginUrl}"" class=""login-button"">Đăng nhập ngay</a>
      </div>
      
      <div class=""warning"">
        <p><span class=""warning-icon"">⚠️</span> <strong>BẮT BUỘC ĐỔI MẬT KHẨU:</strong></p>
        <p>Đây là mật khẩu tạm thời được hệ thống tự động tạo. Vì lý do bảo mật, bạn <strong>BẮT BUỘC phải đổi mật khẩu</strong> ngay khi đăng nhập lần đầu tiên.</p>
      </div>
      
      <div class=""security-tips"">
        <p><strong>🛡️ Hướng dẫn bảo mật:</strong></p>
        <ul>
          <li>Không chia sẻ mật khẩu này với bất kỳ ai</li>
          <li>Đăng nhập và đổi mật khẩu ngay lập tức</li>
          <li>Mật khẩu mới phải:
            <ul>
              <li>Có ít nhất 8 ký tự</li>
              <li>Bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt</li>
              <li>Không sử dụng thông tin cá nhân dễ đoán</li>
            </ul>
          </li>
          <li>Xóa email này sau khi đã đổi mật khẩu thành công</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu bạn không yêu cầu tạo tài khoản này hoặc có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi ngay:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> 1900-xxxx<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    public static string BuildPasswordResetEmailHtml(string resetUrl)
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

    /// <summary>
    /// Build email content for appointment cancellation refund (patient has bank account - PENDING status)
    /// </summary>
    public static string BuildRefundEmailWithBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        decimal refundAmount)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận hủy lịch hẹn - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#4CAF50; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .info-box {{ background:#e8f5e9; border:1px solid #c8e6c9; border-radius:8px; padding:16px; margin:20px 0; }}
    .info-box strong {{ color:#2e7d32; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:20px; font-weight:700; color:#2e7d32; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Xác nhận hủy lịch hẹn</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi rất tiếc phải thông báo rằng lịch hẹn của quý khách đã được hủy.</p>
      
      <div class=""info-box"">
        <p><strong>📅 Thông tin lịch hẹn:</strong></p>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy HH:mm}</div>
        <div class=""info-item""><strong>Lý do hủy:</strong> {cancellationReason}</div>
        <div class=""info-item""><strong>Số tiền hoàn trả:</strong> <span class=""amount"">{refundAmount:N0} VNĐ</span></div>
      </div>
      
      <p><strong>💰 Về việc hoàn tiền:</strong></p>
      <p class=""lead"">Chúng tôi sẽ tiến hành hoàn trả số tiền <strong>{refundAmount:N0} VNĐ</strong> vào tài khoản ngân hàng của quý khách trong vòng <strong>5-7 ngày làm việc</strong>.</p>
      
      <p class=""muted"">Chúng tôi xin lỗi vì sự bất tiện này. Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hotline: <strong>1900-xxxx</strong>.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for appointment cancellation refund (patient has NO bank account - WAITING status)
    /// </summary>
    public static string BuildRefundEmailNoBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        decimal refundAmount)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận hủy lịch hẹn - Cần hành động - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#FF9800; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .warning-box {{ background:#fff3e0; border:1px solid #ffe0b2; border-radius:8px; padding:16px; margin:20px 0; }}
    .warning-box strong {{ color:#f57c00; }}
    .action-required {{ background:#ffebee; border:2px solid #ef5350; border-radius:8px; padding:16px; margin:20px 0; }}
    .action-required strong {{ color:#c62828; }}
    .action-required ol {{ margin:12px 0; padding-left:20px; }}
    .action-required li {{ margin:8px 0; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:20px; font-weight:700; color:#f57c00; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Xác nhận hủy lịch hẹn</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi rất tiếc phải thông báo rằng lịch hẹn của quý khách đã được hủy.</p>
      
      <div class=""warning-box"">
        <p><strong>📅 Thông tin lịch hẹn:</strong></p>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy HH:mm}</div>
        <div class=""info-item""><strong>Lý do hủy:</strong> {cancellationReason}</div>
        <div class=""info-item""><strong>Số tiền hoàn trả:</strong> <span class=""amount"">{refundAmount:N0} VNĐ</span></div>
      </div>
      
      <div class=""action-required"">
        <p><strong>⚠️ HÀNH ĐỘNG YÊU CẦU - VUI LÒNG ĐỌC KỸ:</strong></p>
        <p>Để nhận lại số tiền <strong>{refundAmount:N0} VNĐ</strong>, quý khách vui lòng thực hiện các bước sau:</p>
        <ol>
          <li>Đăng nhập vào tài khoản BookingCare của quý khách</li>
          <li>Vào phần <strong>""Tài khoản ngân hàng""</strong> trong cài đặt</li>
          <li>Thêm thông tin tài khoản ngân hàng để nhận hoàn tiền</li>
        </ol>
        <p><strong>⏰ Lưu ý quan trọng:</strong> Nếu không cung cấp thông tin tài khoản ngân hàng, chúng tôi sẽ không thể hoàn trả tiền cho quý khách.</p>
      </div>
      
      <p class=""muted"">Sau khi cập nhật thông tin tài khoản ngân hàng, chúng tôi sẽ tiến hành hoàn tiền trong vòng <strong>5-7 ngày làm việc</strong>.</p>
      <p class=""muted"">Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hotline: <strong>1900-xxxx</strong>.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for successful refund completion
    /// </summary>
    public static string BuildRefundCompletedEmailHtml(
        string patientName,
        decimal refundAmount,
        string bankName,
        string accountNumber,
        DateTime transferDate)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Hoàn tiền thành công - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .success-box strong {{ color:#047857; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:24px; font-weight:700; color:#10b981; }}
    .check-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Hoàn tiền thành công</div>
    </div>
    <div class=""content"">
      <div class=""check-icon"">✅</div>
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi xin thông báo rằng khoản hoàn tiền của quý khách đã được chuyển thành công!</p>
      
      <div class=""success-box"">
        <p><strong>💰 Thông tin hoàn tiền:</strong></p>
        <div class=""info-item""><strong>Số tiền:</strong> <span class=""amount"">{refundAmount:N0} VNĐ</span></div>
        <div class=""info-item""><strong>Ngân hàng:</strong> {bankName}</div>
        <div class=""info-item""><strong>Số tài khoản:</strong> {accountNumber}</div>
        <div class=""info-item""><strong>Ngày chuyển:</strong> {transferDate:dd/MM/yyyy HH:mm}</div>
      </div>
      
      <p><strong>📝 Lưu ý:</strong></p>
      <p class=""lead"">Số tiền sẽ được ghi nhận vào tài khoản ngân hàng của quý khách trong vòng <strong>1-2 ngày làm việc</strong>, tùy thuộc vào quy trình xử lý của từng ngân hàng.</p>
      <p class=""lead"">Vui lòng kiểm tra tài khoản ngân hàng của quý khách để xác nhận giao dịch.</p>
      
      <p class=""muted"">Cảm ơn quý khách đã sử dụng dịch vụ BookingCare. Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hotline: <strong>1900-xxxx</strong>.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for bank account issue report
    /// </summary>
    public static string BuildRefundBankIssueReportedEmailHtml(
        string patientName,
        decimal refundAmount,
        string issueDescription,
        string? bankName,
        string? accountNumber)
    {
        var bankInfoHtml = "";
        if (!string.IsNullOrEmpty(bankName) && !string.IsNullOrEmpty(accountNumber))
        {
            bankInfoHtml = $@"
        <div class=""info-item""><strong>Ngân hàng hiện tại:</strong> {bankName}</div>
        <div class=""info-item""><strong>Số tài khoản:</strong> {accountNumber}</div>";
        }

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Sự cố tài khoản ngân hàng - Cần cập nhật - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#ef4444; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .error-box {{ background:#fee2e2; border:1px solid #fecaca; border-radius:8px; padding:16px; margin:20px 0; }}
    .error-box strong {{ color:#dc2626; }}
    .action-required {{ background:#fef3c7; border:2px solid #fcd34d; border-radius:8px; padding:16px; margin:20px 0; }}
    .action-required strong {{ color:#92400e; }}
    .action-required ol {{ margin:12px 0; padding-left:20px; }}
    .action-required li {{ margin:8px 0; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:20px; font-weight:700; color:#dc2626; }}
    .warning-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Sự cố hoàn tiền</div>
    </div>
    <div class=""content"">
      <div class=""warning-icon"">⚠️</div>
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi rất tiếc phải thông báo rằng có sự cố xảy ra trong quá trình hoàn tiền cho quý khách.</p>
      
      <div class=""error-box"">
        <p><strong>❌ Sự cố đã phát sinh:</strong></p>
        <div class=""info-item""><strong>Số tiền hoàn trả:</strong> <span class=""amount"">{refundAmount:N0} VNĐ</span></div>{bankInfoHtml}
        <div class=""info-item""><strong>Vấn đề:</strong> {issueDescription}</div>
      </div>
      
      <div class=""action-required"">
        <p><strong>🔔 HÀNH ĐỘNG YÊU CẦU - VUI LÒNG ĐỌC KỸ:</strong></p>
        <p>Để tiếp tục nhận khoản hoàn trả <strong>{refundAmount:N0} VNĐ</strong>, quý khách vui lòng:</p>
        <ol>
          <li>Đăng nhập vào tài khoản BookingCare</li>
          <li>Vào phần <strong>""Tài khoản ngân hàng""</strong></li>
          <li>Kiểm tra và <strong>cập nhật lại thông tin chính xác</strong></li>
          <li>Đảm bảo thông tin tài khoản đúng với tên chủ tài khoản</li>
        </ol>
        <p><strong>📞 Hoặc liên hệ trực tiếp:</strong> Nếu cần hỗ trợ, vui lòng gọi hotline: <strong>1900-xxxx</strong></p>
      </div>
      
      <p class=""muted"">Sau khi quý khách cập nhật thông tin chính xác, chúng tôi sẽ tiến hành hoàn tiền trong vòng <strong>2-3 ngày làm việc</strong>.</p>
      <p class=""muted"">Chúng tôi xin lỗi vì sự bất tiện này và mong nhận được sự hợp tác của quý khách.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for no refund case (0% refund due to late cancellation)
    /// </summary>
    public static string BuildNoRefundEmailHtml(string patientName, DateTime appointmentDate, string cancellationReason)
    {
        var appointmentDateStr = appointmentDate.ToString(DateTimeFormat);

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Thông báo hủy lịch hẹn</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background: #f9f9f9; padding: 30px; border-radius: 10px; }}
        .header {{ background: #dc3545; color: white; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 20px; }}
        .content {{ background: white; padding: 25px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }}
        .alert {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .info-box {{ background: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning-icon {{ color: #dc3545; font-size: 18px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>🚫 Lịch hẹn đã được hủy</h2>
        </div>
        
        <div class=""content"">
            <p>Xin chào <strong>{patientName}</strong>,</p>
            
            <p>Chúng tôi xin thông báo rằng lịch hẹn của bạn đã được hủy thành công.</p>
            
            <div class=""info-box"">
                <p><strong>📅 Thời gian hẹn:</strong> {appointmentDateStr}</p>
                <p><strong>📝 Lý do hủy:</strong> {cancellationReason}</p>
            </div>
            
            <div class=""alert"">
                <p><span class=""warning-icon"">⚠️</span> <strong>Thông báo về chính sách hoàn tiền:</strong></p>
                <p>Do lịch hẹn được hủy trong vòng 12 giờ trước thời gian hẹn, theo chính sách của chúng tôi, <strong>không có khoản hoàn tiền nào được áp dụng</strong>.</p>
                <p>Chúng tôi hiểu rằng điều này có thể gây bất tiện và chân thành xin lỗi về sự bất tiện này.</p>
            </div>
            
            <p>Nếu bạn có bất kỳ thắc mắc nào về chính sách hoàn tiền hoặc cần hỗ trợ thêm, vui lòng liên hệ với chúng tôi.</p>
            
            <p>Trân trọng,<br/>
            <strong>Đội ngũ BookingCare</strong></p>
        </div>
        
        <div class=""footer"">
            Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for successful appointment cancellation without payment
    /// Used when appointment is cancelled but no payment record exists
    /// </summary>
    public static string BuildCancellationSuccessEmailHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        string? doctorName = null,
        string? hospitalName = null)
    {
        var appointmentDateStr = appointmentDate.ToString(DateTimeFormat);

        var doctorInfoHtml = !string.IsNullOrEmpty(doctorName)
            ? $"<p><strong>&#x1F468;&#x200D;&#x2695;&#xFE0F; Bác sĩ:</strong> {doctorName}</p>"
            : "";

        var hospitalInfoHtml = !string.IsNullOrEmpty(hospitalName)
            ? $"<p><strong>🏥 Bệnh viện:</strong> {hospitalName}</p>"
            : "";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Thông báo hủy lịch hẹn thành công</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .container {{ background: #f9f9f9; padding: 30px; border-radius: 10px; }}
        .header {{ background: #22c55e; color: white; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 20px; }}
        .content {{ background: white; padding: 25px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }}
        .info-box {{ background: #f0fdf4; padding: 15px; border-left: 4px solid #22c55e; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .success-icon {{ color: #22c55e; font-size: 18px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>✅ Hủy lịch hẹn thành công</h2>
        </div>
        
        <div class=""content"">
            <p>Xin chào <strong>{patientName}</strong>,</p>
            
            <p>Chúng tôi xin xác nhận rằng lịch hẹn của bạn đã được hủy thành công.</p>
            
            <div class=""info-box"">
                <p><strong>📅 Thời gian hẹn đã hủy:</strong> {appointmentDateStr}</p>
                {doctorInfoHtml}
                {hospitalInfoHtml}
                <p><strong>📝 Lý do hủy:</strong> {cancellationReason}</p>
            </div>
            
            <p>Nếu bạn muốn đặt lịch hẹn mới, vui lòng truy cập website của chúng tôi hoặc liên hệ trực tiếp với chúng tôi.</p>
            
            <p>Chúng tôi mong được phục vụ bạn trong tương lai!</p>
            
            <p>Trân trọng,<br/>
            <strong>Đội ngũ BookingCare</strong></p>
        </div>
        
        <div class=""footer"">
            Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for successful appointment booking
    /// Fixed SonarQube issue: Reduced from 10 parameters to 1 parameter object
    /// Updated to use pure composition pattern with AppointmentData
    /// </summary>
    public static string BuildAppointmentBookedSuccessEmailHtml(AppointmentBookingEmailData emailData)
    {
        var appointmentData = emailData.AppointmentData; // Direct access to avoid repetition

        var doctorInfoHtml = "";
        if (!string.IsNullOrEmpty(appointmentData.DoctorName))
        {
            doctorInfoHtml = $@"
        <div class=""info-item""><strong>Bác sĩ:</strong> {appointmentData.DoctorName}</div>";
            if (!string.IsNullOrEmpty(appointmentData.DoctorSpecialty))
            {
                doctorInfoHtml += $@"
        <div class=""info-item""><strong>Chuyên khoa:</strong> {appointmentData.DoctorSpecialty}</div>";
            }
        }

        var hospitalInfoHtml = "";
        if (!string.IsNullOrEmpty(appointmentData.HospitalName))
        {
            hospitalInfoHtml = $@"
        <div class=""info-item""><strong>Bệnh viện:</strong> {appointmentData.HospitalName}</div>";
            if (!string.IsNullOrEmpty(appointmentData.HospitalAddress))
            {
                hospitalInfoHtml += $@"
        <div class=""info-item""><strong>Địa chỉ:</strong> {appointmentData.HospitalAddress}</div>";
            }
        }

        var serviceInfoHtml = "";
        if (!string.IsNullOrEmpty(appointmentData.ServiceName))
        {
            serviceInfoHtml = $@"
        <div class=""info-item""><strong>Dịch vụ:</strong> {appointmentData.ServiceName}</div>";
        }

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Đặt lịch hẹn thành công - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .success-box strong {{ color:#047857; }}
    .info-box {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .info-box strong {{ color:#0369a1; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:20px; font-weight:700; color:#10b981; }}
    .check-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .reminder {{ background:#fff7ed; border:1px solid #fed7aa; border-radius:8px; padding:16px; margin:20px 0; }}
    .reminder strong {{ color:#c2410c; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Đặt lịch hẹn thành công</div>
    </div>
    <div class=""content"">
      <div class=""check-icon"">✅</div>
      <p class=""greeting"">Kính gửi {appointmentData.PatientName},</p>
      <p class=""lead"">Chúc mừng! Lịch hẹn của quý khách đã được đặt thành công và thanh toán hoàn tất.</p>
      
      <div class=""success-box"">
        <p><strong>🎉 Thông tin lịch hẹn:</strong></p>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {appointmentData.AppointmentDate:dd/MM/yyyy}</div>
        <div class=""info-item""><strong>Thời gian:</strong> {appointmentData.AppointmentTime}</div>
        <div class=""info-item""><strong>Loại hẹn:</strong> {appointmentData.AppointmentType}</div>{doctorInfoHtml}{serviceInfoHtml}
        <div class=""info-item""><strong>Số tiền đã thanh toán:</strong> <span class=""amount"">{appointmentData.Amount:N0} VNĐ</span></div>
      </div>
      
      <div class=""info-box"">
        <p><strong>🏥 Địa điểm khám:</strong></p>{hospitalInfoHtml}
      </div>
      
      <div class=""reminder"">
        <p><strong>📋 Lưu ý quan trọng:</strong></p>
        <ul style=""margin:8px 0; padding-left:20px;"">
          <li>Vui lòng có mặt <strong>15 phút trước</strong> giờ hẹn</li>
          <li>Mang theo giấy tờ tùy thân (CMND/CCCD/Hộ chiếu)</li>
          <li>Mang theo sổ bảo hiểm y tế (nếu có)</li>
          <li>Chuẩn bị các kết quả xét nghiệm, chẩn đoán hình ảnh liên quan (nếu có)</li>
          <li>Nếu cần hủy lịch hẹn, vui lòng thông báo trước <strong>24 giờ</strong></li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> 1900-xxxx<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Cảm ơn quý khách đã tin tương và sử dụng dịch vụ BookingCare.<br/><br/>
      Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for staff-initiated cancellation with reschedule options
    /// Patient can choose from 4 options: reschedule same doctor, confirm new doctor, choose new doctor, or request refund
    /// </summary>
    public static string BuildCancellationWithOptionsEmailHtml(CancellationWithOptionsEmailData data)
    {
        var doctorInfo = !string.IsNullOrEmpty(data.DoctorName) ? $" với bác sĩ <strong>{data.DoctorName}</strong>" : "";
        var hospitalInfo = !string.IsNullOrEmpty(data.HospitalName) ? $" tại <strong>{data.HospitalName}</strong>" : "";

        var refundInfo = "";
        if (data.PotentialRefundAmount.HasValue && data.PotentialRefundPercentage.HasValue)
        {
            refundInfo = $@"
        <div class=""refund-info"">
            <p>💰 <strong>Thông tin hoàn tiền (nếu chọn Option 4):</strong></p>
            <div class=""info-item"">Tỷ lệ hoàn: <strong>{data.PotentialRefundPercentage:N0}%</strong></div>
            <div class=""info-item"">Số tiền ước tính: <strong>{data.PotentialRefundAmount:N0} VNĐ</strong></div>
        </div>";
        }

        var expiryInfo = data.TokenExpiry.HasValue
            ? $"<p class=\"warning\">⏰ <strong>Lưu ý:</strong> Các tùy chọn đổi lịch có hiệu lực đến <strong>{data.TokenExpiry.Value.ToString(DateTimeFormat)}</strong> (trước ngày hẹn gốc)</p>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Lịch hẹn đã bị hủy - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:12px; color:#222; }}
    .card {{ max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#f59e0b; color:#fff; padding:16px 20px; }}
    .brand {{ font-size:16px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:20px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 16px; color:#444; line-height:1.5; }}
    .cancel-box {{ background:#fef2f2; border:1px solid #fecaca; border-radius:8px; padding:12px; margin:16px 0; }}
    .cancel-box strong {{ color:#991b1b; }}
    .info-item {{ margin:6px 0; font-size:14px; word-break:break-word; }}
    .refund-info {{ background:#f0fdf4; border:1px solid #86efac; border-radius:8px; padding:12px; margin:16px 0; }}
    .refund-info strong {{ color:#166534; }}
    .options-box {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:16px 0; }}
    .option-button {{ display:block; width:100%; max-width:100%; padding:12px 16px; margin:8px 0; background:#0ea5e9; color:#fff; text-decoration:none; border-radius:6px; font-weight:600; text-align:center; transition:background 0.3s; font-size:14px; word-wrap:break-word; overflow-wrap:break-word; box-sizing:border-box; }}
    .option-button:hover {{ background:#0284c7; }}
    .option-button.secondary {{ background:#8b5cf6; }}
    .option-button.secondary:hover {{ background:#7c3aed; }}
    .option-button.tertiary {{ background:#10b981; }}
    .option-button.tertiary:hover {{ background:#059669; }}
    .option-button.danger {{ background:#ef4444; }}
    .option-button.danger:hover {{ background:#dc2626; }}
    .option-desc {{ font-size:12px; color:#6b7280; margin:4px 0 12px; text-align:center; line-height:1.4; word-break:break-word; }}
    .warning {{ background:#fff7ed; border:1px solid #fed7aa; padding:10px; border-radius:6px; margin:12px 0; color:#c2410c; font-size:13px; word-break:break-word; }}
    .muted {{ margin-top:12px; color:#6b7280; font-size:12px; word-break:break-word; }}
    .divider {{ height:1px; background:#f1f5f9; margin:16px 0; }}
    .footer {{ padding:12px 20px 16px; color:#6b7280; font-size:11px; text-align:center; word-break:break-word; }}
    
    /* Mobile responsive */
    @media only screen and (max-width: 600px) {{
      body {{ padding:8px; }}
      .content {{ padding:16px; }}
      .header {{ padding:12px 16px; }}
      .brand {{ font-size:14px; }}
      .option-button {{ padding:10px 12px; font-size:13px; }}
      .option-desc {{ font-size:11px; }}
      .info-item {{ font-size:13px; }}
    }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Thông báo hủy lịch hẹn</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi {data.PatientName},</p>
      <p class=""lead"">Chúng tôi rất tiếc phải thông báo rằng lịch hẹn của quý khách{doctorInfo}{hospitalInfo} đã bị hủy bởi bệnh viện.</p>
      
      <div class=""cancel-box"">
        <p><strong>📅 Thông tin lịch hẹn bị hủy:</strong></p>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {data.AppointmentDate.ToString(DateTimeFormat)}</div>
        <div class=""info-item""><strong>Lý do hủy:</strong> {data.CancellationReason}</div>
      </div>
      {refundInfo}
      
      <div class=""options-box"">
        <p style=""text-align:center; font-size:16px; font-weight:600; margin-bottom:20px; color:#0369a1;"">
          🔄 VUI LÒNG CHỌN PHƯƠNG ÁN XỬ LÝ
        </p>
        
        {(!string.IsNullOrEmpty(data.SameDoctorRescheduleUrl) ? $@"
        <a href=""{data.SameDoctorRescheduleUrl}"" class=""option-button"">
          📆 Option: Đổi lịch với cùng bác sĩ
        </a>
        <p class=""option-desc"">Chọn ngày giờ khác với bác sĩ {data.DoctorName}</p>" : "")}
        
        {(!string.IsNullOrEmpty(data.ConfirmNewDoctorUrl) ? $@"
        <a href=""{data.ConfirmNewDoctorUrl}"" class=""option-button secondary"">
          👨‍⚕️ Option: Xác nhận bác sĩ mới (do bệnh viện chỉ định)
        </a>
        <p class=""option-desc"">Bệnh viện chỉ định bác sĩ thay thế</p>" : "")}
        
        {(!string.IsNullOrEmpty(data.ChooseNewDoctorUrl) ? $@"
        <a href=""{data.ChooseNewDoctorUrl}"" class=""option-button tertiary"">
          🔍 Option 3: Tự chọn bác sĩ mới
        </a>
        <p class=""option-desc"">Tự chọn bác sĩ khác cùng chuyên khoa</p>" : "")}
        
        {(!string.IsNullOrEmpty(data.RefundRequestUrl) ? $@"
        <a href=""{data.RefundRequestUrl}"" class=""option-button danger"">
          💰 Option: Yêu cầu hoàn tiền
        </a>
        <p class=""option-desc"">Không muốn đổi lịch, xin hoàn tiền</p>" : "")}
      </div>
      
      {expiryInfo}
      
      <p class=""muted"">Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> 1900-xxxx<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Chúng tôi chân thành xin lỗi vì sự bất tiện này và hy vọng quý khách sẽ tiếp tục tin tưởng sử dụng dịch vụ của BookingCare.<br/><br/>
      Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for doctor change refund (patient has bank account - PENDING status)
    /// Used when patient chooses new doctor with lower deposit price (Option 3: Lower price scenario)
    /// </summary>
    public static string BuildDoctorChangeRefundEmailWithBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string originalDoctorName,
        decimal originalFee,
        string newDoctorName,
        decimal newFee,
        decimal refundAmount)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận thay đổi bác sĩ - Hoàn tiền chênh lệch - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .change-box {{ background:#e0f2fe; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .change-box strong {{ color:#0369a1; }}
    .doctor-change {{ display:flex; align-items:center; justify-content:space-between; margin:16px 0; }}
    .doctor-item {{ flex:1; text-align:center; }}
    .doctor-name {{ font-weight:700; color:#0c4a6e; margin:8px 0; }}
    .doctor-price {{ color:#6b7280; font-size:14px; }}
    .arrow {{ font-size:24px; color:#10b981; margin:0 16px; }}
    .refund-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .refund-box strong {{ color:#047857; }}
    .amount {{ font-size:20px; font-weight:700; color:#10b981; }}
    .info-item {{ margin:8px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Xác nhận thay đổi bác sĩ</div>
    </div>
    <div class=""content"">
      <div class=""success-icon"">✅</div>
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi xác nhận rằng quý khách đã thay đổi bác sĩ khám thành công!</p>
      
      <div class=""change-box"">
        <p><strong>👨‍⚕️ Thông tin thay đổi:</strong></p>
        <div class=""doctor-change"">
          <div class=""doctor-item"">
            <p class=""muted"" style=""margin:0;"">Bác sĩ cũ</p>
            <p class=""doctor-name"">{originalDoctorName}</p>
            <p class=""doctor-price"">Cọc: {originalFee:N0} VNĐ</p>
          </div>
          <div class=""arrow"">→</div>
          <div class=""doctor-item"">
            <p class=""muted"" style=""margin:0;"">Bác sĩ mới</p>
            <p class=""doctor-name"">{newDoctorName}</p>
            <p class=""doctor-price"">Cọc: {newFee:N0} VNĐ</p>
          </div>
        </div>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy HH:mm}</div>
      </div>
      
      <div class=""refund-box"">
        <p><strong>💰 Hoàn tiền chênh lệch:</strong></p>
        <div class=""info-item"">Do bác sĩ mới có mức cọc thấp hơn, chúng tôi sẽ hoàn lại cho quý khách số tiền chênh lệch:</div>
        <div class=""info-item""><strong>Số tiền hoàn trả:</strong> <span class=""amount"">{refundAmount:N0} VNĐ</span></div>
        <div class=""info-item"">Tiền sẽ được chuyển vào tài khoản ngân hàng của quý khách trong vòng <strong>5-7 ngày làm việc</strong>.</div>
      </div>
      
      <p class=""muted"">Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hotline: <strong>1900-xxxx</strong>.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for doctor change refund (patient has NO bank account - WAITING status)
    /// Used when patient chooses new doctor with lower deposit price (Option 3: Lower price scenario)
    /// </summary>
    public static string BuildDoctorChangeRefundEmailNoBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string originalDoctorName,
        decimal originalFee,
        string newDoctorName,
        decimal newFee,
        decimal refundAmount)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận thay đổi bác sĩ - Cần cung cấp tài khoản - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#FF9800; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .change-box {{ background:#e0f2fe; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .change-box strong {{ color:#0369a1; }}
    .doctor-change {{ display:flex; align-items:center; justify-content:space-between; margin:16px 0; }}
    .doctor-item {{ flex:1; text-align:center; }}
    .doctor-name {{ font-weight:700; color:#0c4a6e; margin:8px 0; }}
    .doctor-price {{ color:#6b7280; font-size:14px; }}
    .arrow {{ font-size:24px; color:#10b981; margin:0 16px; }}
    .action-required {{ background:#ffebee; border:2px solid #ef5350; border-radius:8px; padding:16px; margin:20px 0; }}
    .action-required strong {{ color:#c62828; }}
    .action-required ol {{ margin:12px 0; padding-left:20px; }}
    .action-required li {{ margin:8px 0; }}
    .info-item {{ margin:8px 0; }}
    .amount {{ font-size:20px; font-weight:700; color:#f57c00; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Xác nhận thay đổi bác sĩ</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi {patientName},</p>
      <p class=""lead"">Chúng tôi xác nhận rằng quý khách đã thay đổi bác sĩ khám thành công!</p>
      
      <div class=""change-box"">
        <p><strong>👨‍⚕️ Thông tin thay đổi:</strong></p>
        <div class=""doctor-change"">
          <div class=""doctor-item"">
            <p class=""muted"" style=""margin:0;"">Bác sĩ cũ</p>
            <p class=""doctor-name"">{originalDoctorName}</p>
            <p class=""doctor-price"">Cọc: {originalFee:N0} VNĐ</p>
          </div>
          <div class=""arrow"">→</div>
          <div class=""doctor-item"">
            <p class=""muted"" style=""margin:0;"">Bác sĩ mới</p>
            <p class=""doctor-name"">{newDoctorName}</p>
            <p class=""doctor-price"">Cọc: {newFee:N0} VNĐ</p>
          </div>
        </div>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy HH:mm}</div>
      </div>
      
      <div class=""action-required"">
        <p><strong>⚠️ HÀNH ĐỘNG YÊU CẦU - VUI LÒNG ĐỌC KỸ:</strong></p>
        <p>Do bác sĩ mới có mức cọc thấp hơn, quý khách sẽ được hoàn lại <strong>{refundAmount:N0} VNĐ</strong>.</p>
        <p>Để nhận lại số tiền này, quý khách vui lòng:</p>
        <ol>
          <li>Đăng nhập vào tài khoản BookingCare</li>
          <li>Vào phần <strong>""Tài khoản ngân hàng""</strong> trong cài đặt</li>
          <li>Thêm thông tin tài khoản ngân hàng để nhận hoàn tiền</li>
        </ol>
        <p><strong>⏰ Lưu ý quan trọng:</strong> Nếu không cung cấp thông tin tài khoản ngân hàng, chúng tôi sẽ không thể hoàn trả tiền cho quý khách.</p>
      </div>
      
      <p class=""muted"">Sau khi cập nhật thông tin tài khoản ngân hàng, chúng tôi sẽ tiến hành hoàn tiền trong vòng <strong>5-7 ngày làm việc</strong>.</p>
      <p class=""muted"">Nếu quý khách có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua hotline: <strong>1900-xxxx</strong>.</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for successful hospital subscription registration
    /// </summary>
    public static string BuildHospitalSubscriptionCreatedEmailHtml(HospitalSubscriptionCreatedEmailData data)
    {
        var billingCycleDisplay = data.BillingCycle?.ToUpper() switch
        {
            "MONTHLY" => "Tháng",
            "QUARTERLY" => "Quý",
            "YEARLY" => "Năm",
            _ => "Tháng"
        };

        var maxDoctorsInfo = data.MaxDoctors.HasValue ? $@"
        <div class=""info-item""><strong>Số lượng bác sĩ tối đa:</strong> {data.MaxDoctors} bác sĩ</div>" : "";

        var maxAppointmentsInfo = data.MaxAppointmentsPerMonth.HasValue ? $@"
        <div class=""info-item""><strong>Số lượng lịch hẹn/tháng:</strong> {data.MaxAppointmentsPerMonth} lịch hẹn</div>" : "";

        var featuresInfo = BuildFeaturesHtml(data.Features);

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Đăng ký gói dịch vụ thành công - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .success-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .success-box strong {{ color:#047857; }}
    .info-item {{ margin:8px 0; }}
    .plan-price {{ font-size:24px; font-weight:700; color:#10b981; }}
    .reminder {{ background:#fff7ed; border:1px solid #fed7aa; border-radius:8px; padding:16px; margin:20px 0; }}
    .reminder strong {{ color:#c2410c; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Đăng ký gói dịch vụ thành công</div>
    </div>
    <div class=""content"">
      <div class=""success-icon"">🎉</div>
      <p class=""greeting"">Kính gửi {data.ContactPersonName},</p>
      <p class=""lead"">Chúc mừng! Bệnh viện <strong>{data.HospitalName}</strong> đã đăng ký gói dịch vụ <strong>{data.PlanName}</strong> thành công.</p>
      
      <div class=""success-box"">
        <p><strong>📦 Thông tin gói dịch vụ:</strong></p>
        <div class=""info-item""><strong>Tên gói:</strong> {data.PlanName}</div>
        <div class=""info-item""><strong>Chu kỳ thanh toán:</strong> {billingCycleDisplay}</div>
        <div class=""info-item""><strong>Giá gói:</strong> <span class=""plan-price"">{data.Price:N0} VNĐ/{billingCycleDisplay}</span></div>
        <div class=""info-item""><strong>Ngày bắt đầu:</strong> {data.StartDate:dd/MM/yyyy HH:mm}</div>
        <div class=""info-item""><strong>Ngày hết hạn:</strong> {data.EndDate:dd/MM/yyyy HH:mm}</div>{maxDoctorsInfo}{maxAppointmentsInfo}{featuresInfo}
      </div>
      
      <div class=""reminder"">
        <p><strong>📋 Lưu ý quan trọng:</strong></p>
        <ul style=""margin:8px 0; padding-left:20px;"">
          <li>Gói dịch vụ của quý bệnh viện đã được kích hoạt và có hiệu lực ngay</li>
          <li>Vui lòng quản lý số lượng bác sĩ và lịch hẹn theo giới hạn của gói đã đăng ký</li>
          <li>Để nâng cấp gói dịch vụ, vui lòng truy cập vào phần Quản lý Gói dịch vụ</li>
          <li>Trước khi hết hạn 7 ngày, hệ thống sẽ gửi thông báo nhắc nhở gia hạn</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu quý bệnh viện có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> 1900-xxxx<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Cảm ơn quý bệnh viện đã tin tưởng và sử dụng dịch vụ BookingCare.<br/><br/>
      Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for hospital partnership registration submission confirmation
    /// </summary>
    public static string BuildHospitalRegistrationSubmittedEmailHtml(
        string hospitalName,
        string email,
        string phone,
        string address,
        string taxCode)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận đăng ký hợp tác - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#0ea5e9; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .info-box {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .info-box strong {{ color:#0369a1; }}
    .info-item {{ margin:8px 0; }}
    .next-steps {{ background:#f0fdf4; border:1px solid #86efac; border-radius:8px; padding:16px; margin:20px 0; }}
    .next-steps strong {{ color:#166534; }}
    .next-steps ul {{ margin:8px 0; padding-left:20px; }}
    .next-steps li {{ margin:4px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Đăng ký hợp tác</div>
    </div>
    <div class=""content"">
      <div class=""success-icon"">✅</div>
      <p class=""greeting"">Kính gửi đại diện {hospitalName},</p>
      <p class=""lead"">Chúng tôi đã nhận được đơn đăng ký hợp tác của quý bệnh viện. Cảm ơn quý bệnh viện đã quan tâm đến nền tảng BookingCare!</p>
      
      <div class=""info-box"">
        <p><strong>📋 Thông tin đăng ký:</strong></p>
        <div class=""info-item""><strong>Tên bệnh viện:</strong> {hospitalName}</div>
        <div class=""info-item""><strong>Email:</strong> {email}</div>
        <div class=""info-item""><strong>Số điện thoại:</strong> {phone}</div>
        <div class=""info-item""><strong>Địa chỉ:</strong> {address}</div>
        <div class=""info-item""><strong>Mã số thuế:</strong> {taxCode}</div>
      </div>
      
      <div class=""next-steps"">
        <p><strong>📌 Các bước tiếp theo:</strong></p>
        <ul>
          <li>Đội ngũ của chúng tôi sẽ xem xét hồ sơ đăng ký trong vòng <strong>2-3 ngày làm việc</strong></li>
          <li>Chúng tôi sẽ kiểm tra tính xác thực của các tài liệu đã gửi</li>
          <li>Sau khi hoàn tất kiểm tra, chúng tôi sẽ liên hệ lại qua email hoặc điện thoại</li>
          <li>Nếu được chấp thuận, chúng tôi sẽ tiến hành ký kết hợp đồng hợp tác</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu quý bệnh viện có bất kỳ thắc mắc nào trong thời gian chờ đợi, vui lòng liên hệ với chúng tôi qua:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> +84 236 3 822 888<br/>
      <strong>📧 Email:</strong> partnership@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ Phát triển Đối tác - BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for hospital partnership registration approval
    /// </summary>
    public static string BuildHospitalRegistrationApprovedEmailHtml(
        string hospitalName,
        string? contractFileUrl)
    {
        var contractSection = !string.IsNullOrEmpty(contractFileUrl)
            ? $@"<p class=""lead"">Hợp đồng hợp tác đã được đính kèm. Quý bệnh viện có thể tải về tại đây:</p>
      <div style=""text-align:center; margin:20px 0;"">
        <a href=""{contractFileUrl}"" class=""download-button"">📄 Tải hợp đồng</a>
      </div>"
            : @"<p class=""lead"">Chúng tôi sẽ liên hệ trực tiếp để hoàn tất thủ tục ký kết hợp đồng.</p>";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Chúc mừng - Đăng ký được chấp thuận - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .approved-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .approved-box strong {{ color:#047857; }}
    .download-button {{ display:inline-block; padding:14px 28px; background:#0ea5e9; color:#fff; text-decoration:none; border-radius:8px; font-weight:600; font-size:16px; }}
    .download-button:hover {{ background:#0284c7; }}
    .next-steps {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .next-steps strong {{ color:#0369a1; }}
    .next-steps ul {{ margin:8px 0; padding-left:20px; }}
    .next-steps li {{ margin:4px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Chúc mừng đối tác mới</div>
    </div>
    <div class=""content"">
      <div class=""success-icon"">🎉</div>
      <p class=""greeting"">Kính gửi đại diện {hospitalName},</p>
      <p class=""lead""><strong>Chúc mừng!</strong> Đơn đăng ký hợp tác của quý bệnh viện đã được chấp thuận. Chúng tôi rất vui mừng được chào đón {hospitalName} trở thành đối tác của BookingCare!</p>
      
      <div class=""approved-box"">
        <p><strong>✅ Trạng thái: ĐÃ CHẤP THUẬN</strong></p>
        <p>Đơn đăng ký của quý bệnh viện đã vượt qua tất cả các bước kiểm tra và đánh giá của chúng tôi.</p>
      </div>
      
      {contractSection}
      
      <div class=""next-steps"">
        <p><strong>🚀 Các bước tiếp theo:</strong></p>
        <ul>
          <li>Đội ngũ của chúng tôi sẽ liên hệ trong vòng <strong>24 giờ</strong> để hướng dẫn chi tiết</li>
          <li>Thiết lập tài khoản quản trị cho bệnh viện</li>
          <li>Hướng dẫn sử dụng hệ thống quản lý</li>
          <li>Cung cấp tài liệu đào tạo và hỗ trợ kỹ thuật</li>
          <li>Triển khai chính thức trên nền tảng BookingCare</li>
        </ul>
      </div>
      
      <p class=""lead"">Chúng tôi mong muốn được hợp tác lâu dài và cùng phát triển với {hospitalName}!</p>
      
      <p class=""muted"">Nếu có bất kỳ câu hỏi nào, vui lòng liên hệ:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> +84 236 3 822 888<br/>
      <strong>📧 Email:</strong> partnership@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ Phát triển Đối tác - BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for successful hospital subscription upgrade
    /// </summary>
    public static string BuildHospitalSubscriptionUpgradedEmailHtml(HospitalSubscriptionUpgradedEmailData data)
    {
        var previousBillingCycleDisplay = data.PreviousBillingCycle?.ToUpper() switch
        {
            "MONTHLY" => "Tháng",
            "QUARTERLY" => "Quý",
            "YEARLY" => "Năm",
            _ => "Tháng"
        };

        var newBillingCycleDisplay = data.NewBillingCycle?.ToUpper() switch
        {
            "MONTHLY" => "Tháng",
            "QUARTERLY" => "Quý",
            "YEARLY" => "Năm",
            _ => "Tháng"
        };

        var bonusDaysInfo = data.BonusDays > 0 ? $@"
        <div class=""info-item""><strong>Số ngày thưởng (từ gói cũ):</strong> {data.BonusDays:N0} ngày</div>" : "";

        var maxDoctorsInfo = data.NewMaxDoctors.HasValue ? $@"
        <div class=""info-item""><strong>Số lượng bác sĩ tối đa:</strong> {data.NewMaxDoctors} bác sĩ</div>" : "";

        var maxAppointmentsInfo = data.NewMaxAppointmentsPerMonth.HasValue ? $@"
        <div class=""info-item""><strong>Số lượng lịch hẹn/tháng:</strong> {data.NewMaxAppointmentsPerMonth} lịch hẹn</div>" : "";

        var featuresInfo = BuildFeaturesHtml(data.NewFeatures);

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Nâng cấp gói dịch vụ thành công - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#8b5cf6; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-icon {{ font-size:48px; text-align:center; margin:16px 0; }}
    .comparison-box {{ background:#f3f4f6; border:1px solid #d1d5db; border-radius:8px; padding:16px; margin:20px 0; }}
    .comparison-box strong {{ color:#6b7280; }}
    .upgrade-arrow {{ text-align:center; font-size:28px; color:#8b5cf6; margin:12px 0; }}
    .new-plan-box {{ background:#ede9fe; border:1px solid #c4b5fd; border-radius:8px; padding:16px; margin:20px 0; }}
    .new-plan-box strong {{ color:#6d28d9; }}
    .info-item {{ margin:8px 0; }}
    .plan-price {{ font-size:24px; font-weight:700; color:#8b5cf6; }}
    .bonus-highlight {{ background:#fef3c7; border:1px solid #fcd34d; border-radius:8px; padding:16px; margin:20px 0; }}
    .bonus-highlight strong {{ color:#92400e; }}
    .reminder {{ background:#f0fdf4; border:1px solid #86efac; border-radius:8px; padding:16px; margin:20px 0; }}
    .reminder strong {{ color:#166534; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Nâng cấp gói dịch vụ thành công</div>
    </div>
    <div class=""content"">
      <div class=""success-icon"">🚀</div>
      <p class=""greeting"">Kính gửi {data.ContactPersonName},</p>
      <p class=""lead"">Chúc mừng! Bệnh viện <strong>{data.HospitalName}</strong> đã nâng cấp gói dịch vụ thành công.</p>
      
      <div class=""comparison-box"">
        <p><strong>📦 Gói dịch vụ trước đây:</strong></p>
        <div class=""info-item""><strong>Tên gói:</strong> {data.PreviousPlanName}</div>
        <div class=""info-item""><strong>Chu kỳ:</strong> {previousBillingCycleDisplay}</div>
        <div class=""info-item""><strong>Giá:</strong> {data.PreviousPrice:N0} VNĐ/{previousBillingCycleDisplay}</div>
      </div>
      
      <div class=""upgrade-arrow"">⬇️</div>
      
      <div class=""new-plan-box"">
        <p><strong>🎁 Gói dịch vụ mới:</strong></p>
        <div class=""info-item""><strong>Tên gói:</strong> {data.NewPlanName}</div>
        <div class=""info-item""><strong>Chu kỳ thanh toán:</strong> {newBillingCycleDisplay}</div>
        <div class=""info-item""><strong>Giá gói:</strong> <span class=""plan-price"">{data.NewPrice:N0} VNĐ/{newBillingCycleDisplay}</span></div>
        <div class=""info-item""><strong>Ngày bắt đầu:</strong> {data.NewStartDate:dd/MM/yyyy HH:mm}</div>
        <div class=""info-item""><strong>Ngày hết hạn:</strong> {data.NewEndDate:dd/MM/yyyy HH:mm}</div>{bonusDaysInfo}{maxDoctorsInfo}{maxAppointmentsInfo}{featuresInfo}
      </div>
      
      {(data.BonusDays > 0 ? $@"<div class=""bonus-highlight"">
        <p><strong>🎉 Ưu đãi đặc biệt:</strong></p>
        <p>Quý bệnh viện được cộng thêm <strong>{data.BonusDays:N0} ngày</strong> miễn phí từ giá trị còn lại của gói cũ!</p>
      </div>" : "")}
      
      <div class=""reminder"">
        <p><strong>✨ Quyền lợi mới:</strong></p>
        <ul style=""margin:8px 0; padding-left:20px;"">
          <li>Gói dịch vụ mới đã được kích hoạt ngay lập tức</li>
          <li>Toàn bộ tính năng và giới hạn mới đã có hiệu lực</li>
          <li>Giá trị còn lại của gói cũ đã được quy đổi thành ngày thưởng</li>
          <li>Hệ thống sẽ nhắc nhở gia hạn trước khi hết hạn 7 ngày</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu quý bệnh viện có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> 1900-xxxx<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Cảm ơn quý bệnh viện đã tin tưởng và sử dụng dịch vụ BookingCare.<br/><br/>
      Trân trọng,<br/>Đội ngũ BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for hospital partnership registration rejection
    /// </summary>
    public static string BuildHospitalRegistrationRejectedEmailHtml(
        string hospitalName,
        string reason)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Thông báo về đơn đăng ký hợp tác - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#ef4444; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .rejected-box {{ background:#fee2e2; border:1px solid #fecaca; border-radius:8px; padding:16px; margin:20px 0; }}
    .rejected-box strong {{ color:#dc2626; }}
    .reason-box {{ background:#fef3c7; border:1px solid #fcd34d; border-radius:8px; padding:16px; margin:20px 0; }}
    .reason-box strong {{ color:#92400e; }}
    .reapply-box {{ background:#f0f9ff; border:1px solid #7dd3fc; border-radius:8px; padding:16px; margin:20px 0; }}
    .reapply-box strong {{ color:#0369a1; }}
    .reapply-box ul {{ margin:8px 0; padding-left:20px; }}
    .reapply-box li {{ margin:4px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Thông báo đơn đăng ký</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi đại diện {hospitalName},</p>
      <p class=""lead"">Cảm ơn quý bệnh viện đã quan tâm và gửi đơn đăng ký hợp tác với BookingCare. Sau khi xem xét kỹ lưỡng, chúng tôi rất tiếc phải thông báo rằng đơn đăng ký của quý bệnh viện chưa được chấp thuận trong thời điểm này.</p>
      
      <div class=""rejected-box"">
        <p><strong>❌ Trạng thái: CHƯA CHẤP THUẬN</strong></p>
      </div>
      
      <div class=""reason-box"">
        <p><strong>📝 Lý do:</strong></p>
        <p>{reason}</p>
      </div>
      
      <div class=""reapply-box"">
        <p><strong>🔄 Đăng ký lại:</strong></p>
        <p>Quý bệnh viện có thể đăng ký lại sau khi:</p>
        <ul>
          <li>Khắc phục các vấn đề được nêu trong phần lý do</li>
          <li>Chuẩn bị đầy đủ các tài liệu cần thiết theo yêu cầu</li>
          <li>Liên hệ với chúng tôi để được tư vấn thêm</li>
        </ul>
        <p>Chúng tôi luôn chào đón quý bệnh viện nộp đơn đăng ký lại khi đã đáp ứng đầy đủ các tiêu chí.</p>
      </div>
      
      <p class=""muted"">Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ thêm, vui lòng liên hệ:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> +84 236 3 822 888<br/>
      <strong>📧 Email:</strong> partnership@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ Phát triển Đối tác - BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Build email content for hospital account credentials
    /// </summary>
    public static string BuildHospitalAccountCredentialsEmailHtml(
        string hospitalName,
        string email,
        string generatedPassword,
        string loginUrl,
        string? contractFileUrl)
    {
        var contractSection = !string.IsNullOrEmpty(contractFileUrl)
            ? $@"<div class=""contract-box"">
        <p><strong>📄 Hợp đồng hợp tác:</strong></p>
        <p>Hợp đồng hợp tác đã được đính kèm. Quý bệnh viện có thể tải về tại đây:</p>
        <div style=""text-align:center; margin:12px 0;"">
          <a href=""{contractFileUrl}"" class=""download-button"">📄 Tải hợp đồng</a>
        </div>
      </div>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Thông tin tài khoản bệnh viện - BookingCare</title>
  <style>
    body {{ font-family: Arial, Helvetica, sans-serif; background:#f6f7fb; margin:0; padding:24px; color:#222; }}
    .card {{ max-width:560px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 16px rgba(0,0,0,0.06); overflow:hidden; }}
    .header {{ background:#10b981; color:#fff; padding:20px 24px; }}
    .brand {{ font-size:18px; font-weight:600; letter-spacing:0.3px; }}
    .content {{ padding:24px; }}
    .greeting {{ margin:0 0 12px; font-size:16px; }}
    .lead {{ margin:0 0 20px; color:#444; line-height:1.6; }}
    .success-box {{ background:#d1fae5; border:1px solid #6ee7b7; border-radius:8px; padding:16px; margin:20px 0; }}
    .success-box strong {{ color:#047857; }}
    .credentials-box {{ background:#eff6ff; border:1px solid #93c5fd; border-radius:8px; padding:16px; margin:20px 0; }}
    .credentials-box strong {{ color:#1e40af; }}
    .credentials-box p {{ margin:8px 0; }}
    .contract-box {{ background:#fef3c7; border:1px solid #fcd34d; border-radius:8px; padding:16px; margin:20px 0; }}
    .contract-box strong {{ color:#92400e; }}
    .download-button {{ display:inline-block; background:#10b981; color:#fff; padding:10px 24px; border-radius:6px; text-decoration:none; font-weight:500; }}
    .login-button {{ display:inline-block; background:#3b82f6; color:#fff; padding:12px 28px; border-radius:6px; text-decoration:none; font-weight:500; margin:20px 0; }}
    .warning-box {{ background:#fef3c7; border:1px solid #fcd34d; border-radius:8px; padding:16px; margin:20px 0; }}
    .warning-box strong {{ color:#92400e; }}
    .warning-box ul {{ margin:8px 0; padding-left:20px; }}
    .warning-box li {{ margin:4px 0; }}
    .muted {{ margin-top:16px; color:#6b7280; font-size:13px; }}
    .divider {{ height:1px; background:#f1f5f9; margin:24px 0; }}
    .footer {{ padding:16px 24px 24px; color:#6b7280; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""header"">
      <div class=""brand"">BookingCare - Tài khoản bệnh viện</div>
    </div>
    <div class=""content"">
      <p class=""greeting"">Kính gửi đại diện {hospitalName},</p>
      <p class=""lead"">Chúng tôi rất vui mừng thông báo rằng đơn đăng ký hợp tác của quý bệnh viện với BookingCare đã được <strong>PHÊ DUYỆT</strong>. Tài khoản quản lý bệnh viện đã được tạo thành công!</p>
      
      <div class=""success-box"">
        <p><strong>✅ Trạng thái: ĐÃ PHÊ DUYỆT</strong></p>
        <p>Tài khoản của quý bệnh viện đã sẵn sàng để sử dụng.</p>
      </div>
      
      <div class=""credentials-box"">
        <p><strong>🔐 Thông tin đăng nhập:</strong></p>
        <p><strong>Email:</strong> {email}</p>
        <p><strong>Mật khẩu tạm thời:</strong> {generatedPassword}</p>
        <p style=""color:#dc2626; font-size:13px; margin-top:12px;"">⚠️ Đây là mật khẩu tạm thời. Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu.</p>
      </div>
      
      {contractSection}
      
      <div style=""text-align:center;"">
        <a href=""{loginUrl}"" class=""login-button"">🚀 Đăng nhập ngay</a>
      </div>
      
      <div class=""warning-box"">
        <p><strong>📌 Lưu ý quan trọng:</strong></p>
        <ul>
          <li>Vui lòng bảo mật thông tin đăng nhập</li>
          <li>Đổi mật khẩu ngay sau khi đăng nhập lần đầu</li>
          <li>Không chia sẻ thông tin tài khoản với người không có thẩm quyền</li>
          <li>Liên hệ với chúng tôi nếu gặp bất kỳ vấn đề nào</li>
        </ul>
      </div>
      
      <p class=""muted"">Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ thêm, vui lòng liên hệ:</p>
      <p class=""muted""><strong>📞 Hotline:</strong> +84 236 3 822 888<br/>
      <strong>📧 Email:</strong> support@bookingcare.vn</p>
      
      <div class=""divider""></div>
      <p class=""muted"">Trân trọng,<br/>Đội ngũ Phát triển Đối tác - BookingCare</p>
    </div>
    <div class=""footer"">Email này được gửi tự động từ hệ thống BookingCare. Vui lòng không trả lời email này.</div>
  </div>
</body>
</html>";
    }
}

