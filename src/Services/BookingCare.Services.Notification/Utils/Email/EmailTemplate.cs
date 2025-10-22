using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Utils.Email;

public static class EmailTemplate
{
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
        var appointmentDateStr = appointmentDate.ToString("dd/MM/yyyy HH:mm");

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
            ? $"<p class=\"warning\">⏰ <strong>Lưu ý:</strong> Các tùy chọn đổi lịch có hiệu lực đến <strong>{data.TokenExpiry.Value.ToString("dd/MM/yyyy HH:mm")}</strong> (trước ngày hẹn gốc)</p>"
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
        <div class=""info-item""><strong>Ngày hẹn:</strong> {data.AppointmentDate.ToString("dd/MM/yyyy HH:mm")}</div>
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
}

