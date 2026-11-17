using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Modules;

public sealed class DesktopsTests
{
    private static readonly Guid First = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Second = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static byte[] Ids(params Guid[] ids) => ids.SelectMany(id => id.ToByteArray()).ToArray();

    [Fact]
    public void Without_registry_data_there_is_a_single_current_desktop()
    {
        var snapshot = DesktopsModule.Parse(null, null, _ => null);

        Assert.Single(snapshot.Items);
        Assert.Equal(0, snapshot.Current);
    }

    [Fact]
    public void Desktops_keep_the_registry_order_and_names()
    {
        var snapshot = DesktopsModule.Parse(Ids(First, Second), Second.ToByteArray(), id => id == Second ? "Jeux" : null);

        Assert.Equal([First, Second], snapshot.Items.Select(item => item.Id));
        Assert.Equal([null, "Jeux"], snapshot.Items.Select(item => item.Name));
        Assert.Equal(1, snapshot.Current);
    }

    [Fact]
    public void An_unknown_current_desktop_falls_back_to_the_first()
    {
        var snapshot = DesktopsModule.Parse(Ids(First, Second), Guid.NewGuid().ToByteArray(), _ => null);

        Assert.Equal(0, snapshot.Current);
    }

    [Fact]
    public void Truncated_registry_data_is_ignored()
    {
        var snapshot = DesktopsModule.Parse([1, 2, 3], null, _ => null);

        Assert.Single(snapshot.Items);
    }
}
