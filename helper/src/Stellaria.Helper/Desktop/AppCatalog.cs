using System.Text.RegularExpressions;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Desktop;

internal static partial class AppCatalog
{
    public static bool IsLaunchable(ShellApp app) => !NoiseName().IsMatch(app.Name) && !DocumentTarget().IsMatch(app.Id);

    public static List<ShellApp> Filter(IEnumerable<ShellApp> apps)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return [.. apps.Where(IsLaunchable).Where(app => seen.Add(app.Id)).OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    [GeneratedRegex(@"^(uninstall|désinstaller|desinstaller|readme|lisezmoi|help|aide|documentation|website|site web)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoiseName();

    [GeneratedRegex(@"\.(url|chm|txt|html?|pdf|ini|log)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DocumentTarget();
}
