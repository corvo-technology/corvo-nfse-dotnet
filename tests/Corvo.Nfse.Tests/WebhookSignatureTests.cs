namespace Corvo.Nfse.Tests;

public class WebhookSignatureTests
{
    private const string Body = """{"type":"nfse.invoice.authorized","data":{"id":"inv_1"}}""";
    private const string Secret = "whsec_test_123";
    private const string Signature = "sha256=8f9bd040d8f717b19618ab3395ddfa4b8c176b8dc863195227b88dd85426bbbc";

    [Fact]
    public void Accepts_the_known_HMAC_SHA256_vector()
    {
        Assert.True(CorvoWebhooks.VerifySignature(Body, Signature, Secret));
        Assert.True(CorvoWebhooks.VerifySignature(Body, $"  {Signature}  ", Secret));
        Assert.True(CorvoWebhooks.VerifySignature(
            System.Text.Encoding.UTF8.GetBytes(Body),
            Signature,
            Secret));
    }

    [Fact]
    public void Rejects_a_tampered_body()
    {
        Assert.False(CorvoWebhooks.VerifySignature(Body + " ", Signature, Secret));
        Assert.False(CorvoWebhooks.VerifySignature("""{"type":"nfse.invoice.rejected","data":{"id":"inv_1"}}""", Signature, Secret));
    }

    [Fact]
    public void Rejects_the_wrong_secret()
    {
        Assert.False(CorvoWebhooks.VerifySignature(Body, Signature, "whsec_other"));
        Assert.False(CorvoWebhooks.VerifySignature(Body, Signature, string.Empty));
        Assert.False(CorvoWebhooks.VerifySignature(Body, Signature, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("8f9bd040d8f717b19618ab3395ddfa4b8c176b8dc863195227b88dd85426bbbc")]
    [InlineData("sha256=not-hex-not-hex-not-hex-not-hex-not-hex-not-hex-not-hex-not-")]
    [InlineData("sha1=8f9bd040d8f717b19618ab3395ddfa4b8c176b8dc863195227b88dd85426bbbc")]
    [InlineData("sha256=8F9BD040D8F717B19618AB3395DDFA4B8C176B8DC863195227B88DD85426BBBC")]
    public void Rejects_malformed_signature_headers(string? header)
    {
        Assert.False(CorvoWebhooks.VerifySignature(Body, header, Secret));
    }

    [Fact]
    public void Rejects_null_body()
    {
        Assert.False(CorvoWebhooks.VerifySignature((string)null!, Signature, Secret));
        Assert.False(CorvoWebhooks.VerifySignature((byte[])null!, Signature, Secret));
    }

    [Fact]
    public void Webhook_event_catalog_matches_the_contract()
    {
        Assert.Equal(9, WebhookEvents.All.Count);
        Assert.Contains(WebhookEvents.WebhookTest, WebhookEvents.All);
        Assert.Contains(WebhookEvents.InvoiceAuthorized, WebhookEvents.All);
        Assert.Contains(WebhookEvents.InvoiceEmailFailed, WebhookEvents.All);
    }
}
