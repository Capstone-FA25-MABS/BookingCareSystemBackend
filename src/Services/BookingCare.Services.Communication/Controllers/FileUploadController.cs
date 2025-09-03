using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller ?? x? lý file uploads cho chat messages
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileUploadController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload single file cho message attachment
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] MessageType messageType)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không ???c ?? tr?ng");
        }

        try
        {
            // Validate file tr??c khi upload
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
            _logger.LogError(ex, "L?i khi upload file: {FileName}", file.FileName);
            return BadRequest("L?i khi upload file");
        }
    }

    /// <summary>
    /// Upload multiple files cùng lúc
    /// </summary>
    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultipleFiles(
        [FromForm] IEnumerable<IFormFile> files,
        [FromForm] string userId,
        [FromForm] MessageType messageType)
    {
        if (!files.Any())
        {
            return BadRequest("Danh sách files không ???c ?? tr?ng");
        }

        try
        {
            var results = await _fileUploadService.UploadMultipleFilesAsync(files, userId, messageType);
            return Success(results, "Upload files thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi upload multiple files");
            return BadRequest("L?i khi upload files");
        }
    }

    /// <summary>
    /// T?o presigned URL ?? client upload tr?c ti?p lên cloud
    /// </summary>
    [HttpPost("presigned-url")]
    public async Task<IActionResult> GeneratePresignedUrl([FromBody] PresignedUrlRequest request)
    {
        try
        {
            var result = await _fileUploadService.GeneratePresignedUrlAsync(
                request.FileName, 
                request.ContentType, 
                request.UserId, 
                request.MessageType);

            return Success(result, "T?o presigned URL thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi t?o presigned URL");
            return BadRequest("L?i khi t?o presigned URL");
        }
    }

    /// <summary>
    /// Generate thumbnail cho image/video
    /// </summary>
    [HttpPost("generate-thumbnail")]
    public async Task<IActionResult> GenerateThumbnail([FromBody] ThumbnailRequest request)
    {
        try
        {
            var thumbnailUrl = await _fileUploadService.GenerateThumbnailAsync(request.OriginalUrl);
            
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                return BadRequest("Không th? t?o thumbnail cho file này");
            }

            return Success(new { ThumbnailUrl = thumbnailUrl }, "T?o thumbnail thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi t?o thumbnail");
            return BadRequest("L?i khi t?o thumbnail");
        }
    }

    /// <summary>
    /// Xóa file t? cloud storage
    /// </summary>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile([FromBody] DeleteFileRequest request)
    {
        try
        {
            var result = await _fileUploadService.DeleteFileAsync(request.FileUrl);
            
            if (!result)
            {
                return NotFound("File không t?n t?i ho?c ?ã b? xóa");
            }

            return Success(new { Deleted = true }, "Xóa file thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi xóa file");
            return BadRequest("L?i khi xóa file");
        }
    }
}

/// <summary>
/// Request ?? t?o presigned URL
/// </summary>
public class PresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
}

/// <summary>
/// Request ?? t?o thumbnail
/// </summary>
public class ThumbnailRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request ?? xóa file
/// </summary>
public class DeleteFileRequest
{
    public string FileUrl { get; set; } = string.Empty;
}