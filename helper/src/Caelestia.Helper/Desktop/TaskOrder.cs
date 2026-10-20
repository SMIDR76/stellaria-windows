namespace Caelestia.Helper.Desktop;

internal static class TaskOrder
{
    public static List<nint> Merge(IReadOnlyList<nint> previousOrder, IReadOnlyList<nint> presentInZOrder)
    {
        var present = new HashSet<nint>(presentInZOrder);
        var order = previousOrder.Where(present.Contains).ToList();
        var known = new HashSet<nint>(order);
        foreach (var hwnd in presentInZOrder.Reverse())
        {
            if (known.Add(hwnd))
            {
                order.Add(hwnd);
            }
        }

        return order;
    }
}
