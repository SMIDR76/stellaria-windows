using System.Globalization;
using System.Text;

namespace Caelestia.Helper.Hosting;

internal enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

internal sealed class Log : IDisposable
{
    public const long MaxFileBytes = 2 * 1024 * 1024;
    public const int KeptArchives = 3;

    private readonly Lock _gate = new();
    private readonly string _path;
    private readonly LogLevel _minimum;
    private StreamWriter? _writer;

    public Log(string directory, LogLevel minimum)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "helper.log");
        _minimum = minimum;
    }

    public void Debug(string source, string message) => Write(LogLevel.Debug, source, message, null);

    public void Info(string source, string message) => Write(LogLevel.Info, source, message, null);

    public void Warning(string source, string message, Exception? exception = null) => Write(LogLevel.Warning, source, message, exception);

    public void Error(string source, string message, Exception? exception = null) => Write(LogLevel.Error, source, message, exception);

    public void Write(LogLevel level, string source, string message, Exception? exception)
    {
        if (level < _minimum)
        {
            return;
        }

        var line = Format(DateTimeOffset.Now, level, source, message, exception);
        lock (_gate)
        {
            try
            {
                RotateIfNeeded();
                _writer ??= OpenWriter();
                _writer.WriteLine(line);
            }
            catch (IOException)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }

    public static string Format(DateTimeOffset time, LogLevel level, string source, string message, Exception? exception)
    {
        var builder = new StringBuilder();
        builder.Append(time.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        builder.Append(' ').Append(level.ToString().ToUpperInvariant().PadRight(7));
        builder.Append(" [").Append(source).Append("] ").Append(message);
        if (exception is not null)
        {
            builder.Append(Environment.NewLine).Append(exception);
        }

        return builder.ToString();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private StreamWriter OpenWriter()
    {
        var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        return new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    private void RotateIfNeeded()
    {
        var info = new FileInfo(_path);
        if (!info.Exists || info.Length < MaxFileBytes)
        {
            return;
        }

        _writer?.Dispose();
        _writer = null;
        for (var index = KeptArchives - 1; index >= 1; index--)
        {
            var older = $"{_path}.{index}";
            if (File.Exists(older))
            {
                File.Move(older, $"{_path}.{index + 1}", overwrite: true);
            }
        }

        File.Move(_path, $"{_path}.1", overwrite: true);
    }
}
