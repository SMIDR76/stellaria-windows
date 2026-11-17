using Stellaria.Helper.Desktop;
using Stellaria.Helper.Modules;
using Stellaria.Helper.Protocol;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class TaskOrderTests
{
    [Fact]
    public void The_first_listing_puts_the_bottom_of_the_z_order_first()
    {
        Assert.Equal([3, 2, 1], TaskOrder.Merge([], [1, 2, 3]));
    }

    [Fact]
    public void Known_windows_keep_their_place_when_the_z_order_changes()
    {
        Assert.Equal([3, 2, 1], TaskOrder.Merge([3, 2, 1], [3, 1, 2]));
    }

    [Fact]
    public void New_windows_are_appended_and_closed_windows_removed()
    {
        Assert.Equal([3, 1, 4], TaskOrder.Merge([3, 2, 1], [4, 1, 3]));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("12abc")]
    [InlineData(" 12")]
    public void Task_ids_must_be_positive_integers(string id)
    {
        var exception = Assert.Throws<HelperException>(() => TasksModule.ParseId(id));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void A_valid_task_id_is_parsed_to_its_handle()
    {
        Assert.Equal((nint)131234, TasksModule.ParseId("131234"));
    }
}
