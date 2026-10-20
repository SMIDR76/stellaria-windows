using System.Text.Json.Nodes;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Tests.Protocol;

public sealed class MessagesTests
{
    [Fact]
    public void A_request_is_parsed_with_its_params()
    {
        var message = ClientMessage.Parse("""{"kind":"request","id":"7","method":"tasks.close","params":{"id":"123"}}""");

        var request = Assert.IsType<RequestMessage>(message);
        Assert.Equal("7", request.Id);
        Assert.Equal("tasks.close", request.Method);
        Assert.Equal("123", request.Params["id"]!.GetValue<string>());
    }

    [Fact]
    public void A_request_without_params_gets_an_empty_object()
    {
        var request = Assert.IsType<RequestMessage>(ClientMessage.Parse("""{"kind":"request","id":"1","method":"apps.list"}"""));

        Assert.Empty(request.Params);
    }

    [Fact]
    public void An_auth_message_carries_token_and_identity()
    {
        var auth = Assert.IsType<AuthMessage>(ClientMessage.Parse("""{"kind":"auth","token":"t0k","widget":"bar","monitor":"\\\\.\\DISPLAY1"}"""));

        Assert.Equal("t0k", auth.Token);
        Assert.Equal("bar", auth.Widget);
        Assert.Equal(@"\\.\DISPLAY1", auth.Monitor);
    }

    [Fact]
    public void An_auth_message_without_token_is_invalid()
    {
        Assert.IsType<InvalidMessage>(ClientMessage.Parse("""{"kind":"auth","widget":"bar"}"""));
    }

    [Fact]
    public void A_heartbeat_reports_visibility()
    {
        var heartbeat = Assert.IsType<HeartbeatMessage>(ClientMessage.Parse("""{"kind":"heartbeat","visible":true}"""));

        Assert.True(heartbeat.Visible);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"kind":"teleport"}""")]
    [InlineData("""{"kind":"request","id":"","method":"x"}""")]
    [InlineData("""{"kind":"request","id":"1","method":"x","params":[1]}""")]
    public void Malformed_messages_are_reported_as_invalid(string text)
    {
        Assert.IsType<InvalidMessage>(ClientMessage.Parse(text));
    }

    [Fact]
    public void A_failure_reply_carries_a_stable_code()
    {
        var reply = JsonNode.Parse(Messages.ReplyFailure("9", ErrorCode.NotFound, "Window is gone."))!;

        Assert.Equal("reply", reply["kind"]!.GetValue<string>());
        Assert.False(reply["ok"]!.GetValue<bool>());
        Assert.Equal("not_found", reply["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public void State_messages_copy_the_data_instead_of_moving_it()
    {
        var data = new JsonObject { ["seconds"] = 30 };

        var first = Messages.State("idle", data);
        var second = Messages.State("idle", data);

        Assert.Equal(first, second);
        Assert.Null(data.Parent);
    }
}
