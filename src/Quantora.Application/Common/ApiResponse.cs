namespace Quantora.Application.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "") =>
        new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ApiResponse<T> FailureResponse(
        string message,
        IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
}
