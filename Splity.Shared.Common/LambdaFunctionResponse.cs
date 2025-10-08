namespace Splity.Shared.Common;

public class LambdaFunctionResponse
{
    public string? ErrorMessage { get; set; }
    public bool Success { get; set; } = true;
    public object? Data { get; set; }
}