namespace SmartX.Shared.Models;

/// <summary>
/// Metadata for a config file, deployment photo or hardware log attached to a
/// sensor profile, as required by the Media/Log Attachment functional
/// requirement. The binary content itself is streamed straight to disk by the
/// API's multipart upload endpoint; only the resulting metadata is kept here.
/// </summary>
public sealed class MediaAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedUtc { get; set; } = DateTimeOffset.UtcNow;
}
