using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Shared.FileUpload.Services;
using iText.Html2pdf;
using iText.Kernel.Pdf;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service for generating partnership contracts
/// NOTE: This is a basic implementation. For production, consider using QuestPDF or iTextSharp
/// </summary>
public class ContractGenerationService : BaseService, IContractGenerationService
{
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly IAdminSignatureRepository _signatureRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IConfiguration _configuration;

    public ContractGenerationService(
        IHospitalRegistrationRepository registrationRepository,
        IAdminSignatureRepository signatureRepository,
        FileUploadOrchestrator uploadOrchestrator,
        IConfiguration configuration,
        ILogger<ContractGenerationService> logger) : base(logger)
    {
        _registrationRepository = registrationRepository;
        _signatureRepository = signatureRepository;
        _uploadOrchestrator = uploadOrchestrator;
        _configuration = configuration;
    }

    public async Task<GenerateContractResponseDto> GenerateContractAsync(Guid registrationId, string adminId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Generating contract for registration: {RegistrationId}", null, registrationId);

            // Get registration
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                throw new NotFoundException($"Registration with ID {registrationId} not found", "REGISTRATION_NOT_FOUND");
            }

            // Get admin signature
            var adminSignature = await _signatureRepository.GetActiveByAdminIdAsync(adminId);
            if (adminSignature == null)
            {
                throw new InvalidOperationException(
                    "Admin must have an active signature before generating contracts. Please create a signature first.");
            }

            // Prepare contract data
            var contractData = BuildContractData(
                registration,
                adminSignature,
                GenerateContractNumber(registration.Id),
                DateTime.Now,
                DateTime.Now,
                DateTime.Now.AddYears(1));

            // Generate PDF
            var pdfBytes = await GenerateContractPdfAsync(contractData);

            // Upload to storage
            var fileName = $"contract-{contractData.ContractNumber}-{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var fileUrl = await UploadContractFileAsync(pdfBytes, fileName);

            LogInfo("Successfully generated contract: {ContractNumber}", null, contractData.ContractNumber);

            return new GenerateContractResponseDto
            {
                ContractFileUrl = fileUrl,
                ContractNumber = contractData.ContractNumber,
                AdminSignatureId = adminSignature.Id,
                GeneratedAt = DateTime.UtcNow
            };
        }, "GenerateContract");
    }

    public async Task<byte[]> GenerateContractPdfAsync(ContractDataDto contractData, string hospitalSignatureBase64 = "", DateTime? hospitalSignedAt = null, string adminSignatureBase64Override = "")
    {
        try
        {
            LogInfo("Generating PDF for contract: {ContractNumber}", null, contractData.ContractNumber);

            // Use provided admin signature or download if not provided
            string adminSignatureBase64 = adminSignatureBase64Override;
            if (string.IsNullOrEmpty(adminSignatureBase64) && !string.IsNullOrEmpty(contractData.AdminSignatureUrl))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var imageBytes = await httpClient.GetByteArrayAsync(contractData.AdminSignatureUrl);
                    adminSignatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error downloading admin signature image: {Url}", null, contractData.AdminSignatureUrl);
                    // Continue without signature image
                }
            }

            var htmlContent = GenerateContractHtml(contractData, adminSignatureBase64, hospitalSignatureBase64, hospitalSignedAt);

            return await Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();

                // Create PDF writer and document
                var writer = new PdfWriter(memoryStream);
                var pdfDocument = new PdfDocument(writer);

                // Set PDF metadata
                var info = pdfDocument.GetDocumentInfo();
                info.SetTitle($"Hợp Đồng Hợp Tác - {contractData.ContractNumber}");
                info.SetAuthor(contractData.CompanyName);
                info.SetCreator("BookingCare System");
                info.SetSubject("Hợp đồng hợp tác cung cấp dịch vụ đặt khám trực tuyến");

                // Configure converter properties for Vietnamese support
                var converterProperties = new ConverterProperties();
                converterProperties.SetCharset("UTF-8");

                // Convert HTML to PDF
                HtmlConverter.ConvertToPdf(htmlContent, pdfDocument, converterProperties);

                // Close document before getting bytes
                pdfDocument.Close();

                LogInfo("Successfully generated PDF for contract: {ContractNumber}", null, contractData.ContractNumber);
                return memoryStream.ToArray();
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "Error generating PDF for contract: {ContractNumber}", null, contractData.ContractNumber);
            throw new InvalidOperationException($"Failed to generate PDF for contract {contractData.ContractNumber}", ex);
        }
    }

    public async Task<string> AddHospitalSignatureToContractAsync(
        Guid registrationId,
        string hospitalSignatureUrl,
        DateTime signedAt)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Adding hospital signature to contract for registration: {RegistrationId}", null, registrationId);

            try
            {
                // Get registration data (same as GenerateContractAsync)
                var registration = await _registrationRepository.GetByIdAsync(registrationId);
                if (registration == null)
                {
                    throw new NotFoundException($"Registration with ID {registrationId} not found", "REGISTRATION_NOT_FOUND");
                }

                // Get admin signature (same as GenerateContractAsync)
                if (!registration.AdminSignatureId.HasValue)
                {
                    throw new InvalidOperationException("Registration does not have an associated admin signature");
                }

                var adminSignature = await _signatureRepository.GetByIdAsync(registration.AdminSignatureId.Value);
                if (adminSignature == null)
                {
                    throw new InvalidOperationException(
                        "Admin signature not found. Please regenerate the contract.");
                }

                // Prepare contract data using saved dates from registration
                var contractData = BuildContractData(
                    registration,
                    adminSignature,
                    registration.ContractNumber!,
                    registration.ContractDate ?? DateTime.Now,
                    registration.ContractEffectiveDate ?? DateTime.Now,
                    registration.ContractExpiryDate ?? DateTime.Now.AddYears(1));

                // Download hospital signature image and convert to base64
                byte[] hospitalSignatureBytes;
                using (var httpClient = new HttpClient())
                {
                    hospitalSignatureBytes = await httpClient.GetByteArrayAsync(hospitalSignatureUrl);
                }
                var hospitalSignatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(hospitalSignatureBytes)}";

                // Download admin signature and convert to base64
                string adminSignatureBase64 = string.Empty;
                if (!string.IsNullOrEmpty(contractData.AdminSignatureUrl))
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        var adminImageBytes = await httpClient.GetByteArrayAsync(contractData.AdminSignatureUrl);
                        adminSignatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(adminImageBytes)}";
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Could not download admin signature: {Error}", null, ex.Message);
                    }
                }

                // Generate new PDF with both signatures using reusable method
                var signedPdfBytes = await GenerateContractPdfAsync(contractData, hospitalSignatureBase64, signedAt, adminSignatureBase64);

                // Upload the new signed contract
                var fileName = $"contract-signed-{Guid.NewGuid():N}.pdf";
                var signedContractUrl = await UploadContractFileAsync(signedPdfBytes, fileName);

                LogInfo("Successfully added hospital signature to contract. New URL: {SignedUrl}", null, signedContractUrl);
                return signedContractUrl;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error adding hospital signature to contract for registration: {RegistrationId}", null, registrationId);
                throw new InvalidOperationException("Failed to add hospital signature to contract", ex);
            }
        }, "AddHospitalSignatureToContract");
    }

    #region Private Helper Methods

    private static string GenerateContractNumber(Guid registrationId)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        var shortId = registrationId.ToString("N")[..8].ToUpper();
        return $"HĐ-BC-{timestamp}-{shortId}";
    }

    private static string GenerateContractHtml(ContractDataDto data, string adminSignatureBase64 = "", string hospitalSignatureBase64 = "", DateTime? hospitalSignedAt = null)
    {
        // Professional contract template in Vietnamese
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hợp Đồng Hợp Tác - {data.ContractNumber}</title>
    <style>
        body {{
            font-family: 'Times New Roman', serif;
            line-height: 1.6;
            margin: 40px;
            color: #000;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            font-size: 18px;
            font-weight: bold;
            text-transform: uppercase;
            margin: 10px 0;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
        }}
        .content {{
            text-align: justify;
            font-size: 13px;
        }}
        .content h2 {{
            font-size: 14px;
            font-weight: bold;
            margin-top: 20px;
            margin-bottom: 10px;
        }}
        .content p {{
            margin: 10px 0;
        }}
        .party-info {{
            margin: 15px 0;
            padding-left: 20px;
        }}
        .signature-section {{
            margin-top: 50px;
            display: flex;
            justify-content: space-between;
        }}
        .signature-box {{
            text-align: center;
            width: 45%;
        }}
        .signature-box p {{
            margin: 5px 0;
            font-weight: bold;
        }}
        .signature-image {{
            margin: 20px 0;
            height: 80px;
        }}
        .terms {{
            margin: 15px 0;
            padding-left: 30px;
        }}
        .terms li {{
            margin: 8px 0;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <p><strong>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</strong></p>
        <p><strong>Độc lập - Tự do - Hạnh phúc</strong></p>
        <p style='margin-top: 20px;'>───────────</p>
        <h1>HỢP ĐỒNG HỢP TÁC</h1>
        <p>Số: {data.ContractNumber}</p>
        <p>Ngày {data.ContractDate:dd} tháng {data.ContractDate:MM} năm {data.ContractDate:yyyy}</p>
    </div>

    <div class='content'>
        <p style='text-align: center; font-weight: bold; margin: 20px 0;'>
            V/v: Hợp tác cung cấp dịch vụ đặt khám trực tuyến
        </p>

        <p>Hôm nay, ngày {data.ContractDate:dd} tháng {data.ContractDate:MM} năm {data.ContractDate:yyyy}, tại {data.CompanyAddress}, chúng tôi gồm:</p>

        <h2>BÊN A (BÊN CUNG CẤP DỊCH VỤ):</h2>
        <div class='party-info'>
            <p><strong>Tên công ty:</strong> {data.CompanyName}</p>
            <p><strong>Địa chỉ:</strong> {data.CompanyAddress}</p>
            <p><strong>Mã số thuế:</strong> {data.CompanyTaxCode}</p>
            <p><strong>Điện thoại:</strong> {data.CompanyPhone}</p>
            <p><strong>Email:</strong> {data.CompanyEmail}</p>
            <p><strong>Đại diện:</strong> {data.AdminFullName} - Chức vụ: {data.AdminPosition}</p>
        </div>

        <h2>BÊN B (BÊN SỬ DỤNG DỊCH VỤ):</h2>
        <div class='party-info'>
            <p><strong>Tên bệnh viện:</strong> {data.HospitalName}</p>
            <p><strong>Địa chỉ:</strong> {data.HospitalAddress}</p>
            <p><strong>Mã số thuế:</strong> {data.HospitalTaxCode}</p>
            <p><strong>Điện thoại:</strong> {data.HospitalPhone}</p>
            <p><strong>Email:</strong> {data.HospitalEmail}</p>
            <p><strong>Đại diện:</strong> {data.RepresentativeName} - Chức vụ: {data.RepresentativePosition}</p>
        </div>

        <p>Hai bên thống nhất ký kết hợp đồng hợp tác với các điều khoản sau:</p>

        <h2>ĐIỀU 1: ĐỐI TƯỢNG VÀ PHẠM VI HỢP ĐỒNG</h2>
        <p>1.1. Bên A cung cấp nền tảng công nghệ đặt khám trực tuyến BookingCare cho Bên B.</p>
        <p>1.2. Bên B sử dụng nền tảng để quản lý lịch khám, bác sĩ và tiếp nhận đặt khám từ bệnh nhân.</p>
        <p>1.3. Phạm vi hợp tác bao gồm:</p>
        <ul class='terms'>
            <li>Cung cấp tài khoản quản trị cho bệnh viện</li>
            <li>Đăng tải thông tin bệnh viện, chuyên khoa, bác sĩ</li>
            <li>Quản lý lịch khám và đặt lịch trực tuyến</li>
            <li>Xử lý thanh toán trực tuyến (nếu có)</li>
            <li>Hỗ trợ kỹ thuật và vận hành</li>
        </ul>

        <h2>ĐIỀU 2: QUYỀN VÀ NGHĨA VỤ CỦA BÊN A</h2>
        <p>2.1. Quyền của Bên A:</p>
        <ul class='terms'>
            <li>Thu phí dịch vụ theo thỏa thuận tại Điều 4</li>
            <li>Yêu cầu Bên B cung cấp thông tin chính xác, đầy đủ</li>
            <li>Tạm ngưng dịch vụ nếu Bên B vi phạm hợp đồng</li>
        </ul>
        <p>2.2. Nghĩa vụ của Bên A:</p>
        <ul class='terms'>
            <li>Đảm bảo hệ thống hoạt động ổn định 24/7</li>
            <li>Bảo mật thông tin của Bên B và bệnh nhân</li>
            <li>Hỗ trợ kỹ thuật trong giờ hành chính</li>
            <li>Cập nhật và nâng cấp hệ thống định kỳ</li>
        </ul>

        <h2>ĐIỀU 3: QUYỀN VÀ NGHĨA VỤ CỦA BÊN B</h2>
        <p>3.1. Quyền của Bên B:</p>
        <ul class='terms'>
            <li>Sử dụng đầy đủ các tính năng của nền tảng</li>
            <li>Yêu cầu hỗ trợ kỹ thuật khi cần thiết</li>
            <li>Được bảo mật thông tin theo quy định</li>
        </ul>
        <p>3.2. Nghĩa vụ của Bên B:</p>
        <ul class='terms'>
            <li>Cung cấp thông tin chính xác, đầy đủ về bệnh viện, bác sĩ</li>
            <li>Cập nhật lịch khám thường xuyên</li>
            <li>Thanh toán phí dịch vụ đúng hạn</li>
            <li>Tuân thủ quy định sử dụng nền tảng</li>
            <li>Bảo mật thông tin đăng nhập tài khoản</li>
        </ul>

        <h2>ĐIỀU 4: PHÍ DỊCH VỤ VÀ THANH TOÁN</h2>
        <p>4.1. Phí dịch vụ: Bên B thanh toán cho Bên A mức phí {data.ServiceFeePercentage}% trên tổng doanh thu từ các lượt khám được đặt qua nền tảng.</p>
        <p>4.2. Phương thức thanh toán: {data.PaymentTerms}</p>
        <p>4.3. Thời hạn thanh toán: Trong vòng {data.PaymentDueDays} ngày kể từ ngày nhận hóa đơn.</p>
        <p>4.4. Hình thức thanh toán: Chuyển khoản ngân hàng theo thông tin Bên A cung cấp.</p>

        <h2>ĐIỀU 5: THỜI HẠN HỢP ĐỒNG</h2>
        <p>5.1. Hợp đồng có hiệu lực từ ngày {data.EffectiveDate:dd/MM/yyyy} đến ngày {data.ExpiryDate:dd/MM/yyyy}.</p>
        <p>5.2. Hợp đồng tự động gia hạn thêm 01 năm nếu không có bên nào thông báo chấm dứt trước 30 ngày.</p>

        <h2>ĐIỀU 6: CHẤM DỨT HỢP ĐỒNG</h2>
        <p>6.1. Hợp đồng chấm dứt trong các trường hợp:</p>
        <ul class='terms'>
            <li>Hết thời hạn và không gia hạn</li>
            <li>Hai bên thỏa thuận chấm dứt</li>
            <li>Một bên vi phạm nghiêm trọng hợp đồng</li>
        </ul>
        <p>6.2. Bên muốn chấm dứt hợp đồng phải thông báo trước 30 ngày.</p>

        <h2>ĐIỀU 7: GIẢI QUYẾT TRANH CHẤP</h2>
        <p>7.1. Mọi tranh chấp phát sinh sẽ được giải quyết thông qua thương lượng, hòa giải.</p>
        <p>7.2. Nếu không thỏa thuận được, tranh chấp sẽ được đưa ra Tòa án có thẩm quyền.</p>

        <h2>ĐIỀU 8: ĐIỀU KHOẢN CHUNG</h2>
        <p>8.1. Hợp đồng được lập thành 02 bản, mỗi bên giữ 01 bản có giá trị pháp lý như nhau.</p>
        <p>8.2. Mọi sửa đổi, bổ sung phải được lập thành văn bản và có chữ ký của cả hai bên.</p>
        <p>8.3. Hợp đồng có hiệu lực kể từ ngày ký.</p>

        <div class='signature-section'>
            <div class='signature-box'>
                <p>ĐẠI DIỆN BÊN A</p>
                <p>{data.AdminPosition}</p>
                <div class='signature-image'>
                    {(string.IsNullOrEmpty(adminSignatureBase64)
                        ? "<p style='font-style: italic; color: #666;'>(Chữ ký)</p>"
                        : $"<img src='{adminSignatureBase64}' alt='Chữ ký Admin' style='max-height: 80px;' />")}
                </div>
                <p>{data.AdminFullName}</p>
            </div>
            <div class='signature-box'>
                <p>ĐẠI DIỆN BÊN B</p>
                <p>{data.RepresentativePosition}</p>
                <div class='signature-image'>
                    {(string.IsNullOrEmpty(hospitalSignatureBase64)
                        ? "<p style='font-style: italic; color: #666;'>(Chữ ký điện tử)</p>"
                        : $"<img src='{hospitalSignatureBase64}' alt='Chữ ký Hospital' style='max-height: 80px;' />")}
                </div>
                {(hospitalSignedAt.HasValue
                    ? $"<p style='font-size: 12px; color: #666; margin-top: 5px;'>Ngày ký: {hospitalSignedAt.Value:dd/MM/yyyy HH:mm}</p>"
                    : "")}
                <p>{data.RepresentativeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private async Task<string> UploadContractFileAsync(byte[] fileBytes, string fileName)
    {
        try
        {
            LogInfo("Uploading contract file: {FileName}, Size: {Size} bytes", null, fileName, fileBytes.Length);

            // Create a memory stream from bytes
            using var memoryStream = new MemoryStream(fileBytes);
            memoryStream.Position = 0;

            // Create IFormFile from memory stream
            var formFile = new FormFile(memoryStream, 0, fileBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // Configure file upload
            var config = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".pdf" },
                MaxSizeInMB = 10,
                Folder = "hospital-registrations/contracts",
                SuccessMessage = "Contract file uploaded successfully",
                EntityType = "ContractFile"
            };

            // Upload using FileUploadOrchestrator
            var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                formFile,
                config,
                Guid.Empty,
                Logger,
                CancellationToken.None
            );

            if (!uploadResult.Success || uploadResult.UploadResult == null)
            {
                throw new FileUploadException($"Failed to upload contract file: {uploadResult.ErrorMessage}");
            }

            var fileUrl = uploadResult.UploadResult.CloudFrontUrl ?? uploadResult.UploadResult.FileUrl;
            LogInfo("Successfully uploaded contract file: {FileName} to {Url}", null, fileName, fileUrl ?? "Unknown URL");

            return fileUrl ?? throw new FileUploadException("Upload result returned null file URL");
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading contract file: {FileName}", null, fileName);
            throw new FileUploadException("Failed to upload contract file", ex);
        }
    }

    /// <summary>
    /// Build ContractDataDto from registration and admin signature
    /// Centralizes company info configuration to avoid code duplication
    /// </summary>
    private ContractDataDto BuildContractData(
        HospitalRegistrationEntity registration,
        AdminSignatureEntity adminSignature,
        string contractNumber,
        DateTime contractDate,
        DateTime effectiveDate,
        DateTime expiryDate)
    {
        return new ContractDataDto
        {
            ContractNumber = contractNumber,
            ContractDate = contractDate,
            EffectiveDate = effectiveDate,
            ExpiryDate = expiryDate,

            // Admin/Company info from configuration
            AdminFullName = adminSignature.FullName,
            AdminPosition = adminSignature.Position,
            AdminSignatureUrl = adminSignature.SignatureImageUrl,
            CompanyName = _configuration["Contract:CompanyName"] ?? "CÔNG TY TNHH BOOKINGCARE",
            CompanyAddress = _configuration["Contract:CompanyAddress"] ?? "Hà Nội, Việt Nam",
            CompanyTaxCode = _configuration["Contract:CompanyTaxCode"] ?? "0123456789",
            CompanyPhone = _configuration["Contract:CompanyPhone"] ?? "1900-xxxx",
            CompanyEmail = _configuration["Contract:CompanyEmail"] ?? "contact@bookingcare.vn",

            // Hospital info from registration
            HospitalName = registration.HospitalName,
            HospitalAddress = registration.Address,
            HospitalTaxCode = registration.TaxCode,
            HospitalPhone = registration.HospitalPhone,
            HospitalEmail = registration.HospitalEmail,
            RepresentativeName = registration.RepresentativeName,
            RepresentativeEmail = registration.RepresentativeEmail,
            RepresentativePhone = registration.RepresentativePhone
        };
    }

    #endregion
}
