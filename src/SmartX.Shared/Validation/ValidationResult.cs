namespace SmartX.Shared.Validation;

public readonly struct ValidationResult
{
    public bool Success { get; }
    public string Message { get; }

    private ValidationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static ValidationResult Ok(string path) => new(true, $"Valid hierarchy: {path}");
    public static ValidationResult Fail(string reason) => new(false, reason);
}
