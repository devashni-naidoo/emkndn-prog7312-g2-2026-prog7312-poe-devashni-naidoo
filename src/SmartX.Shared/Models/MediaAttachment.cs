namespace SmartX.Shared.Models;

public sealed class MediaAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedUtc { get; set; } = DateTimeOffset.UtcNow;
}
