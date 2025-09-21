using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.FileUpload.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IFileUploadService fileUploadService, ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResult>> UploadFile(
        IFormFile file,
        [FromForm] string? folder = null,
        [FromForm] bool generateUniqueFileName = true,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
            var request = new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = folder,
                GenerateUniqueFileName = generateUniqueFileName
            };

            var result = await _fileUploadService.UploadFileAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload-multiple")]
    public async Task<ActionResult<MultipleFileUploadResult>> UploadMultipleFiles(
        IFormFileCollection files,
        [FromForm] string? folder = null,
        [FromForm] bool generateUniqueFileName = true,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        try
        {
            var requests = files.Select(file => new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = folder,
                GenerateUniqueFileName = generateUniqueFileName
            });

            var result = await _fileUploadService.UploadMultipleFilesAsync(requests, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate presigned URL for direct upload to S3
    /// </summary>
    [HttpPost("presigned-upload-url")]
    public async Task<ActionResult<PresignedUrlResult>> GeneratePresignedUploadUrl(
        [FromBody] PresignedUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.GeneratePresignedUploadUrlAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned upload URL for file: {FileName}", request.FileName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate presigned URL for downloading from S3
    /// </summary>
    [HttpGet("presigned-download-url/{*s3Key}")]
    public async Task<ActionResult<PresignedUrlResult>> GeneratePresignedDownloadUrl(
        string s3Key,
        [FromQuery] int expiryHours = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.GeneratePresignedDownloadUrlAsync(s3Key, expiryHours, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned download URL for S3 key: {S3Key}", s3Key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a file from S3
    /// </summary>
    [HttpDelete("{*s3Key}")]
    public async Task<ActionResult<bool>> DeleteFile(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.DeleteFileAsync(s3Key, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3. Key: {S3Key}", s3Key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete multiple files from S3
    /// </summary>
    [HttpDelete("multiple")]
    public async Task<ActionResult<Dictionary<string, bool>>> DeleteMultipleFiles(
        [FromBody] string[] s3Keys,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.DeleteMultipleFilesAsync(s3Keys, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple files from S3");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if file exists in S3
    /// </summary>
    [HttpHead("{*s3Key}")]
    public async Task<ActionResult> FileExists(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _fileUploadService.FileExistsAsync(s3Key, cancellationToken);
            return exists ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists in S3. Key: {S3Key}", s3Key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get file info from S3
    /// </summary>
    [HttpGet("info/{*s3Key}")]
    public async Task<ActionResult<FileUploadResult>> GetFileInfo(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.GetFileInfoAsync(s3Key, cancellationToken);
            
            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info from S3. Key: {S3Key}", s3Key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Invalidate CloudFront cache for specific files
    /// </summary>
    [HttpPost("invalidate-cache")]
    public async Task<ActionResult<bool>> InvalidateCloudFrontCache(
        [FromBody] string[] paths,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileUploadService.InvalidateCloudFrontCacheAsync(paths, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating CloudFront cache");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get CloudFront URL for an S3 key
    /// </summary>
    [HttpGet("cloudfront-url")]
    public ActionResult<string> GetCloudFrontUrl([FromQuery] string s3Key)
    {
        try
        {
            var url = _fileUploadService.GetCloudFrontUrl(s3Key);
            return Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CloudFront URL for S3 key: {S3Key}", s3Key);
            return StatusCode(500, "Internal server error");
        }
    }
}