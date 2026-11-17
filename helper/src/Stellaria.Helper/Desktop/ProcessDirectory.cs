using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Desktop;

internal sealed record ProcessIdentity(uint Id, string ImagePath, long CreationTime, string AppId = "")
{
    public string Name => Path.GetFileNameWithoutExtension(ImagePath);
}

internal sealed class ProcessDirectory
{
    private readonly Lock _gate = new();
    private readonly Dictionary<uint, ProcessIdentity> _cache = [];

    public ProcessIdentity? Find(uint processId)
    {
        if (processId == 0)
        {
            return null;
        }

        var process = Kernel32.OpenProcess(Kernel32.ProcessQueryLimitedInformation, false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            if (!Kernel32.GetProcessTimes(process, out var creation, out _, out _, out _))
            {
                return null;
            }

            lock (_gate)
            {
                if (_cache.TryGetValue(processId, out var cached) && cached.CreationTime == creation)
                {
                    return cached;
                }
            }

            var identity = new ProcessIdentity(processId, ImagePath(process), creation, Kernel32.ApplicationUserModelId(process));
            lock (_gate)
            {
                _cache[processId] = identity;
            }

            return identity;
        }
        finally
        {
            Kernel32.CloseHandle(process);
        }
    }

    public string NameOf(uint processId) => Find(processId)?.Name ?? "";

    private static unsafe string ImagePath(nint process)
    {
        var buffer = stackalloc char[1024];
        uint size = 1024;
        return Kernel32.QueryFullProcessImageName(process, 0, buffer, ref size) ? new string(buffer, 0, (int)size) : "";
    }
}
