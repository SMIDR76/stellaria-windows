using System.Text.Json.Nodes;
using System.Xml;
using Microsoft.Data.Sqlite;

namespace Stellaria.Helper.Modules;

internal sealed record NotificationEntry(long Id, string AppId, long Time, string Title, string Body);

internal sealed class NotificationsModule : IHelperModule
{
    public const int MaxItems = 50;
    public const int MaxTextLength = 1024;
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan FreshWindow = TimeSpan.FromSeconds(30);

    private static readonly string s_database = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Notifications", "wpndatabase.db");

    private readonly HashSet<long> _known = [];
    private DateTime _lastWrite = DateTime.MinValue;
    private bool _primed;
    private Timer? _poll;

    public string Name => "notifications";

    public Task StartAsync(ModuleContext context)
    {
        _poll = new Timer(_ => Poll(context), null, TimeSpan.Zero, PollInterval);
        context.Shutdown.Register(() => _poll.Dispose());
        return Task.CompletedTask;
    }

    public static (string Title, string Body) ParsePayload(string xml)
    {
        var document = new XmlDocument { XmlResolver = null };
        using (var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            document.Load(reader);
        }

        var texts = document.SelectNodes("//visual/binding/text")?.Cast<XmlNode>()
            .Select(node => node.InnerText.Trim())
            .Where(text => text.Length > 0)
            .ToList() ?? [];
        var header = document.SelectSingleNode("//header/@title")?.Value?.Trim() ?? "";
        var title = texts.Count > 0 ? texts[0] : header;
        var body = string.Join("\n", texts.Skip(1));
        return (Clip(title), Clip(body));
    }

    public static long FileTimeToUnixMilliseconds(long fileTime) =>
        fileTime <= 0 ? 0 : DateTime.FromFileTimeUtc(fileTime).Ticks / TimeSpan.TicksPerMillisecond - DateTime.UnixEpoch.Ticks / TimeSpan.TicksPerMillisecond;

    private static string Clip(string text) => text.Length > MaxTextLength ? text[..MaxTextLength] : text;

    private void Poll(ModuleContext context)
    {
        if (!context.Config.GetFlag("notifications.history"))
        {
            if (_primed)
            {
                _primed = false;
                _known.Clear();
                _lastWrite = DateTime.MinValue;
                context.Hub.Publish("notifications", new JsonObject { ["items"] = new JsonArray() });
            }

            return;
        }

        try
        {
            var write = LastWrite();
            if (write == _lastWrite)
            {
                return;
            }

            _lastWrite = write;
            var entries = Read();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var items = new JsonArray();
            foreach (var entry in entries)
            {
                var item = ToJson(entry);
                items.Add((JsonNode)item);
                if (_primed && !_known.Contains(entry.Id) && now - entry.Time < FreshWindow.TotalMilliseconds)
                {
                    context.Hub.Raise("notification", item.DeepClone());
                }
            }

            _known.Clear();
            _known.UnionWith(entries.Select(entry => entry.Id));
            _primed = true;
            context.Hub.Publish("notifications", new JsonObject { ["items"] = items });
        }
        catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException or XmlException)
        {
            context.Log.Warning("notifications", "The notification history could not be read.", exception);
        }
    }

    private static DateTime LastWrite()
    {
        var wal = s_database + "-wal";
        var main = File.GetLastWriteTimeUtc(s_database);
        return File.Exists(wal) ? new DateTime(Math.Max(main.Ticks, File.GetLastWriteTimeUtc(wal).Ticks), DateTimeKind.Utc) : main;
    }

    private static List<NotificationEntry> Read()
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = s_database, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT n.Id, h.PrimaryId, n.ArrivalTime, n.Payload
            FROM Notification n JOIN NotificationHandler h ON h.RecordId = n.HandlerId
            WHERE n.Type = 'toast'
            ORDER BY n.ArrivalTime DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", MaxItems);
        var entries = new List<NotificationEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var payload = reader.IsDBNull(3) ? "" : System.Text.Encoding.UTF8.GetString((byte[])reader.GetValue(3));
            string title, body;
            try
            {
                (title, body) = ParsePayload(payload);
            }
            catch (XmlException)
            {
                continue;
            }

            if (title.Length == 0 && body.Length == 0)
            {
                continue;
            }

            entries.Add(new NotificationEntry(reader.GetInt64(0), reader.GetString(1), FileTimeToUnixMilliseconds(reader.GetInt64(2)), title, body));
        }

        return entries;
    }

    private static JsonObject ToJson(NotificationEntry entry) => new()
    {
        ["id"] = entry.Id,
        ["appId"] = entry.AppId,
        ["time"] = entry.Time,
        ["title"] = entry.Title,
        ["body"] = entry.Body,
    };
}
