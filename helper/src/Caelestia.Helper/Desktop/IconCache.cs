using System.Collections.Concurrent;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Imaging;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Desktop;

internal sealed class IconCache(Log log)
{
    public const int IconSize = 48;

    private readonly ConcurrentDictionary<string, Task<string?>> _icons = new(StringComparer.OrdinalIgnoreCase);

    public Task<string?> GetAsync(string parsingName) =>
        string.IsNullOrEmpty(parsingName) ? Task.FromResult<string?>(null) : _icons.GetOrAdd(parsingName, LoadAsync);

    public async Task<Dictionary<string, string>> ResolveAsync(IEnumerable<string> parsingNames)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in parsingNames.Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await GetAsync(name).ConfigureAwait(false) is { } icon)
            {
                result[name] = icon;
            }
        }

        return result;
    }

    private async Task<string?> LoadAsync(string parsingName)
    {
        try
        {
            var image = await Task.Run(() => GdiBitmaps.ReadAndDelete(ShellItemImages.IconBitmap(parsingName, IconSize))).ConfigureAwait(false);
            return image is null ? null : Convert.ToBase64String(await ImageCodec.EncodePngAsync(image, IconSize, IconSize).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            log.Warning("icons", "Could not extract an application icon.", exception);
            return null;
        }
    }
}
