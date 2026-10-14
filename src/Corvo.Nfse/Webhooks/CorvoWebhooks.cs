using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Corvo.Nfse;

/// <summary>Webhook payload signature verification (HMAC-SHA256, constant-time).</summary>
public static partial class CorvoWebhooks
{
    [GeneratedRegex("^sha256=([0-9a-f]{64})$")]
    private static partial Regex SignatureRegex();

    /// <summary>
    /// Verifies an <c>X-Corvo-Signature</c> header (<c>sha256=&lt;hex&gt;</c>) against the
    /// raw request body using HMAC-SHA256 and a constant-time comparison.
    /// Use the raw bytes — parsing the JSON first and re-serializing will never match.
    /// </summary>
    /// <param name="rawBody">The exact bytes the server signed.</param>
    /// <param name="signatureHeader">The signature header value, e.g. <c>sha256=8f9b...</c>.</param>
    /// <param name="secret">The webhook signing secret (e.g. <c>whsec_...</c>).</param>
    /// <returns><c>true</c> when the signature is valid.</returns>
    public static bool VerifySignature(byte[]? rawBody, string? signatureHeader, string? secret)
    {
        if (rawBody is null || secret is null || secret.Length == 0 || signatureHeader is null)
        {
            return false;
        }

        var match = SignatureRegex().Match(signatureHeader.Trim());
        if (!match.Success)
        {
            return false;
        }

        byte[] received;
        try
        {
            received = Convert.FromHexString(match.Groups[1].Value);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] expected;
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            expected = hmac.ComputeHash(rawBody);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            return false;
        }

        return received.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(received, expected);
    }

    /// <summary>Verifies a signature header against the UTF-8 bytes of the raw body.</summary>
    /// <param name="rawBody">The exact body the server signed.</param>
    /// <param name="signatureHeader">The signature header value, e.g. <c>sha256=8f9b...</c>.</param>
    /// <param name="secret">The webhook signing secret.</param>
    /// <returns><c>true</c> when the signature is valid.</returns>
    public static bool VerifySignature(string? rawBody, string? signatureHeader, string? secret)
        => rawBody is not null &&
           VerifySignature(Encoding.UTF8.GetBytes(rawBody), signatureHeader, secret);
}
