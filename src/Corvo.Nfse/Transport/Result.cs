namespace Corvo.Nfse;

/// <summary>
/// The result of an API call. Success carries <see cref="Data"/>; failure carries
/// <see cref="Error"/>. Requests to the API, network failures, and timeouts never throw —
/// they always come back as a <see cref="Result{T}"/>.
/// </summary>
/// <typeparam name="T">The payload type on success.</typeparam>
/// <param name="Data">The unwrapped payload on success; <c>null</c> on failure (or for empty 204 responses).</param>
/// <param name="Error">The typed error on failure; <c>null</c> on success.</param>
public sealed record Result<T>(T? Data, CorvoApiError? Error)
{
    /// <summary>True when the request succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>True when the request failed.</summary>
    public bool IsError => Error is not null;

    /// <summary>Visits exactly one branch of the result.</summary>
    /// <param name="onSuccess">Called with <see cref="Data"/> when the request succeeded.</param>
    /// <param name="onFailure">Called with <see cref="Error"/> when the request failed.</param>
    /// <typeparam name="TResult">The visitor return type.</typeparam>
    public TResult Match<TResult>(
        Func<T?, TResult> onSuccess,
        Func<CorvoApiError, TResult> onFailure)
        => Error is null ? onSuccess(Data) : onFailure(Error);
}
