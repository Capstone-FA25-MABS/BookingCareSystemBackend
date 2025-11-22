using BookingCare.Services.AI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Workflows;

/// <summary>
/// Service for handling audio transcription workflows
/// </summary>
public interface IAudioTranscriptionWorkflow
{
    /// <summary>
    /// Process audio file and return transcript
    /// Handles validation, temporary storage, transcription, and cleanup
    /// </summary>
    /// <param name="audioFile">Audio file to transcribe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> ProcessAudioForTranscriptAsync(
        IFormFile audioFile,
        CancellationToken cancellationToken = default
    );
}

public class AudioTranscriptionWorkflow : IAudioTranscriptionWorkflow
{
    private readonly IGeminiTranscriptionService _transcriptionService;
    private readonly ILogger<AudioTranscriptionWorkflow> _logger;
    private readonly string _tempDirectory;

    private static readonly string[] AllowedMimeTypes = { "audio/webm", "audio/wav" };
    private static readonly string[] AllowedExtensions = { ".webm", ".wav" };
    private const long MaxFileSizeBytes = 100_000_000; // 100 MB

    public AudioTranscriptionWorkflow(
        IGeminiTranscriptionService transcriptionService,
        ILogger<AudioTranscriptionWorkflow> logger,
        IWebHostEnvironment environment
    )
    {
        _transcriptionService = transcriptionService;
        _logger = logger;

        // Use system temp directory for temporary audio files
        _tempDirectory = Path.Combine(Path.GetTempPath(), "audio-transcription-temp");

        // Ensure temp directory exists
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
            _logger.LogInformation("Created temporary directory: {TempDirectory}", _tempDirectory);
        }
    }

    /// <inheritdoc />
    public async Task<string> ProcessAudioForTranscriptAsync(
        IFormFile audioFile,
        CancellationToken cancellationToken = default
    )
    {
        // Validate input
        ValidateAudioFile(audioFile);

        string? tempFilePath = null;

        try
        {
            // Step 1: Validate audio MIME type
            _logger.LogInformation(
                "Processing audio file: Name={FileName}, Size={Size} bytes, Type={ContentType}",
                audioFile.FileName,
                audioFile.Length,
                audioFile.ContentType
            );

            ValidateMimeType(audioFile);
            ValidateFileExtension(audioFile);

            // Step 2: Save file temporarily
            tempFilePath = await SaveFileTemporarilyAsync(audioFile, cancellationToken);

            _logger.LogInformation("Audio file saved temporarily: {FilePath}", tempFilePath);

            // Step 3: Call TranscribeAudioAsync (Gemini)
            var transcript = await TranscribeAudioAsync(tempFilePath, cancellationToken);

            _logger.LogInformation(
                "Transcription completed successfully: Length={Length} characters",
                transcript.Length
            );

            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing audio file: {FileName}",
                audioFile?.FileName ?? "unknown"
            );
            throw;
        }
        finally
        {
            // Step 4: Delete temporary file
            if (tempFilePath != null)
            {
                await DeleteTemporaryFileAsync(tempFilePath);
            }
        }
    }

    /// <summary>
    /// Validate that audio file is not null or empty
    /// </summary>
    private void ValidateAudioFile(IFormFile audioFile)
    {
        if (audioFile == null)
        {
            _logger.LogWarning("Audio file is null");
            throw new ArgumentNullException(nameof(audioFile), "Audio file is required");
        }

        if (audioFile.Length == 0)
        {
            _logger.LogWarning("Audio file is empty: {FileName}", audioFile.FileName);
            throw new ArgumentException("Audio file cannot be empty", nameof(audioFile));
        }

        if (audioFile.Length > MaxFileSizeBytes)
        {
            _logger.LogWarning(
                "Audio file exceeds maximum size: {Size} bytes (max: {MaxSize} bytes)",
                audioFile.Length,
                MaxFileSizeBytes
            );
            throw new ArgumentException(
                $"Audio file size exceeds maximum limit of {MaxFileSizeBytes / 1_000_000} MB",
                nameof(audioFile)
            );
        }
    }

    /// <summary>
    /// Validate audio file MIME type
    /// </summary>
    private void ValidateMimeType(IFormFile audioFile)
    {
        var mimeType = audioFile.ContentType?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            _logger.LogWarning("Audio file has no MIME type: {FileName}", audioFile.FileName);
            throw new ArgumentException(
                "Audio file must have a valid MIME type",
                nameof(audioFile)
            );
        }

        if (!AllowedMimeTypes.Contains(mimeType))
        {
            _logger.LogWarning(
                "Unsupported MIME type: {MimeType} for file: {FileName}",
                mimeType,
                audioFile.FileName
            );
            throw new NotSupportedException(
                $"Unsupported audio format: {mimeType}. Allowed formats: {string.Join(", ", AllowedMimeTypes)}"
            );
        }

        _logger.LogDebug("MIME type validated: {MimeType}", mimeType);
    }

    /// <summary>
    /// Validate audio file extension
    /// </summary>
    private void ValidateFileExtension(IFormFile audioFile)
    {
        var extension = Path.GetExtension(audioFile.FileName)?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
        {
            _logger.LogWarning("Audio file has no extension: {FileName}", audioFile.FileName);
            throw new ArgumentException(
                "Audio file must have a valid extension",
                nameof(audioFile)
            );
        }

        if (!AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning(
                "Unsupported file extension: {Extension} for file: {FileName}",
                extension,
                audioFile.FileName
            );
            throw new NotSupportedException(
                $"Unsupported file extension: {extension}. Allowed extensions: {string.Join(", ", AllowedExtensions)}"
            );
        }

        _logger.LogDebug("File extension validated: {Extension}", extension);
    }

    /// <summary>
    /// Save audio file to temporary location
    /// </summary>
    private async Task<string> SaveFileTemporarilyAsync(
        IFormFile audioFile,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Generate unique filename with timestamp and GUID
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var extension = Path.GetExtension(audioFile.FileName);
            var tempFileName = $"audio_{timestamp}_{uniqueId}{extension}";
            var tempFilePath = Path.Combine(_tempDirectory, tempFileName);

            _logger.LogDebug("Saving audio file temporarily: {TempFilePath}", tempFilePath);

            // Save file asynchronously with buffer
            await using var fileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );

            await audioFile.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            _logger.LogDebug("Audio file saved: {Size} bytes written", audioFile.Length);

            return tempFilePath;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while saving temporary file");
            throw new InvalidOperationException("Failed to save audio file temporarily", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving temporary file");
            throw;
        }
    }

    /// <summary>
    /// Transcribe audio file using Gemini API
    /// </summary>
    private async Task<string> TranscribeAudioAsync(
        string audioPath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation("Starting transcription for: {AudioPath}", audioPath);

            var transcript = await _transcriptionService.TranscribeAudioAsync(
                audioPath,
                cancellationToken
            );

            if (string.IsNullOrWhiteSpace(transcript))
            {
                _logger.LogWarning(
                    "Transcription returned empty result for: {AudioPath}",
                    audioPath
                );
                throw new InvalidOperationException(
                    "Transcription completed but returned empty result"
                );
            }

            return transcript;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Audio file not found: {AudioPath}", audioPath);
            throw new InvalidOperationException("Temporary audio file not found", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Transcription failed for: {AudioPath}", audioPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transcription: {AudioPath}", audioPath);
            throw new InvalidOperationException("Audio transcription failed", ex);
        }
    }

    /// <summary>
    /// Delete temporary audio file with retry logic
    /// </summary>
    private async Task DeleteTemporaryFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogDebug(
                    "Temporary file already deleted or does not exist: {FilePath}",
                    filePath
                );
                return;
            }

            _logger.LogDebug("Deleting temporary file: {FilePath}", filePath);

            // Retry logic for file deletion (sometimes files are locked briefly)
            const int maxRetries = 3;
            const int delayMs = 100;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Temporary file deleted successfully: {FilePath}", filePath);
                    return;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete temporary file (attempt {Attempt}/{MaxRetries}): {FilePath}",
                        i + 1,
                        maxRetries,
                        filePath
                    );
                    await Task.Delay(delayMs * (i + 1)); // Exponential backoff
                }
            }
        }
        catch (Exception ex)
        {
            // Don't throw - file cleanup failure shouldn't break the workflow
            _logger.LogError(
                ex,
                "Failed to delete temporary file after all retries: {FilePath}. Manual cleanup may be required.",
                filePath
            );
        }
    }

    /// <summary>
    /// Clean up old temporary files (can be called periodically)
    /// </summary>
    public async Task CleanupOldTemporaryFilesAsync(TimeSpan maxAge)
    {
        try
        {
            if (!Directory.Exists(_tempDirectory))
            {
                return;
            }

            _logger.LogInformation(
                "Starting cleanup of old temporary files older than {MaxAge}",
                maxAge
            );

            var cutoffTime = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(_tempDirectory);
            var deletedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffTime)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old temporary file: {FilePath}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old temporary file: {FilePath}", file);
                }
            }

            _logger.LogInformation(
                "Cleanup completed: {DeletedCount} old files deleted",
                deletedCount
            );

            await Task.CompletedTask; // Keep method async for future enhancements
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temporary files cleanup");
        }
    }
}
