namespace Corvo.Nfse;

/// <summary>How the transport unwraps a successful JSON response body.</summary>
public enum UnwrapMode
{
    /// <summary><c>{ "data": ... }</c> → the value of <c>data</c> (or <c>null</c> when missing).</summary>
    Data,

    /// <summary>Keep the whole body (used for lists and raw health checks).</summary>
    None,
}
