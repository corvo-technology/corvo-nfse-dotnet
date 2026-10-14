namespace Corvo.Nfse;

/// <summary>Cursor pagination metadata returned with every list.</summary>
/// <param name="Limit">Page size that was applied.</param>
/// <param name="HasMore">Whether another page exists.</param>
/// <param name="NextCursor">Opaque base64url cursor for the next page; <c>null</c> on the last page.</param>
public sealed record Pagination(int Limit, bool HasMore, string? NextCursor);

/// <summary>A cursor-paginated list response.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Data">The page items.</param>
/// <param name="Pagination">Cursor metadata.</param>
public sealed record Paginated<T>(IReadOnlyList<T> Data, Pagination Pagination);
