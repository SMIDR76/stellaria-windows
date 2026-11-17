using System.Security.Cryptography;
using System.Text;
using Caelestia.Helper.Catalog;
using Caelestia.Helper.Security;

namespace Caelestia.Helper.Tests.Security;

public sealed class MinisignTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly MinisignSecretKey Key = MinisignSecretKey.Generate();

    [Fact]
    public void A_signature_verifies_and_returns_its_trusted_comment()
    {
        var data = Encoding.UTF8.GetBytes("hello");

        var signature = Minisign.Sign(data, Key, "release 1");

        Assert.Equal("release 1", Minisign.Verify(data, signature, Key.PublicKey));
    }

    [Fact]
    public void Altered_data_comments_or_keys_are_refused()
    {
        var data = Encoding.UTF8.GetBytes("hello");
        var signature = Minisign.Sign(data, Key, "release 1");

        Assert.Throws<CryptographicException>(() => Minisign.Verify(Encoding.UTF8.GetBytes("hellO"), signature, Key.PublicKey));
        Assert.Throws<CryptographicException>(() => Minisign.Verify(data, signature.Replace("release 1", "release 2", StringComparison.Ordinal), Key.PublicKey));
        Assert.Throws<CryptographicException>(() => Minisign.Verify(data, signature, MinisignSecretKey.Generate().PublicKey));
    }

    [Fact]
    public void Keys_round_trip_through_their_text_forms()
    {
        var secret = MinisignSecretKey.Parse(Key.Render());
        var pub = MinisignPublicKey.Parse(Key.PublicKey.Render());

        Assert.Equal(Key.PublicKey.Key, secret.PublicKey.Key);
        Assert.Equal(Key.KeyId, pub.KeyId);
    }

    [Fact]
    public void A_fresh_index_opens()
    {
        var (index, signature) = Index(7, Now.AddDays(14));

        var opened = SignedIndex.Open(index, signature, Key.PublicKey, 6, Now);

        Assert.Equal(7, opened.Version);
        Assert.Equal("caelestia", opened.Document["items"]![0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public void Expired_older_or_mismatched_indexes_are_refused()
    {
        var (expired, expiredSignature) = Index(7, Now.AddMinutes(-1));
        var (current, currentSignature) = Index(7, Now.AddDays(1));
        var mismatched = Encoding.UTF8.GetBytes("""{"schema":1,"version":9,"items":[]}""");
        var mismatchedSignature = Minisign.Sign(mismatched, Key, SignedIndex.TrustedComment(7, Now.AddDays(1)));

        Assert.Throws<CryptographicException>(() => SignedIndex.Open(expired, expiredSignature, Key.PublicKey, 0, Now));
        Assert.Throws<CryptographicException>(() => SignedIndex.Open(current, currentSignature, Key.PublicKey, 8, Now));
        Assert.Throws<CryptographicException>(() => SignedIndex.Open(mismatched, mismatchedSignature, Key.PublicKey, 0, Now));
    }

    private static (byte[] Index, string Signature) Index(long version, DateTimeOffset expires)
    {
        var index = Encoding.UTF8.GetBytes($$"""{"schema":1,"version":{{version}},"items":[{"id":"caelestia"}]}""");
        return (index, Minisign.Sign(index, Key, SignedIndex.TrustedComment(version, expires)));
    }
}
