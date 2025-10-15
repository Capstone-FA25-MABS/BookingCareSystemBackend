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
    /// Build email content for successful appointment booking
    /// Fixed SonarQube issue: Reduced from 10 parameters to 1 parameter object
    /// </summary>
    public static string BuildAppointmentBookedSuccessEmailHtml(AppointmentBookingEmailData emailData)
    {
        var doctorInfoHtml = "";
        if (!string.IsNullOrEmpty(emailData.DoctorName))
        {
            doctorInfoHtml = $@"
        <div class=""info-item""><strong>Bác sĩ:</strong> {emailData.DoctorName}</div>";
            if (!string.IsNullOrEmpty(emailData.DoctorSpecialty))
            {
                doctorInfoHtml += $@"
        <div class=""info-item""><strong>Chuyên khoa:</strong> {emailData.DoctorSpecialty}</div>";
            }
        }

        var hospitalInfoHtml = "";
        if (!string.IsNullOrEmpty(emailData.HospitalName))
        {
            hospitalInfoHtml = $@"
        <div class=""info-item""><strong>Bệnh viện:</strong> {emailData.HospitalName}</div>";
            if (!string.IsNullOrEmpty(emailData.HospitalAddress))
            {
                hospitalInfoHtml += $@"
        <div class=""info-item""><strong>Địa chỉ:</strong> {emailData.HospitalAddress}</div>";
            }
        }

        var serviceInfoHtml = "";
        if (!string.IsNullOrEmpty(emailData.ServiceName))
        {
            serviceInfoHtml = $@"
        <div class=""info-item""><strong>Dịch vụ:</strong> {emailData.ServiceName}</div>";
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
      <p class=""greeting"">Kính gửi {emailData.PatientName},</p>
      <p class=""lead"">Chúc mừng! Lịch hẹn của quý khách đã được đặt thành công và thanh toán hoàn tất.</p>
      
      <div class=""success-box"">
        <p><strong>🎉 Thông tin lịch hẹn:</strong></p>
        <div class=""info-item""><strong>Ngày hẹn:</strong> {emailData.AppointmentDate:dd/MM/yyyy}</div>
        <div class=""info-item""><strong>Thời gian:</strong> {emailData.AppointmentTime}</div>
        <div class=""info-item""><strong>Loại hẹn:</strong> {emailData.AppointmentType}</div>{doctorInfoHtml}{serviceInfoHtml}
        <div class=""info-item""><strong>Số tiền đã thanh toán:</strong> <span class=""amount"">{emailData.Amount:N0} VNĐ</span></div>
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
}

