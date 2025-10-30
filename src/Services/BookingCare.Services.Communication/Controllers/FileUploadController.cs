using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller để xử lý file uploads cho chat messages với API versioning
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class FileUploadController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger
    )
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload single file cho message attachment
    /// </summary>
    [HttpPost("upload")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] MessageType messageType
    )
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không được để trống");
        }

        try
        {
            // Validate file trước khi upload
            var validation = await _fileUploadService.ValidateFileAsync(file, messageType);
            if (!validation.IsValid)
            {
                return BadRequest(new { Errors = validation.Errors });
            }

            // Upload file
            var result = await _fileUploadService.UploadFileAsync(file, userId, messageType);

            return Success(result, "Upload file thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi upload file: {FileName}", file.FileName);
            return BadRequest("Lỗi khi upload file");
        }
    }

    /// <summary>
    /// Upload multiple files cùng lúc
    /// </summary>
    [HttpPost("upload-multiple")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadMultipleFiles(
        [FromForm] IEnumerable<IFormFile> files,
        [FromForm] string userId,
        [FromForm] MessageType messageType
    )
    {
        if (!files.Any())
        {
            return BadRequest("Danh sách files không được để trống");
        }

        try
        {
            var results = await _fileUploadService.UploadMultipleFilesAsync(
                files,
                userId,
                messageType
            );
            return Success(results, "Upload files thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi upload multiple files");
            return BadRequest("Lỗi khi upload files");
        }
    }

    /// <summary>
    /// Tạo presigned URL để client upload trực tiếp lên cloud
    /// </summary>
    [HttpPost("presigned-url")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GeneratePresignedUrl([FromBody] PresignedUrlRequest request)
    {
        try
        {
            var result = await _fileUploadService.GeneratePresignedUrlAsync(
                request.FileName,
                request.ContentType,
                request.UserId,
                request.MessageType
            );

            return Success(result, "Tạo presigned URL thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo presigned URL");
            return BadRequest("Lỗi khi tạo presigned URL");
        }
    }

    /// <summary>
    /// Generate thumbnail cho image/video
    /// </summary>
    [HttpPost("generate-thumbnail")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GenerateThumbnail([FromBody] ThumbnailRequest request)
    {
        try
        {
            var thumbnailUrl = await _fileUploadService.GenerateThumbnailAsync(request.OriginalUrl);

            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                return BadRequest("Không thể tạo thumbnail cho file này");
            }

            return Success(new { ThumbnailUrl = thumbnailUrl }, "Tạo thumbnail thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo thumbnail");
            return BadRequest("Lỗi khi tạo thumbnail");
        }
    }

    /// <summary>
    /// Xóa file từ cloud storage
    /// </summary>
    [HttpDelete("delete")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteFile([FromBody] DeleteFileRequest request)
    {
        try
        {
            var result = await _fileUploadService.DeleteFileAsync(request.FileUrl);

            if (!result)
            {
                return NotFound("File không tồn tại hoặc đã bị xóa");
            }

            return Success(new { Deleted = true }, "Xóa file thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa file");
            return BadRequest("Lỗi khi xóa file");
        }
    }
}

/// <summary>
/// Request để tạo presigned URL
/// </summary>
public class PresignedUrlRequest
{
    public required string FileName { get; set; } = string.Empty;
    public required string ContentType { get; set; } = string.Empty;
    public required string UserId { get; set; } = string.Empty;
    public required MessageType MessageType { get; set; }
}

/// <summary>
/// Request để tạo thumbnail
/// </summary>
public class ThumbnailRequest
{
    public required string OriginalUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request để xóa file
/// </summary>
public class DeleteFileRequest
{
    public required string FileUrl { get; set; } = string.Empty;
}
