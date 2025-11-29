using Microsoft.AspNetCore.Http;

namespace BookingCare.Shared.FileUpload.Wrappers;

/// <summary>
/// Wrapper class to convert Stream to IFormFile for FileUploadOrchestrator
/// Used for uploading files from stream data (e.g., base64 decoded images)
/// Owns the stream and will dispose it when this wrapper is disposed
/// </summary>
public sealed class FormFileWrapper : IFormFile, IDisposable
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly string _contentType;
    private bool _disposed;

    public FormFileWrapper(Stream stream, string fileName, string contentType)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _stream.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public void CopyTo(Stream target)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0;
        _stream.CopyTo(target);
        _stream.Position = 0;
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0;
        await _stream.CopyToAsync(target, cancellationToken);
        _stream.Position = 0;
    }

    public Stream OpenReadStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0;
        return _stream;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }
}
