namespace Corvo.Nfse;

/// <summary>A binary file returned by the API (invoice PDF/XML downloads).</summary>
/// <param name="Buffer">The raw bytes of the file.</param>
/// <param name="MimeType">The <c>Content-Type</c> reported by the server, when present.</param>
/// <param name="FileName">The file name from <c>Content-Disposition</c>, when present.</param>
public sealed record DownloadedFile(byte[] Buffer, string? MimeType, string? FileName);
