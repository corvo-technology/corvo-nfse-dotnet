using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>
/// The single serializer configuration used for requests and responses.
/// camelCase matches the Node SDK's JSON.stringify defaults; nulls are omitted so that
/// optional request fields behave like TypeScript <c>undefined</c>; reads are
/// case-insensitive so the SDK never re-cases or rejects what the server sent.
/// </summary>
internal static class CorvoJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
