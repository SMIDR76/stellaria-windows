using System.Text.Json.Nodes;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Tests.Server;

public sealed class StateHubTests
{
    [Fact]
    public void Publishing_an_identical_value_twice_is_ignored()
    {
        var hub = new StateHub();

        Assert.True(hub.Publish("idle", new JsonObject { ["seconds"] = 30 }));
        Assert.False(hub.Publish("idle", new JsonObject { ["seconds"] = 30 }));
        Assert.True(hub.Publish("idle", new JsonObject { ["seconds"] = 35 }));
    }

    [Fact]
    public void The_latest_value_of_each_topic_is_kept()
    {
        var hub = new StateHub();

        hub.Publish("shell", new JsonObject { ["taskbarHidden"] = true });
        hub.Publish("shell", new JsonObject { ["taskbarHidden"] = false });

        var stored = JsonNode.Parse(hub.CurrentState("shell")!)!;
        Assert.False(stored["data"]!["taskbarHidden"]!.GetValue<bool>());
        Assert.Null(hub.CurrentState("unknown"));
    }
}
