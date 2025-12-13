using System.Text.Json;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Utils.Email;

public static class EmailTemplate
{
    // Brand constants
    private const string BRAND_NAME = "MedCure";
    private const string HOTLINE = "19001979";
    private const string SUPPORT_EMAIL = "medcure.contact@gmail.com";

    // Color scheme - Professional medical theme
    private const string PRIMARY_COLOR = "#0d9488";      // Teal - main brand color
    private const string PRIMARY_DARK = "#0f766e";       // Darker teal
    private const string SUCCESS_COLOR = "#059669";      // Green
    private const string WARNING_COLOR = "#d97706";      // Amber
    private const string DANGER_COLOR = "#dc2626";       // Red
    private const string TEXT_PRIMARY = "#1f2937";       // Dark gray
    private const string TEXT_SECONDARY = "#6b7280";     // Medium gray
    private const string BG_LIGHT = "#f9fafb";           // Light gray background
    private const string BORDER_COLOR = "#e5e7eb";       // Border gray

    // Background colors for info boxes
    private const string BG_TEAL_LIGHT = "#f0fdfa";      // Light teal background
    private const string BG_RED_LIGHT = "#fef2f2";       // Light red background
    private const string BG_AMBER_LIGHT = "#fef3c7";     // Light amber background
    private const string BG_GREEN_LIGHT = "#f0fdf4";     // Light green background

    // URLs
    private const string BOOKING_URL = "https://medcure.vn/booking";
    private const string HOME_URL = "https://medcure.vn";

    // Date format constants
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    private const string DateFormat = "dd/MM/yyyy";

    /// <summary>
    /// Common email wrapper with consistent styling
    /// </summary>
    private static string WrapEmailContent(string headerBgColor, string headerTitle, string bodyContent)
    {
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{headerTitle} - {BRAND_NAME}</title>
</head>
<body style=""margin:0; padding:0; font-family:'Segoe UI',Arial,sans-serif; background:{BG_LIGHT}; color:{TEXT_PRIMARY};"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:{BG_LIGHT}; padding:32px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 1px 3px rgba(0,0,0,0.1);"">
          <tr>
            <td style=""background:{headerBgColor}; padding:24px 32px;"">
              <h1 style=""margin:0; color:#ffffff; font-size:20px; font-weight:600;"">{headerTitle}</h1>
              <p style=""margin:8px 0 0; color:rgba(255,255,255,0.9); font-size:13px;"">{BRAND_NAME}</p>
            </td>
          </tr>
          <tr>
            <td style=""padding:32px;"">
              {bodyContent}
            </td>
          </tr>
          <tr>
            <td style=""padding:20px 32px; background:{BG_LIGHT}; border-top:1px solid {BORDER_COLOR};"">
              <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:12px; text-align:center;"">
                Email tự động từ {BRAND_NAME}. Vui lòng không trả lời email này.<br/>
                Hotline: {HOTLINE} | Email: {SUPPORT_EMAIL}
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    /// <summary>
    /// Build info box HTML
    /// </summary>
    private static string BuildInfoBox(string bgColor, string borderColor, string content)
    {
        return $@"<div style=""background:{bgColor}; border-left:3px solid {borderColor}; padding:16px; margin:20px 0; border-radius:4px;"">{content}</div>";
    }

    /// <summary>
    /// Build button HTML
    /// </summary>
    private static string BuildButton(string url, string text, string bgColor)
    {
        return $@"<div style=""text-align:center; margin:24px 0;"">
  <a href=""{url}"" style=""display:inline-block; padding:12px 28px; background:{bgColor}; color:#ffffff; text-decoration:none; border-radius:6px; font-weight:500; font-size:14px;"">{text}</a>
</div>";
    }

    /// <summary>
    /// Parse and build HTML for features from JSON string
    /// </summary>
    private static string BuildFeaturesHtml(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return "";

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var features = JsonSerializer.Deserialize<List<FeatureItem>>(featuresJson, options);

            if (features == null || features.Count == 0) return "";

            var featuresHtml = string.Join("", features.Select(f =>
            {
                var text = string.IsNullOrWhiteSpace(f.Text) ? "" : f.Text;
                return $@"<li style=""margin:4px 0;"">{text}</li>";
            }));

            return $@"<div style=""margin-top:16px;"">
  <p style=""margin:0 0 8px; font-weight:500; color:{TEXT_PRIMARY};"">Tính năng của gói:</p>
  <ul style=""margin:0; padding-left:20px; color:{TEXT_SECONDARY};"">{featuresHtml}</ul>
</div>";
        }
        catch
        {
            return "";
        }
    }

    private class FeatureItem
    {
        public string Text { get; set; } = string.Empty;
        public string? IconType { get; set; }
        public string? Subtext { get; set; }
    }

    public static string BuildOtpEmailHtml(string otpCode, string purpose)
    {
        var safePurpose = string.IsNullOrWhiteSpace(purpose) ? "xác thực" : purpose;

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Xin chào,</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Mã xác thực (OTP) của bạn để {safePurpose}:
</p>
<div style=""text-align:center; margin:24px 0;"">
  <span style=""display:inline-block; padding:16px 32px; background:{BG_LIGHT}; border:1px solid {BORDER_COLOR}; border-radius:8px; font-size:28px; font-weight:700; letter-spacing:6px; color:{TEXT_PRIMARY};"">{otpCode}</span>
</div>
<p style=""margin:20px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
  Mã có hiệu lực trong 5 phút. Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.
</p>
<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">Trân trọng,<br/>{BRAND_NAME}</p>
</div>";

        return WrapEmailContent(PRIMARY_COLOR, "Mã xác thực OTP", body);
    }

    public static string BuildDoctorCredentialsEmailHtml(
        string fullName,
        string email,
        string password,
        string loginUrl,
        string? hospitalName = null)
    {
        var hospitalInfo = !string.IsNullOrEmpty(hospitalName)
            ? $@"<p style=""margin:8px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>"
            : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi BS. {fullName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Chào mừng bạn đến với {BRAND_NAME}. Tài khoản của bạn đã được tạo thành công.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin đăng nhập</p>
  {hospitalInfo}
  <p style=""margin:8px 0;""><strong>Email:</strong> {email}</p>
  <p style=""margin:8px 0;""><strong>Mật khẩu tạm thời:</strong> <code style=""background:#fee2e2; padding:2px 8px; border-radius:4px; color:{DANGER_COLOR};"">{password}</code></p>
")}

{BuildButton(loginUrl, "Đăng nhập ngay", PRIMARY_COLOR)}

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{DANGER_COLOR};"">Lưu ý bảo mật</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Đây là mật khẩu tạm thời. Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">Trân trọng,<br/>{BRAND_NAME}</p>
</div>";

        return WrapEmailContent(PRIMARY_COLOR, "Thông tin tài khoản bác sĩ", body);
    }

    public static string BuildPasswordResetEmailHtml(string resetUrl)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Xin chào,</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
</p>

{BuildButton(resetUrl, "Đặt lại mật khẩu", DANGER_COLOR)}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Liên kết có hiệu lực trong 24 giờ</li>
    <li>Chỉ sử dụng một lần duy nhất</li>
    <li>Nếu bạn không yêu cầu, vui lòng bỏ qua email này</li>
  </ul>
")}

<p style=""margin:20px 0 0; color:{TEXT_SECONDARY}; font-size:12px;"">
  Nếu nút không hoạt động, sao chép liên kết sau vào trình duyệt:<br/>
  <span style=""word-break:break-all; color:{PRIMARY_COLOR};"">{resetUrl}</span>
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">Trân trọng,<br/>{BRAND_NAME}</p>
</div>";

        return WrapEmailContent(DANGER_COLOR, "Đặt lại mật khẩu", body);
    }

    public static string BuildRefundEmailWithBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        decimal refundAmount)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã được hủy. Thông tin chi tiết như sau:
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Lý do hủy:</strong> {cancellationReason}</p>
  <p style=""margin:4px 0;""><strong>Số tiền hoàn trả:</strong> <span style=""font-size:18px; font-weight:600; color:{SUCCESS_COLOR};"">{refundAmount:N0} VNĐ</span></p>
")}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Chúng tôi sẽ hoàn trả số tiền trên vào tài khoản ngân hàng của bạn trong vòng <strong>5-7 ngày làm việc</strong>.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Xác nhận hủy lịch hẹn", body);
    }

    public static string BuildRefundEmailNoBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        decimal refundAmount)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã được hủy. Thông tin chi tiết như sau:
</p>

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:#92400e;"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Lý do hủy:</strong> {cancellationReason}</p>
  <p style=""margin:4px 0;""><strong>Số tiền hoàn trả:</strong> <span style=""font-size:18px; font-weight:600; color:{WARNING_COLOR};"">{refundAmount:N0} VNĐ</span></p>
")}

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{DANGER_COLOR};"">Yêu cầu hành động</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Để nhận hoàn tiền, vui lòng đăng nhập và cập nhật thông tin tài khoản ngân hàng trong phần Cài đặt.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(WARNING_COLOR, "Xác nhận hủy lịch hẹn", body);
    }

    public static string BuildRefundCompletedEmailHtml(
        string patientName,
        decimal refundAmount,
        string bankName,
        string accountNumber,
        DateTime transferDate)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Khoản hoàn tiền của bạn đã được chuyển thành công.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin hoàn tiền</p>
  <p style=""margin:4px 0;""><strong>Số tiền:</strong> <span style=""font-size:20px; font-weight:600; color:{SUCCESS_COLOR};"">{refundAmount:N0} VNĐ</span></p>
  <p style=""margin:4px 0;""><strong>Ngân hàng:</strong> {bankName}</p>
  <p style=""margin:4px 0;""><strong>Số tài khoản:</strong> {accountNumber}</p>
  <p style=""margin:4px 0;""><strong>Ngày chuyển:</strong> {transferDate.ToString(DateTimeFormat)}</p>
")}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; font-size:13px;"">
  Số tiền sẽ được ghi nhận vào tài khoản trong 1-2 ngày làm việc tùy theo ngân hàng.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Cảm ơn bạn đã sử dụng {BRAND_NAME}.<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Hoàn tiền thành công", body);
    }

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
  <p style=""margin:4px 0;""><strong>Ngân hàng:</strong> {bankName}</p>
  <p style=""margin:4px 0;""><strong>Số tài khoản:</strong> {accountNumber}</p>";
        }

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Có sự cố xảy ra trong quá trình hoàn tiền cho bạn.
</p>

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{DANGER_COLOR};"">Thông tin sự cố</p>
  <p style=""margin:4px 0;""><strong>Số tiền:</strong> {refundAmount:N0} VNĐ</p>
  {bankInfoHtml}
  <p style=""margin:4px 0;""><strong>Vấn đề:</strong> {issueDescription}</p>
")}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Vui lòng đăng nhập và cập nhật lại thông tin tài khoản ngân hàng chính xác để tiếp tục nhận hoàn tiền.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(DANGER_COLOR, "Sự cố hoàn tiền", body);
    }

    public static string BuildNoRefundEmailHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã được hủy thành công.
</p>

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{TEXT_PRIMARY};"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Lý do hủy:</strong> {cancellationReason}</p>
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Thông báo về hoàn tiền</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Do lịch hẹn được hủy trong vòng 12 giờ trước thời gian hẹn, theo chính sách của chúng tôi, không có khoản hoàn tiền nào được áp dụng.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(WARNING_COLOR, "Thông báo hủy lịch hẹn", body);
    }

    public static string BuildCancellationSuccessEmailHtml(
        string patientName,
        DateTime appointmentDate,
        string cancellationReason,
        string? doctorName = null,
        string? hospitalName = null)
    {
        var doctorInfo = !string.IsNullOrEmpty(doctorName) ? $@"<p style=""margin:4px 0;""><strong>Bác sĩ:</strong> {doctorName}</p>" : "";
        var hospitalInfo = !string.IsNullOrEmpty(hospitalName) ? $@"<p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>" : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã được hủy thành công.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin lịch hẹn đã hủy</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate.ToString(DateTimeFormat)}</p>
  {doctorInfo}
  {hospitalInfo}
  <p style=""margin:4px 0;""><strong>Lý do hủy:</strong> {cancellationReason}</p>
")}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Nếu bạn muốn đặt lịch hẹn mới, vui lòng truy cập website hoặc ứng dụng {BRAND_NAME}.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">Trân trọng,<br/>{BRAND_NAME}</p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Hủy lịch hẹn thành công", body);
    }


    public static string BuildAppointmentBookedSuccessEmailHtml(AppointmentBookingEmailData emailData)
    {
        var data = emailData.AppointmentData;

        var doctorInfo = !string.IsNullOrEmpty(data.DoctorName)
            ? $@"<p style=""margin:4px 0;""><strong>Bác sĩ:</strong> {data.DoctorName}</p>"
            : "";
        var specialtyInfo = !string.IsNullOrEmpty(data.DoctorSpecialty)
            ? $@"<p style=""margin:4px 0;""><strong>Chuyên khoa:</strong> {data.DoctorSpecialty}</p>"
            : "";
        var hospitalInfo = !string.IsNullOrEmpty(data.HospitalName)
            ? $@"<p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {data.HospitalName}</p>"
            : "";
        var addressInfo = !string.IsNullOrEmpty(data.HospitalAddress)
            ? $@"<p style=""margin:4px 0;""><strong>Địa chỉ:</strong> {data.HospitalAddress}</p>"
            : "";
        var serviceInfo = !string.IsNullOrEmpty(data.ServiceName)
            ? $@"<p style=""margin:4px 0;""><strong>Dịch vụ:</strong> {data.ServiceName}</p>"
            : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {data.PatientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã được đặt thành công và thanh toán hoàn tất.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {data.AppointmentDate.ToString(DateFormat)}</p>
  <p style=""margin:4px 0;""><strong>Thời gian:</strong> {data.AppointmentTime}</p>
  <p style=""margin:4px 0;""><strong>Loại hẹn:</strong> {data.AppointmentType}</p>
  {doctorInfo}
  {specialtyInfo}
  {serviceInfo}
  {hospitalInfo}
  {addressInfo}
  <p style=""margin:8px 0 0;""><strong>Đã thanh toán:</strong> <span style=""font-size:18px; font-weight:600; color:{SUCCESS_COLOR};"">{data.Amount:N0} VNĐ</span></p>
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Vui lòng có mặt trước giờ hẹn 15 phút</li>
    <li>Mang theo giấy tờ tùy thân (CMND/CCCD)</li>
    <li>Mang theo thẻ BHYT nếu có</li>
    <li>Hủy lịch trước 24 giờ nếu cần</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Đặt lịch hẹn thành công", body);
    }

    public static string BuildCancellationWithOptionsEmailHtml(CancellationWithOptionsEmailData data)
    {
        var doctorInfo = !string.IsNullOrEmpty(data.DoctorName) ? $" với BS. {data.DoctorName}" : "";
        var hospitalInfo = !string.IsNullOrEmpty(data.HospitalName) ? $" tại {data.HospitalName}" : "";

        var refundInfo = "";
        if (data.PotentialRefundAmount.HasValue && data.PotentialRefundPercentage.HasValue)
        {
            refundInfo = $@"<p style=""margin:8px 0;""><strong>Hoàn tiền (nếu chọn):</strong> {data.PotentialRefundAmount:N0} VNĐ ({data.PotentialRefundPercentage:N0}%)</p>";
        }

        var expiryInfo = data.TokenExpiry.HasValue
            ? $@"<p style=""margin:16px 0 0; color:{WARNING_COLOR}; font-size:13px;"">Các tùy chọn có hiệu lực đến: {data.TokenExpiry.Value.ToString(DateTimeFormat)}</p>"
            : "";

        var optionsHtml = "";
        if (!string.IsNullOrEmpty(data.SameDoctorRescheduleUrl))
            optionsHtml += $@"<a href=""{data.SameDoctorRescheduleUrl}"" style=""display:block; padding:12px 16px; margin:8px 0; background:{PRIMARY_COLOR}; color:#fff; text-decoration:none; border-radius:6px; text-align:center; font-size:14px;"">Đổi lịch với cùng bác sĩ</a>";
        if (!string.IsNullOrEmpty(data.ConfirmNewDoctorUrl))
            optionsHtml += $@"<a href=""{data.ConfirmNewDoctorUrl}"" style=""display:block; padding:12px 16px; margin:8px 0; background:#8b5cf6; color:#fff; text-decoration:none; border-radius:6px; text-align:center; font-size:14px;"">Xác nhận bác sĩ mới do bệnh viện chỉ định</a>";
        if (!string.IsNullOrEmpty(data.ChooseNewDoctorUrl))
            optionsHtml += $@"<a href=""{data.ChooseNewDoctorUrl}"" style=""display:block; padding:12px 16px; margin:8px 0; background:{SUCCESS_COLOR}; color:#fff; text-decoration:none; border-radius:6px; text-align:center; font-size:14px;"">Tự chọn bác sĩ mới</a>";
        if (!string.IsNullOrEmpty(data.RefundRequestUrl))
            optionsHtml += $@"<a href=""{data.RefundRequestUrl}"" style=""display:block; padding:12px 16px; margin:8px 0; background:{DANGER_COLOR}; color:#fff; text-decoration:none; border-radius:6px; text-align:center; font-size:14px;"">Yêu cầu hoàn tiền</a>";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {data.PatientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn{doctorInfo}{hospitalInfo} đã bị hủy bởi bệnh viện.
</p>

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{DANGER_COLOR};"">Thông tin lịch hẹn bị hủy</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {data.AppointmentDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Lý do:</strong> {data.CancellationReason}</p>
  {refundInfo}
")}

<div style=""background:{BG_LIGHT}; padding:20px; border-radius:8px; margin:20px 0;"">
  <p style=""margin:0 0 16px; font-weight:500; color:{TEXT_PRIMARY}; text-align:center;"">Vui lòng chọn phương án xử lý</p>
  {optionsHtml}
  {expiryInfo}
</div>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(WARNING_COLOR, "Lịch hẹn đã bị hủy", body);
    }

    public static string BuildDoctorChangeRefundEmailWithBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string originalDoctorName,
        decimal originalFee,
        string newDoctorName,
        decimal newFee,
        decimal refundAmount)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bạn đã thay đổi bác sĩ khám thành công.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin thay đổi</p>
  <table style=""width:100%; font-size:14px;"">
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Bác sĩ cũ:</td>
      <td style=""padding:8px 0;"">{originalDoctorName} ({originalFee:N0} VNĐ)</td>
    </tr>
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Bác sĩ mới:</td>
      <td style=""padding:8px 0;"">{newDoctorName} ({newFee:N0} VNĐ)</td>
    </tr>
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Ngày hẹn:</td>
      <td style=""padding:8px 0;"">{appointmentDate.ToString(DateTimeFormat)}</td>
    </tr>
  </table>
")}

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{SUCCESS_COLOR};"">Hoàn tiền chênh lệch</p>
  <p style=""margin:8px 0 0;"">Số tiền: <span style=""font-size:18px; font-weight:600; color:{SUCCESS_COLOR};"">{refundAmount:N0} VNĐ</span></p>
  <p style=""margin:4px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">Sẽ được chuyển trong 5-7 ngày làm việc.</p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">Trân trọng,<br/>{BRAND_NAME}</p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Thay đổi bác sĩ thành công", body);
    }

    public static string BuildDoctorChangeRefundEmailNoBankAccountHtml(
        string patientName,
        DateTime appointmentDate,
        string originalDoctorName,
        decimal originalFee,
        string newDoctorName,
        decimal newFee,
        decimal refundAmount)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bạn đã thay đổi bác sĩ khám thành công.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin thay đổi</p>
  <table style=""width:100%; font-size:14px;"">
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Bác sĩ cũ:</td>
      <td style=""padding:8px 0;"">{originalDoctorName} ({originalFee:N0} VNĐ)</td>
    </tr>
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Bác sĩ mới:</td>
      <td style=""padding:8px 0;"">{newDoctorName} ({newFee:N0} VNĐ)</td>
    </tr>
    <tr>
      <td style=""padding:8px 0; color:{TEXT_SECONDARY};"">Ngày hẹn:</td>
      <td style=""padding:8px 0;"">{appointmentDate.ToString(DateTimeFormat)}</td>
    </tr>
  </table>
")}

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{DANGER_COLOR};"">Yêu cầu hành động</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Bạn được hoàn <strong>{refundAmount:N0} VNĐ</strong>. Vui lòng đăng nhập và cập nhật thông tin tài khoản ngân hàng để nhận tiền.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(WARNING_COLOR, "Thay đổi bác sĩ thành công", body);
    }


    public static string BuildHospitalSubscriptionCreatedEmailHtml(HospitalSubscriptionCreatedEmailData data)
    {
        var billingCycleDisplay = data.BillingCycle?.ToUpper() switch
        {
            "MONTHLY" => "Tháng",
            "QUARTERLY" => "Quý",
            "YEARLY" => "Năm",
            _ => "Tháng",
        };

        var maxDoctorsInfo = data.MaxDoctors.HasValue ? $@"<p style=""margin:4px 0;""><strong>Số bác sĩ tối đa:</strong> {data.MaxDoctors}</p>" : "";
        var maxAppointmentsInfo = data.MaxAppointmentsPerMonth.HasValue ? $@"<p style=""margin:4px 0;""><strong>Lịch hẹn/tháng:</strong> {data.MaxAppointmentsPerMonth}</p>" : "";
        var featuresInfo = BuildFeaturesHtml(data.Features);

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {data.ContactPersonName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bệnh viện <strong>{data.HospitalName}</strong> đã đăng ký gói dịch vụ thành công.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin gói dịch vụ</p>
  <p style=""margin:4px 0;""><strong>Tên gói:</strong> {data.PlanName}</p>
  <p style=""margin:4px 0;""><strong>Chu kỳ:</strong> {billingCycleDisplay}</p>
  <p style=""margin:4px 0;""><strong>Giá:</strong> <span style=""font-size:18px; font-weight:600; color:{SUCCESS_COLOR};"">{data.Price:N0} VNĐ/{billingCycleDisplay}</span></p>
  <p style=""margin:4px 0;""><strong>Bắt đầu:</strong> {data.StartDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Hết hạn:</strong> {data.EndDate.ToString(DateTimeFormat)}</p>
  {maxDoctorsInfo}
  {maxAppointmentsInfo}
  {featuresInfo}
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Gói dịch vụ đã được kích hoạt ngay</li>
    <li>Hệ thống sẽ nhắc nhở gia hạn trước 7 ngày</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Đăng ký gói dịch vụ thành công", body);
    }

    public static string BuildHospitalRegistrationSubmittedEmailHtml(
        string hospitalName,
        string hospitalEmail,
        string hospitalPhone,
        string address,
        string taxCode)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi đại diện {hospitalName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Chúng tôi đã nhận được đơn đăng ký hợp tác của quý bệnh viện.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin đăng ký</p>
  <p style=""margin:4px 0;""><strong>Tên bệnh viện:</strong> {hospitalName}</p>
  <p style=""margin:4px 0;""><strong>Email:</strong> {hospitalEmail}</p>
  <p style=""margin:4px 0;""><strong>Điện thoại:</strong> {hospitalPhone}</p>
  <p style=""margin:4px 0;""><strong>Địa chỉ:</strong> {address}</p>
  <p style=""margin:4px 0;""><strong>Mã số thuế:</strong> {taxCode}</p>
")}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Các bước tiếp theo</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đội ngũ sẽ xem xét hồ sơ trong 2-3 ngày làm việc</li>
    <li>Kiểm tra tính xác thực của tài liệu</li>
    <li>Liên hệ lại qua email hoặc điện thoại</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ: {HOTLINE} | {SUPPORT_EMAIL}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(PRIMARY_COLOR, "Xác nhận đăng ký hợp tác", body);
    }

    public static string BuildHospitalRegistrationApprovedEmailHtml(string hospitalName, string? contractFileUrl)
    {
        var contractSection = !string.IsNullOrEmpty(contractFileUrl)
            ? BuildButton(contractFileUrl, "Tải hợp đồng", PRIMARY_COLOR)
            : "<p style=\"margin:20px 0; color:" + TEXT_SECONDARY + ";\">Chúng tôi sẽ liên hệ trực tiếp để hoàn tất thủ tục ký kết hợp đồng.</p>";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi đại diện {hospitalName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Đơn đăng ký hợp tác của quý bệnh viện đã được <strong style=""color:{SUCCESS_COLOR};"">CHẤP THUẬN</strong>.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{SUCCESS_COLOR};"">Trạng thái: ĐÃ CHẤP THUẬN</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Chào mừng {hospitalName} trở thành đối tác của {BRAND_NAME}!
  </p>
")}

{contractSection}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Các bước tiếp theo</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đội ngũ sẽ liên hệ trong 24 giờ</li>
    <li>Thiết lập tài khoản quản trị</li>
    <li>Hướng dẫn sử dụng hệ thống</li>
    <li>Triển khai chính thức</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ: {HOTLINE} | {SUPPORT_EMAIL}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Đăng ký hợp tác được chấp thuận", body);
    }

    public static string BuildHospitalSubscriptionUpgradedEmailHtml(HospitalSubscriptionUpgradedEmailData data)
    {
        var prevCycle = data.PreviousBillingCycle?.ToUpper() switch { "MONTHLY" => "Tháng", "QUARTERLY" => "Quý", "YEARLY" => "Năm", _ => "Tháng" };
        var newCycle = data.NewBillingCycle?.ToUpper() switch { "MONTHLY" => "Tháng", "QUARTERLY" => "Quý", "YEARLY" => "Năm", _ => "Tháng" };

        var bonusDaysInfo = data.BonusDays > 0 ? $@"<p style=""margin:4px 0;""><strong>Ngày thưởng:</strong> {data.BonusDays:N0} ngày</p>" : "";
        var maxDoctorsInfo = data.NewMaxDoctors.HasValue ? $@"<p style=""margin:4px 0;""><strong>Số bác sĩ tối đa:</strong> {data.NewMaxDoctors}</p>" : "";
        var maxAppointmentsInfo = data.NewMaxAppointmentsPerMonth.HasValue ? $@"<p style=""margin:4px 0;""><strong>Lịch hẹn/tháng:</strong> {data.NewMaxAppointmentsPerMonth}</p>" : "";
        var featuresInfo = BuildFeaturesHtml(data.NewFeatures);

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {data.ContactPersonName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bệnh viện <strong>{data.HospitalName}</strong> đã nâng cấp gói dịch vụ thành công.
</p>

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0 0 8px; font-weight:500; color:{TEXT_SECONDARY};"">Gói cũ</p>
  <p style=""margin:4px 0;"">{data.PreviousPlanName} - {data.PreviousPrice:N0} VNĐ/{prevCycle}</p>
")}

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Gói mới</p>
  <p style=""margin:4px 0;""><strong>Tên gói:</strong> {data.NewPlanName}</p>
  <p style=""margin:4px 0;""><strong>Giá:</strong> <span style=""font-size:18px; font-weight:600; color:{SUCCESS_COLOR};"">{data.NewPrice:N0} VNĐ/{newCycle}</span></p>
  <p style=""margin:4px 0;""><strong>Bắt đầu:</strong> {data.NewStartDate.ToString(DateTimeFormat)}</p>
  <p style=""margin:4px 0;""><strong>Hết hạn:</strong> {data.NewEndDate.ToString(DateTimeFormat)}</p>
  {bonusDaysInfo}
  {maxDoctorsInfo}
  {maxAppointmentsInfo}
  {featuresInfo}
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Nâng cấp gói dịch vụ thành công", body);
    }

    public static string BuildHospitalRegistrationRejectedEmailHtml(string hospitalName, string reason)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi đại diện {hospitalName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Sau khi xem xét, đơn đăng ký hợp tác của quý bệnh viện chưa được chấp thuận.
</p>

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{DANGER_COLOR};"">Trạng thái: CHƯA CHẤP THUẬN</p>
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lý do</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY};"">{reason}</p>
")}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Đăng ký lại</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Quý bệnh viện có thể đăng ký lại sau khi khắc phục các vấn đề được nêu. Liên hệ với chúng tôi để được tư vấn thêm.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ: {HOTLINE} | {SUPPORT_EMAIL}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(DANGER_COLOR, "Thông báo về đơn đăng ký", body);
    }

    public static string BuildHospitalAccountCredentialsEmailHtml(
        string hospitalName,
        string email,
        string generatedPassword,
        string loginUrl,
        string? contractFileUrl)
    {
        var contractSection = !string.IsNullOrEmpty(contractFileUrl)
            ? BuildButton(contractFileUrl, "Tải hợp đồng", SUCCESS_COLOR)
            : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi đại diện {hospitalName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Đơn đăng ký hợp tác đã được <strong style=""color:{SUCCESS_COLOR};"">PHÊ DUYỆT</strong>. Tài khoản quản lý đã được tạo.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin đăng nhập</p>
  <p style=""margin:4px 0;""><strong>Email:</strong> {email}</p>
  <p style=""margin:4px 0;""><strong>Mật khẩu tạm thời:</strong> <code style=""background:#fee2e2; padding:2px 8px; border-radius:4px; color:{DANGER_COLOR};"">{generatedPassword}</code></p>
")}

{BuildButton(loginUrl, "Đăng nhập ngay", PRIMARY_COLOR)}

{contractSection}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý bảo mật</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đổi mật khẩu ngay sau khi đăng nhập</li>
    <li>Không chia sẻ thông tin tài khoản</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ: {HOTLINE} | {SUPPORT_EMAIL}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Tài khoản bệnh viện", body);
    }


    public static string BuildDoctorAssignedEmailHtml(
        string patientName,
        string doctorName,
        string specialty,
        string hospitalName,
        string appointmentDate,
        string appointmentTime,
        string? staffNote)
    {
        var staffNoteSection = string.IsNullOrEmpty(staffNote) ? "" : BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Ghi chú từ nhân viên</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY};"">{staffNote}</p>
");

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bệnh viện đã gán bác sĩ cho lịch hẹn của bạn.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin bác sĩ</p>
  <p style=""margin:4px 0;""><strong>Bác sĩ:</strong> {doctorName}</p>
  <p style=""margin:4px 0;""><strong>Chuyên khoa:</strong> {specialty}</p>
")}

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày khám:</strong> {appointmentDate}</p>
  <p style=""margin:4px 0;""><strong>Giờ khám:</strong> {appointmentTime}</p>
  <p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>
")}

{staffNoteSection}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; font-size:13px;"">
  Vui lòng đến đúng giờ. Nếu cần thay đổi, liên hệ trước ít nhất 24 giờ.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(PRIMARY_COLOR, "Bác sĩ đã được gán", body);
    }

    public static string BuildAutoCancelledEmailHtml(
        string patientName,
        string hospitalName,
        string specialtyName,
        string appointmentDate,
        string appointmentTime)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Lịch hẹn của bạn đã bị <strong style=""color:{DANGER_COLOR};"">tự động hủy</strong> do bệnh viện không gán bác sĩ trước ngày hẹn.
</p>

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{DANGER_COLOR};"">Thông tin lịch hẹn đã hủy</p>
  <p style=""margin:4px 0;""><strong>Chuyên khoa:</strong> {specialtyName}</p>
  <p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate}</p>
  <p style=""margin:4px 0;""><strong>Giờ hẹn:</strong> {appointmentTime}</p>
  <p style=""margin:4px 0;""><strong>Lý do:</strong> Bệnh viện không gán bác sĩ trước ngày hẹn</p>
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Gợi ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đặt lịch hẹn mới tại bệnh viện khác</li>
    <li>Chọn bác sĩ cụ thể thay vì để bệnh viện gán</li>
    <li>Liên hệ bệnh viện để được hỗ trợ</li>
  </ul>
")}

{BuildButton(BOOKING_URL, "Đặt lịch hẹn mới", PRIMARY_COLOR)}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(DANGER_COLOR, "Lịch hẹn đã bị hủy", body);
    }

    public static string BuildContractGeneratedEmailHtml(
        string hospitalName,
        string representativeName,
        string contractNumber,
        string signingLink,
        DateTime linkExpiresAt)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {representativeName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Hợp đồng hợp tác giữa <strong>{hospitalName}</strong> và <strong>{BRAND_NAME}</strong> đã được tạo.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Thông tin hợp đồng</p>
  <p style=""margin:4px 0;""><strong>Số hợp đồng:</strong> {contractNumber}</p>
  <p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>
  <p style=""margin:4px 0;""><strong>Người đại diện:</strong> {representativeName}</p>
  <p style=""margin:4px 0;""><strong>Link hết hạn:</strong> {linkExpiresAt.ToString(DateTimeFormat)}</p>
")}

{BuildButton(signingLink, "Ký hợp đồng ngay", SUCCESS_COLOR)}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Hướng dẫn ký hợp đồng</p>
  <ol style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Truy cập link ký hợp đồng</li>
    <li>Xem xét nội dung hợp đồng</li>
    <li>Vẽ chữ ký điện tử</li>
    <li>Xác thực bằng OTP</li>
    <li>Hoàn tất</li>
  </ol>
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Link có hiệu lực đến {linkExpiresAt.ToString(DateTimeFormat)}</li>
    <li>Mỗi link chỉ sử dụng một lần</li>
    <li>Không chia sẻ link với người khác</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ: {HOTLINE} | {SUPPORT_EMAIL}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(PRIMARY_COLOR, "Hợp đồng hợp tác", body);
    }

    public static string BuildContractSignedConfirmationEmailHtml(
        string hospitalName,
        string representativeName,
        string contractNumber,
        DateTime signedAt)
    {
        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {representativeName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bạn đã ký hợp đồng hợp tác thành công với {BRAND_NAME}.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin hợp đồng</p>
  <p style=""margin:4px 0;""><strong>Số hợp đồng:</strong> {contractNumber}</p>
  <p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>
  <p style=""margin:4px 0;""><strong>Người đại diện:</strong> {representativeName}</p>
  <p style=""margin:4px 0;""><strong>Thời gian ký:</strong> {signedAt.ToString(DateTimeFormat)}</p>
")}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Bước tiếp theo</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Đội ngũ {BRAND_NAME} sẽ xem xét và phê duyệt hợp đồng trong thời gian sớm nhất. Bạn sẽ nhận được email thông báo khi hoàn tất.
  </p>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Cảm ơn bạn đã tin tưởng {BRAND_NAME}.<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Ký hợp đồng thành công", body);
    }

    public static string BuildAppointmentResultEmailHtml(
        string patientName,
        DateTime appointmentDate,
        string appointmentTime,
        string resultUrl,
        string? doctorName = null,
        string? hospitalName = null)
    {
        var doctorInfo = !string.IsNullOrEmpty(doctorName) ? $@"<p style=""margin:4px 0;""><strong>Bác sĩ:</strong> {doctorName}</p>" : "";
        var hospitalInfo = !string.IsNullOrEmpty(hospitalName) ? $@"<p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>" : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Bác sĩ đã hoàn tất khám bệnh và cập nhật kết quả cho buổi khám của bạn.
</p>

{BuildInfoBox(BG_GREEN_LIGHT, SUCCESS_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{SUCCESS_COLOR};"">Thông tin buổi khám</p>
  <p style=""margin:4px 0;""><strong>Ngày khám:</strong> {appointmentDate.ToString(DateFormat)}</p>
  <p style=""margin:4px 0;""><strong>Giờ khám:</strong> {appointmentTime}</p>
  {doctorInfo}
  {hospitalInfo}
")}

{BuildButton(resultUrl, "Xem kết quả khám bệnh", PRIMARY_COLOR)}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đọc kỹ kết quả và tuân thủ hướng dẫn của bác sĩ</li>
    <li>Liên hệ bác sĩ nếu có thắc mắc</li>
    <li>Lưu giữ kết quả để theo dõi sức khỏe</li>
  </ul>
")}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(SUCCESS_COLOR, "Kết quả khám bệnh", body);
    }

    public static string BuildAppointmentRejectedEmailHtml(
        string patientName,
        DateTime appointmentDate,
        string appointmentTime,
        string rejectionReason,
        string? doctorName = null,
        string? hospitalName = null)
    {
        var doctorInfo = !string.IsNullOrEmpty(doctorName) ? $@"<p style=""margin:4px 0;""><strong>Bác sĩ:</strong> {doctorName}</p>" : "";
        var hospitalInfo = !string.IsNullOrEmpty(hospitalName) ? $@"<p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {hospitalName}</p>" : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {patientName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Yêu cầu đặt lịch hẹn của bạn đã bị từ chối.
</p>

{BuildInfoBox(BG_RED_LIGHT, DANGER_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{DANGER_COLOR};"">Thông tin lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày hẹn:</strong> {appointmentDate.ToString(DateFormat)}</p>
  <p style=""margin:4px 0;""><strong>Giờ hẹn:</strong> {appointmentTime}</p>
  {doctorInfo}
  {hospitalInfo}
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lý do từ chối</p>
  <p style=""margin:8px 0 0; color:{TEXT_SECONDARY};"">{rejectionReason}</p>
")}

{BuildInfoBox(BG_LIGHT, BORDER_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:{TEXT_PRIMARY};"">Gợi ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Đặt lịch với bác sĩ/chuyên khoa khác</li>
    <li>Chọn thời gian khác phù hợp hơn</li>
    <li>Liên hệ bệnh viện để được tư vấn</li>
  </ul>
")}

{BuildButton(HOME_URL, "Đặt lịch hẹn mới", PRIMARY_COLOR)}

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(DANGER_COLOR, "Lịch hẹn bị từ chối", body);
    }

    public static string BuildReminderEmailHtml(AppointmentReminderEvent @event)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(@event.AppointmentDate, vietnamTimeZone);
        var formattedDate = vietnamTime.ToString(DateFormat);

        var timeText = @event.ReminderType == "24_HOURS" ? "24 giờ" : "1 giờ";
        var headerColor = @event.ReminderType == "1_HOUR" ? DANGER_COLOR : PRIMARY_COLOR;

        var doctorInfo = !string.IsNullOrEmpty(@event.DoctorName) ? $@"<p style=""margin:4px 0;""><strong>Bác sĩ:</strong> BS. {@event.DoctorName}</p>" : "";
        var specialtyInfo = !string.IsNullOrEmpty(@event.SpecialtyName) ? $@"<p style=""margin:4px 0;""><strong>Chuyên khoa:</strong> {@event.SpecialtyName}</p>" : "";
        var serviceInfo = !string.IsNullOrEmpty(@event.ServiceName) ? $@"<p style=""margin:4px 0;""><strong>Dịch vụ:</strong> {@event.ServiceName}</p>" : "";
        var addressInfo = !string.IsNullOrEmpty(@event.HospitalAddress) ? $@"<p style=""margin:4px 0;""><strong>Địa chỉ:</strong> {@event.HospitalAddress}</p>" : "";

        var body = $@"
<p style=""margin:0 0 16px; font-size:15px;"">Kính gửi {@event.PatientFullName},</p>
<p style=""margin:0 0 20px; color:{TEXT_SECONDARY}; line-height:1.6;"">
  Đây là lời nhắc về lịch hẹn khám sắp tới của bạn. Còn <strong>{timeText}</strong> nữa là đến giờ hẹn.
</p>

{BuildInfoBox(BG_TEAL_LIGHT, PRIMARY_COLOR, $@"
  <p style=""margin:0 0 12px; font-weight:500; color:{PRIMARY_DARK};"">Chi tiết lịch hẹn</p>
  <p style=""margin:4px 0;""><strong>Ngày khám:</strong> {formattedDate}</p>
  <p style=""margin:4px 0;""><strong>Giờ khám:</strong> {@event.AppointmentTime}</p>
  <p style=""margin:4px 0;""><strong>Bệnh viện:</strong> {@event.HospitalName}</p>
  {addressInfo}
  {doctorInfo}
  {specialtyInfo}
  {serviceInfo}
")}

{BuildInfoBox(BG_AMBER_LIGHT, WARNING_COLOR, $@"
  <p style=""margin:0; font-weight:500; color:#92400e;"">Lưu ý</p>
  <ul style=""margin:8px 0 0; padding-left:20px; color:{TEXT_SECONDARY}; font-size:13px;"">
    <li>Vui lòng đến trước giờ hẹn 15 phút</li>
    <li>Mang theo CMND/CCCD và thẻ BHYT (nếu có)</li>
    <li>Mang theo kết quả xét nghiệm trước đó (nếu có)</li>
  </ul>
")}

<p style=""margin:20px 0; color:{TEXT_SECONDARY}; font-size:13px;"">
  Nếu cần thay đổi lịch hẹn, vui lòng truy cập ứng dụng {BRAND_NAME} hoặc liên hệ hotline.
</p>

<div style=""margin-top:24px; padding-top:20px; border-top:1px solid {BORDER_COLOR};"">
  <p style=""margin:0; color:{TEXT_SECONDARY}; font-size:13px;"">
    Liên hệ hỗ trợ: {HOTLINE}<br/>
    Trân trọng, {BRAND_NAME}
  </p>
</div>";

        return WrapEmailContent(headerColor, $"Nhắc lịch hẹn - Còn {timeText}", body);
    }
}
