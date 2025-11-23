using System;
using System.IO;
using System.Threading.Tasks;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers
{
    /// <summary>
    /// Example ASP.NET Core controller for handling call recordings
    /// This demonstrates how to receive the audio file from the React frontend
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CallRecordingsController : ControllerBase
    {
        private readonly ILogger<CallRecordingsController> _logger;
        private readonly string _uploadPath;
        private readonly long _maxFileSizeBytes;
        private const string InternalServerErrorMessage = "Internal server error";

        public CallRecordingsController(
            ILogger<CallRecordingsController> logger,
            IConfiguration configuration
        )
        {
            _logger = logger;
            // Configure upload path in appsettings.json or use environment variable
            _uploadPath =
                configuration["CallRecordings:UploadPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "call-recordings");

            // Ensure directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            // Read and validate max file size from configuration using shared helper
            _maxFileSizeBytes = FileSizeLimitConfiguration.GetMaxFileSizeBytes(
                configuration,
                _logger,
                "CallRecordings:MaxFileSizeBytes"
            );
        }

        /// <summary>
        /// Upload a call recording
        /// </summary>
        /// <param name="file">The audio file (WebM format)</param>
        /// <param name="appointmentId">The appointment ID</param>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>Upload result with recording metadata</returns>
        [HttpPost("upload")]
        [RequestSizeLimit(FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
        [RequestFormLimits(
            MultipartBodyLengthLimit = FileSizeLimitConfiguration.DefaultFileSizeBytes
        )] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
        [ProducesResponseType(typeof(UploadRecordingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadRecording(
            [FromForm] IFormFile file,
            [FromForm] string appointmentId,
            [FromForm] string conversationId
        )
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(appointmentId))
                {
                    return BadRequest(
                        new { success = false, message = "AppointmentId is required" }
                    );
                }

                if (string.IsNullOrEmpty(conversationId))
                {
                    return BadRequest(
                        new { success = false, message = "ConversationId is required" }
                    );
                }

                // Validate audio file with extended MIME types for call recordings
                var allowedMimeTypes = new[]
                {
                    "audio/webm",
                    "audio/ogg",
                    "audio/mp4",
                    "audio/mpeg",
                };
                var validationResult = AudioFileValidator.ValidateAudioFile(
                    file,
                    _maxFileSizeBytes,
                    allowedMimeTypes
                );

                if (!validationResult.IsValid)
                {
                    AudioFileValidator.LogValidationResult(validationResult, _logger);
                    return BadRequest(
                        new { success = false, message = validationResult.ErrorMessage }
                    );
                }

                AudioFileValidator.LogValidationResult(validationResult, _logger, file);

                _logger.LogInformation(
                    "Uploading call recording: AppointmentId={AppointmentId}, Size={Size} bytes, Type={ContentType}",
                    appointmentId,
                    file.Length,
                    file.ContentType
                );

                // Generate unique file ID
                var recordingId = Guid.NewGuid().ToString();
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{recordingId}{fileExtension}";
                var filePath = Path.Combine(_uploadPath, fileName);

                // Save file to disk
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Call recording saved: {FilePath}", filePath);

                var response = new UploadRecordingResponse
                {
                    Success = true,
                    Data = new RecordingData
                    {
                        RecordingId = recordingId,
                        FileUrl = $"/api/call-recordings/{recordingId}",
                        Duration = 0,
                        FileSize = file.Length,
                    },
                    Message = "Recording uploaded successfully",
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading call recording");
                return StatusCode(
                    500,
                    new { success = false, message = InternalServerErrorMessage }
                );
            }
        }

        /// <summary>
        /// Download a call recording by ID
        /// </summary>
        [HttpGet("{recordingId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadRecording(string recordingId)
        {
            try
            {
                var files = Directory.GetFiles(_uploadPath, $"{recordingId}.*");
                if (files.Length == 0)
                {
                    return NotFound(new { success = false, message = "Recording not found" });
                }

                var filePath = files[0];
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var contentType = "audio/webm";

                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error downloading call recording: {RecordingId}",
                    recordingId
                );
                return StatusCode(
                    500,
                    new { success = false, message = InternalServerErrorMessage }
                );
            }
        }

        /// <summary>
        /// Delete a call recording
        /// </summary>
        [HttpDelete("{recordingId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRecording(string recordingId)
        {
            try
            {
                var files = Directory.GetFiles(_uploadPath, $"{recordingId}.*");
                if (files.Length == 0)
                {
                    return NotFound(new { success = false, message = "Recording not found" });
                }

                // Delete file
                System.IO.File.Delete(files[0]);

                _logger.LogInformation("Call recording deleted: {RecordingId}", recordingId);
                return Ok(new { success = true, message = "Recording deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call recording: {RecordingId}", recordingId);
                return StatusCode(
                    500,
                    new { success = false, message = InternalServerErrorMessage }
                );
            }
        }

        /// <summary>
        /// Get recording metadata by appointment ID
        /// </summary>
        [HttpGet("appointment/{appointmentId}")]
        [ProducesResponseType(typeof(UploadRecordingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRecordingByAppointment(string appointmentId)
        {
            try
            {
                return NotFound(new { success = false, message = "Recording not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting call recording for appointment: {AppointmentId}",
                    appointmentId
                );
                return StatusCode(
                    500,
                    new { success = false, message = InternalServerErrorMessage }
                );
            }
        }
    }

    #region DTOs

    public class UploadRecordingResponse
    {
        public bool Success { get; set; }
        public RecordingData? Data { get; set; }
        public string? Message { get; set; }
    }

    public class RecordingData
    {
        public string RecordingId { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long Duration { get; set; }
        public long FileSize { get; set; }
    }

    #endregion
}
