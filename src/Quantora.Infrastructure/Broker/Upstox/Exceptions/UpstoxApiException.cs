namespace Quantora.Infrastructure.Broker.Upstox.Exceptions;

public sealed class UpstoxApiException : Exception
{
    public int StatusCode { get; }
    public UpstoxApiException(int statusCode, string message)
        : base(message) => StatusCode = statusCode;
}
