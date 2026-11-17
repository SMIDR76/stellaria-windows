using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Caelestia.Helper.Security;

namespace Caelestia.Helper.Catalog;

internal sealed record IndexEnvelope(long Version, DateTimeOffset Expires, JsonObject Document);

internal static class SignedIndex
{
    public const int Schema = 1;

    public static string TrustedComment(long version, DateTimeOffset expires) =>
        $"caelestia-index version={version} expires={expires.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}";

    public static IndexEnvelope Open(byte[] index, string signature, MinisignPublicKey key, long lastVersion, DateTimeOffset now)
    {
        var comment = Minisign.Verify(index, signature, key);
        var fields = comment.Split(' ')
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.Ordinal);
        if (!comment.StartsWith("caelestia-index ", StringComparison.Ordinal) ||
            !fields.TryGetValue("version", out var versionText) || !long.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out var version) ||
            !fields.TryGetValue("expires", out var expiresText) ||
            !DateTimeOffset.TryParse(expiresText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires))
        {
            throw new CryptographicException("The index signature does not carry a version and an expiry date.");
        }

        if (version < lastVersion)
        {
            throw new CryptographicException($"The index is older ({version}) than the one already seen ({lastVersion}).");
        }

        if (expires <= now)
        {
            throw new CryptographicException("The index has expired.");
        }

        if (JsonNode.Parse(index) is not JsonObject document || document["schema"]?.GetValue<int>() != Schema ||
            document["version"]?.GetValue<long>() != version)
        {
            throw new CryptographicException("The index content does not match its signature metadata.");
        }

        return new IndexEnvelope(version, expires, document);
    }
}
