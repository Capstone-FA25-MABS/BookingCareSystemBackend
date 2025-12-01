using Microsoft.AspNetCore.Http;

namespace BookingCare.Shared.FileUpload.Wrappers;

/// <summary>
/// Wrapper class to provide a sanitized file name for IFormFile
/// This prevents issues with non-ASCII characters in HTTP headers when uploading to S3
/// </summary>
public sealed class SanitizedFormFileWrapper : IFormFile
{
    private readonly IFormFile _originalFile;
    private readonly string _sanitizedFileName;

    public SanitizedFormFileWrapper(IFormFile originalFile, string sanitizedFileName)
    {
        _originalFile = originalFile ?? throw new ArgumentNullException(nameof(originalFile));
        _sanitizedFileName = sanitizedFileName ?? throw new ArgumentNullException(nameof(sanitizedFileName));
    }

    public string ContentType => _originalFile.ContentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_sanitizedFileName}\"";
    public IHeaderDictionary Headers => _originalFile.Headers;
    public long Length => _originalFile.Length;
    public string Name => _originalFile.Name;
    public string FileName => _sanitizedFileName;

    public void CopyTo(Stream target) => _originalFile.CopyTo(target);

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        => _originalFile.CopyToAsync(target, cancellationToken);

    public Stream OpenReadStream() => _originalFile.OpenReadStream();
}
