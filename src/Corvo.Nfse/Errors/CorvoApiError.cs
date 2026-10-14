namespace Corvo.Nfse;

/// <summary>
/// A single field-level validation error returned by the API in the <c>details</c> array.
/// </summary>
/// <param name="Field">The offending field name.</param>
/// <param name="Code">Stable machine-readable code for this field error.</param>
/// <param name="Message">Human-readable description.</param>
public sealed record CorvoErrorDetail(string Field, string Code, string Message);

/// <summary>
/// The typed error carried by <see cref="Result{T}.Error"/>. Every API, network, or timeout
/// failure is surfaced as this value instead of an exception.
/// </summary>
/// <param name="Status">HTTP status code; <c>0</c> for network and timeout failures.</param>
/// <param name="Code">
/// Stable error code from the server (e.g. <c>ERROR_VALIDATION</c>), falling back to
/// <c>http_&lt;status&gt;</c>; <c>network_error</c> or <c>timeout</c> for transport failures.
/// </param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Field-level details for body validation errors, when present.</param>
/// <param name="RequestId">Request id from the body or the <c>X-Request-Id</c> header.</param>
/// <param name="RetryAfter">Seconds to wait before retrying (429), when the server said so.</param>
public sealed record CorvoApiError(
    int Status,
    string Code,
    string Message,
    IReadOnlyList<CorvoErrorDetail>? Details = null,
    string? RequestId = null,
    int? RetryAfter = null)
{
    /// <summary>True when the request never reached the API (DNS, connection, TLS, ...).</summary>
    public bool IsNetworkError => Status == 0 && Code == "network_error";

    /// <summary>True when the request exceeded the configured timeout.</summary>
    public bool IsTimeout => Status == 0 && Code == "timeout";

    /// <inheritdoc />
    public override string ToString() => $"CorvoApiError {Status} {Code}: {Message}";
}
