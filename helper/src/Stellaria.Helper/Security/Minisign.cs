using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Stellaria.Helper.Security;

internal sealed record MinisignPublicKey(byte[] KeyId, byte[] Key)
{
    public static MinisignPublicKey Parse(string text)
    {
        var line = text.Split('\n').Select(part => part.Trim()).LastOrDefault(part => part.Length > 0 && !part.StartsWith("untrusted comment:", StringComparison.Ordinal))
            ?? throw new FormatException("The public key is empty.");
        var bytes = Convert.FromBase64String(line);
        if (bytes.Length != 42 || bytes[0] != 'E' || bytes[1] != 'd')
        {
            throw new FormatException("This is not a minisign Ed25519 public key.");
        }

        return new MinisignPublicKey(bytes[2..10], bytes[10..42]);
    }

    public string Render() => "untrusted comment: Stellaria public key\n" + Convert.ToBase64String([(byte)'E', (byte)'d', .. KeyId, .. Key]) + "\n";
}

internal sealed record MinisignSecretKey(byte[] KeyId, byte[] Seed)
{
    public static MinisignSecretKey Generate()
    {
        var seed = RandomNumberGenerator.GetBytes(Ed25519PrivateKeyParameters.KeySize);
        return new MinisignSecretKey(RandomNumberGenerator.GetBytes(8), seed);
    }

    public static MinisignSecretKey Parse(string text)
    {
        var bytes = Convert.FromBase64String(text.Trim());
        if (bytes.Length != 40)
        {
            throw new FormatException("A Stellaria signing key is 8 bytes of key id followed by a 32 byte Ed25519 seed, in base64.");
        }

        return new MinisignSecretKey(bytes[..8], bytes[8..]);
    }

    public string Render() => Convert.ToBase64String([.. KeyId, .. Seed]);

    public MinisignPublicKey PublicKey => new(KeyId, new Ed25519PrivateKeyParameters(Seed).GeneratePublicKey().GetEncoded());
}

internal static class Minisign
{
    public const string TrustedPrefix = "trusted comment: ";

    public static string Sign(byte[] data, MinisignSecretKey key, string trustedComment)
    {
        if (trustedComment.Contains('\n', StringComparison.Ordinal))
        {
            throw new ArgumentException("A trusted comment fits on one line.", nameof(trustedComment));
        }

        var signature = Ed25519(Blake2b(data), key.Seed);
        var global = Ed25519([.. signature, .. Encoding.UTF8.GetBytes(trustedComment)], key.Seed);
        return "untrusted comment: signature from the Stellaria key\n" +
            Convert.ToBase64String([(byte)'E', (byte)'D', .. key.KeyId, .. signature]) + "\n" +
            TrustedPrefix + trustedComment + "\n" +
            Convert.ToBase64String(global) + "\n";
    }

    public static string Verify(byte[] data, string signatureFile, MinisignPublicKey key)
    {
        var lines = signatureFile.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        if (lines.Length < 4 || !lines[2].StartsWith(TrustedPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("The signature file is malformed.");
        }

        var header = Convert.FromBase64String(lines[1].Trim());
        if (header.Length != 74 || header[0] != 'E' || (header[1] != 'D' && header[1] != 'd'))
        {
            throw new CryptographicException("The signature is not a minisign Ed25519 signature.");
        }

        if (!header.AsSpan(2, 8).SequenceEqual(key.KeyId))
        {
            throw new CryptographicException("The signature was made with another key.");
        }

        var signature = header[10..];
        var message = header[1] == 'D' ? Blake2b(data) : data;
        if (!Verify(message, signature, key.Key))
        {
            throw new CryptographicException("The signature does not match the data.");
        }

        var trustedComment = lines[2][TrustedPrefix.Length..];
        if (!Verify([.. signature, .. Encoding.UTF8.GetBytes(trustedComment)], Convert.FromBase64String(lines[3].Trim()), key.Key))
        {
            throw new CryptographicException("The trusted comment was altered.");
        }

        return trustedComment;
    }

    private static byte[] Blake2b(byte[] data)
    {
        var digest = new Blake2bDigest(512);
        digest.BlockUpdate(data, 0, data.Length);
        var hash = new byte[64];
        digest.DoFinal(hash, 0);
        return hash;
    }

    private static byte[] Ed25519(byte[] message, byte[] seed)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, new Ed25519PrivateKeyParameters(seed));
        signer.BlockUpdate(message, 0, message.Length);
        return signer.GenerateSignature();
    }

    private static bool Verify(byte[] message, byte[] signature, byte[] publicKey)
    {
        if (signature.Length != 64 || publicKey.Length != 32)
        {
            return false;
        }

        var verifier = new Ed25519Signer();
        verifier.Init(false, new Ed25519PublicKeyParameters(publicKey));
        verifier.BlockUpdate(message, 0, message.Length);
        return verifier.VerifySignature(signature);
    }
}
