using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Services;
using BookingCare.Services.AI.Workflows;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class VoiceController : BaseApiController
{
    private readonly ILogger<VoiceController> _logger;
    private readonly IGeminiTranscriptionService _transcriptionService;
    private readonly IAudioTranscriptionWorkflow _audioWorkflow;
    private readonly string _audioTempPath;
    private readonly long _maxFileSizeBytes;
    private const string InvalidFilenameMessage = "Invalid filename";

    public VoiceController(
        ILogger<VoiceController> logger,
        IWebHostEnvironment environment,
        IGeminiTranscriptionService transcriptionService,
        IAudioTranscriptionWorkflow audioWorkflow,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _transcriptionService = transcriptionService;
        _audioWorkflow = audioWorkflow;

        // Set up audio temp directory
        _audioTempPath = Path.Combine(environment.WebRootPath ?? "wwwroot", "audio-temp");

        // Ensure directory exists
        if (!Directory.Exists(_audioTempPath))
        {
            Directory.CreateDirectory(_audioTempPath);
            _logger.LogInformation("Created audio-temp directory: {Path}", _audioTempPath);
        }

        // Read and validate max file size from configuration using shared helper
        _maxFileSizeBytes = FileSizeLimitConfiguration.GetMaxFileSizeBytes(
            configuration,
            _logger,
            "Voice:MaxFileSizeBytes"
        );
    }

    /// <summary>
    /// Upload an audio file for processing
    /// </summary>
    /// <param name="audio">The audio file (WebM or WAV format)</param>
    /// <returns>Upload result with file path</returns>
    /// <response code="200">Audio file uploaded successfully</response>
    /// <response code="400">Invalid file or unsupported format</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("upload")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [RequestSizeLimit(FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [RequestFormLimits(MultipartBodyLengthLimit = FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAudio([FromForm] IFormFile audio)
    {
        try
        {
            // Validate audio file
            var validationResult = AudioFileValidator.ValidateAudioFile(audio, _maxFileSizeBytes);
            if (!validationResult.IsValid)
            {
                AudioFileValidator.LogValidationResult(validationResult, _logger);
                return BadRequest(new { message = validationResult.ErrorMessage });
            }

            AudioFileValidator.LogValidationResult(validationResult, _logger, audio);

            _logger.LogInformation(
                "Uploading audio file: Name={FileName}, Size={Size} bytes, Type={ContentType}",
                audio.FileName,
                audio.Length,
                audio.ContentType
            );

            // Generate unique filename
            var fileExtension = Path.GetExtension(audio.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_audioTempPath, uniqueFileName);

            // Save file asynchronously
            await using (
                var stream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true
                )
            )
            {
                await audio.CopyToAsync(stream);
            }

            _logger.LogInformation("Audio file saved successfully: {FilePath}", filePath);

            // Return relative path for API response
            var relativePath = Path.Combine("audio-temp", uniqueFileName).Replace("\\", "/");

            return Success(
                new
                {
                    filePath = relativePath,
                    fileName = uniqueFileName,
                    originalFileName = audio.FileName,
                    fileSize = audio.Length,
                    contentType = audio.ContentType,
                    uploadedAt = DateTime.UtcNow,
                },
                "Audio file uploaded successfully"
            );
        }
        catch (IOException ioEx)
        {
            _logger.LogError(ioEx, "IO error while uploading audio file");
            return StatusCode(500, new { message = "Error saving audio file" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading audio file");
            return StatusCode(
                500,
                new { message = "An unexpected error occurred while uploading the audio file" }
            );
        }
    }

    /// <summary>
    /// Delete a temporary audio file
    /// </summary>
    /// <param name="fileName">The name of the file to delete</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("temp/{fileName}")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public IActionResult DeleteTempAudio(string fileName)
    {
        try
        {
            // Validate filename (prevent directory traversal)
            if (
                string.IsNullOrWhiteSpace(fileName)
                || fileName.Contains("..")
                || fileName.Contains("/")
                || fileName.Contains("\\")
            )
            {
                _logger.LogWarning("Invalid filename provided for deletion: {FileName}", fileName);
                return BadRequest(new { message = InvalidFilenameMessage });
            }

            // Sanitize filename to prevent path traversal
            var sanitizedFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_audioTempPath, sanitizedFileName);

            // Verify the resulting path is within the expected directory
            var fullPath = Path.GetFullPath(filePath);
            var expectedDirectory = Path.GetFullPath(_audioTempPath);
            if (!fullPath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FileName}", fileName);
                return BadRequest(new { message = InvalidFilenameMessage });
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Deletion attempt for non-existent file: {FilePath}", filePath);
                return NotFound(new { message = "Audio file not found" });
            }

            System.IO.File.Delete(filePath);
            _logger.LogInformation("Temporary audio file deleted: {FilePath}", filePath);

            return Success(new { fileName }, "Audio file deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting temporary audio file: {FileName}", fileName);
            return StatusCode(500, new { message = "Error deleting audio file" });
        }
    }

    /// <summary>
    /// Transcribe audio file to text using Gemini API
    /// </summary>
    /// <param name="fileName">The name of the uploaded audio file in audio-temp directory</param>
    /// <returns>Transcribed text</returns>
    /// <response code="200">Transcription successful</response>
    /// <response code="400">Invalid filename</response>
    /// <response code="404">Audio file not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("transcribe/{fileName}")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TranscribeAudio(
        string fileName,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate filename (prevent directory traversal)
            if (
                string.IsNullOrWhiteSpace(fileName)
                || fileName.Contains("..")
                || fileName.Contains("/")
                || fileName.Contains("\\")
            )
            {
                _logger.LogWarning(
                    "Invalid filename provided for transcription: {FileName}",
                    fileName
                );
                return BadRequest(new { message = InvalidFilenameMessage });
            }

            // Sanitize filename to prevent path traversal
            var sanitizedFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_audioTempPath, sanitizedFileName);

            // Verify the resulting path is within the expected directory
            var fullPath = Path.GetFullPath(filePath);
            var expectedDirectory = Path.GetFullPath(_audioTempPath);
            if (!fullPath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FileName}", fileName);
                return BadRequest(new { message = InvalidFilenameMessage });
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning(
                    "Transcription attempt for non-existent file: {FilePath}",
                    filePath
                );
                return NotFound(new { message = "Audio file not found" });
            }

            _logger.LogInformation("Starting transcription for file: {FileName}", fileName);

            // Call Gemini API for transcription
            var transcript = await _transcriptionService.TranscribeAudioAsync(
                filePath,
                cancellationToken
            );

            _logger.LogInformation(
                "Transcription successful: File={FileName}, Length={Length} characters",
                fileName,
                transcript.Length
            );

            return Success(
                new
                {
                    fileName,
                    transcript,
                    transcriptLength = transcript.Length,
                    transcribedAt = DateTime.UtcNow,
                },
                "Audio transcription completed successfully"
            );
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Audio file not found: {FileName}", fileName);
            return NotFound(new { message = "Audio file not found" });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Unsupported audio format: {FileName}", fileName);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Transcription failed: {FileName}", fileName);
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transcription: {FileName}", fileName);
            return StatusCode(
                500,
                new { message = "An unexpected error occurred during transcription" }
            );
        }
    }

    /// <summary>
    /// Upload and transcribe audio file in one request
    /// </summary>
    /// <param name="audio">The audio file (WebM or WAV format)</param>
    /// <returns>Upload result and transcription</returns>
    /// <response code="200">Audio uploaded and transcribed successfully</response>
    /// <response code="400">Invalid file or unsupported format</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("upload-and-transcribe")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [RequestSizeLimit(FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [RequestFormLimits(MultipartBodyLengthLimit = FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAndTranscribeAudio(
        [FromForm] IFormFile audio,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate audio file
            var validationResult = AudioFileValidator.ValidateAudioFile(audio, _maxFileSizeBytes);
            if (!validationResult.IsValid)
            {
                AudioFileValidator.LogValidationResult(validationResult, _logger);
                return BadRequest(new { message = validationResult.ErrorMessage });
            }

            AudioFileValidator.LogValidationResult(validationResult, _logger, audio);

            _logger.LogInformation(
                "Uploading and transcribing audio: Name={FileName}, Size={Size} bytes, Type={ContentType}",
                audio.FileName,
                audio.Length,
                audio.ContentType
            );

            // Read file bytes directly for transcription (no need to save to disk)
            byte[] audioBytes;
            await using (var memoryStream = new MemoryStream())
            {
                await audio.CopyToAsync(memoryStream, cancellationToken);
                audioBytes = memoryStream.ToArray();
            }

            // Call Gemini API for transcription
            var transcript = await _transcriptionService.TranscribeAudioBytesAsync(
                audioBytes,
                audio.ContentType,
                cancellationToken
            );

            _logger.LogInformation(
                "Upload and transcription successful: File={FileName}, Transcript length={Length} characters",
                audio.FileName,
                transcript.Length
            );

            return Success(
                new
                {
                    originalFileName = audio.FileName,
                    fileSize = audio.Length,
                    contentType = audio.ContentType,
                    transcript,
                    transcriptLength = transcript.Length,
                    processedAt = DateTime.UtcNow,
                },
                "Audio uploaded and transcribed successfully"
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Transcription failed during upload and transcribe");
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during upload and transcribe");
            return StatusCode(
                500,
                new { message = "An unexpected error occurred during upload and transcription" }
            );
        }
    }

    /// <summary>
    /// Process audio file with complete workflow (recommended)
    /// Validates, saves temporarily, transcribes, and cleans up automatically
    /// </summary>
    /// <param name="audio">The audio file (WebM or WAV format)</param>
    /// <returns>Transcription result</returns>
    /// <response code="200">Audio processed and transcribed successfully</response>
    /// <response code="400">Invalid file or unsupported format</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("process")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [RequestSizeLimit(FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [RequestFormLimits(MultipartBodyLengthLimit = FileSizeLimitConfiguration.DefaultFileSizeBytes)] // Configurable limit (default: 100 MB, max: 500 MB) - validated in constructor
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessAudioForTranscript(
        [FromForm] IFormFile audio,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Early validation
            if (audio == null)
            {
                return BadRequest(new { message = "Audio file is required" });
            }

            _logger.LogInformation(
                "Processing audio file with workflow: Name={FileName}, Size={Size} bytes",
                audio.FileName,
                audio.Length
            );

            // Use workflow to handle complete process
            var transcript = await _audioWorkflow.ProcessAudioForTranscriptAsync(
                audio,
                cancellationToken
            );

            _logger.LogInformation(
                "Audio processing completed: File={FileName}, Transcript length={Length} characters",
                audio.FileName,
                transcript.Length
            );

            return Success(
                new
                {
                    originalFileName = audio.FileName,
                    fileSize = audio.Length,
                    contentType = audio.ContentType,
                    transcript,
                    transcriptLength = transcript.Length,
                    processedAt = DateTime.UtcNow,
                },
                "Audio processed and transcribed successfully"
            );
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "No audio file provided");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid audio file: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported audio format: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Processing failed: {Message}", ex.Message);
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during audio processing");
            return StatusCode(
                500,
                new { message = "An unexpected error occurred while processing the audio file" }
            );
        }
    }

    /// <summary>
    /// Health check for Voice service
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var tempDirExists = Directory.Exists(_audioTempPath);
        var tempFileCount = tempDirExists ? Directory.GetFiles(_audioTempPath).Length : 0;

        return Ok(
            new
            {
                Status = "Healthy",
                Service = "Voice",
                Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
                Timestamp = DateTime.UtcNow,
                AudioTempPath = _audioTempPath,
                TempDirectoryExists = tempDirExists,
                TempFileCount = tempFileCount,
            }
        );
    }
}
