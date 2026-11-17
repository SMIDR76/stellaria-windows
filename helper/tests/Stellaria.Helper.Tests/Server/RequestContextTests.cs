using System.Text.Json.Nodes;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Tests.Server;

public sealed class RequestContextTests
{
    private static RequestContext Context(string json) => new((JsonObject)JsonNode.Parse(json)!, "bar", "");

    [Fact]
    public void Typed_parameters_are_read()
    {
        var context = Context("""{"id":"12","on":true,"value":55}""");

        Assert.Equal("12", context.RequireString("id"));
        Assert.True(context.RequireBoolean("on"));
        Assert.Equal(55, context.RequireInteger("value", 0, 100));
        Assert.Equal(40, context.OptionalInteger("limit", 40, 1, 200));
    }

    [Theory]
    [InlineData("""{"id":""}""")]
    [InlineData("""{"id":5}""")]
    [InlineData("""{}""")]
    public void Missing_or_wrong_strings_are_invalid_params(string json)
    {
        var exception = Assert.Throws<HelperException>(() => Context(json).RequireString("id"));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Out_of_range_integers_are_invalid_params()
    {
        var exception = Assert.Throws<HelperException>(() => Context("""{"value":150}""").RequireInteger("value", 0, 100));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }
}
