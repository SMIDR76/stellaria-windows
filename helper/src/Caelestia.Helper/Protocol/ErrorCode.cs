namespace Caelestia.Helper.Protocol;

internal static class ErrorCode
{
    public const string InvalidRequest = "invalid_request";
    public const string UnknownMethod = "unknown_method";
    public const string InvalidParams = "invalid_params";
    public const string NotFound = "not_found";
    public const string Unsupported = "unsupported";
    public const string Denied = "denied";
    public const string RateLimited = "rate_limited";
    public const string Failed = "failed";
}

internal sealed class HelperException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
