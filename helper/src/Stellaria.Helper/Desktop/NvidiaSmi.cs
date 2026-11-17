using System.Diagnostics;
using System.Globalization;

namespace Stellaria.Helper.Desktop;

internal sealed record NvidiaReading(string Name, double Usage, double Temperature);

internal static class NvidiaSmi
{
    public const string Arguments = "--query-gpu=name,utilization.gpu,temperature.gpu --format=csv,noheader,nounits";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public static string? Locate()
    {
        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe"),
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    public static NvidiaReading? Parse(string output)
    {
        var line = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var fields = line?.Split(',').Select(field => field.Trim()).ToArray();
        if (fields is not { Length: 3 } ||
            !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var usage) ||
            !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
        {
            return null;
        }

        return new NvidiaReading(fields[0], usage, temperature);
    }

    public static async Task<NvidiaReading?> ReadAsync(string executable, CancellationToken cancellation)
    {
        var start = new ProcessStartInfo(executable, Arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start);
        if (process is null)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(Timeout);
        try
        {
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return process.ExitCode == 0 ? Parse(output) : null;
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            return null;
        }
    }
}
