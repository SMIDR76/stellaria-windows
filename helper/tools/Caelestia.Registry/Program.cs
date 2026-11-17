using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Caelestia.Helper.Catalog;
using Caelestia.Helper.Security;

namespace Caelestia.Registry;

internal static class Program
{
    private const string KeyVariable = "CAELESTIA_SIGNING_KEY";

    private static readonly JsonSerializerOptions s_indented = new() { WriteIndented = true };

    private static int Main(string[] args)
    {
        try
        {
            return args switch
            {
                ["keygen", var directory] => KeyGen(directory),
                ["validate", var registry] => Validate(registry),
                ["build", var registry, var output, .. var rest] => Build(registry, output, rest),
                ["update", var setup, var version, var output] => Update(setup, version, output),
                _ => Usage(),
            };
        }
        catch (Exception exception) when (exception is IOException or JsonException or FormatException or InvalidOperationException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("caelestia-registry keygen <directory>");
        Console.Error.WriteLine("caelestia-registry validate <registry>");
        Console.Error.WriteLine($"caelestia-registry build <registry> <output> [--expires-days 14]   (signing key in ${KeyVariable})");
        Console.Error.WriteLine($"caelestia-registry update <setup.exe> <version> <output>   (signing key in ${KeyVariable})");
        return 2;
    }

    private static int KeyGen(string directory)
    {
        Directory.CreateDirectory(directory);
        var secret = Path.Combine(directory, "caelestia-registry.key");
        if (File.Exists(secret))
        {
            throw new InvalidOperationException($"{secret} already exists; delete it first to replace the key.");
        }

        var key = MinisignSecretKey.Generate();
        File.WriteAllText(secret, key.Render() + "\n");
        File.WriteAllText(Path.Combine(directory, "caelestia-registry.pub"), key.PublicKey.Render());
        Console.WriteLine($"Secret key: {secret} (store it as the {KeyVariable} secret, never commit it)");
        Console.WriteLine($"Public key: {key.PublicKey.Render().Split('\n')[1]}");
        return 0;
    }

    private static int Validate(string registry)
    {
        var (items, errors) = Load(registry);
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }

        Console.WriteLine($"{items.Count} items, {errors.Count} problems.");
        return errors.Count == 0 ? 0 : 1;
    }

    private static int Build(string registry, string output, string[] options)
    {
        var (items, errors) = Load(registry);
        if (errors.Count > 0)
        {
            errors.ForEach(Console.Error.WriteLine);
            return 1;
        }

        var keyText = Environment.GetEnvironmentVariable(KeyVariable) ?? throw new InvalidOperationException($"Set {KeyVariable} to the registry signing key.");
        var key = MinisignSecretKey.Parse(keyText);
        var days = options is ["--expires-days", var value] ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : 14;
        var now = DateTimeOffset.UtcNow;
        var version = now.ToUnixTimeSeconds();
        var expires = now.AddDays(days);
        var news = File.Exists(Path.Combine(registry, "news.json")) ? JsonNode.Parse(File.ReadAllText(Path.Combine(registry, "news.json"))) : new JsonArray();
        var index = new JsonObject
        {
            ["schema"] = SignedIndex.Schema,
            ["version"] = version,
            ["generated"] = now.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            ["items"] = new JsonArray([.. items.OrderBy(item => item["id"]!.GetValue<string>(), StringComparer.Ordinal).Select(item => item.DeepClone())]),
            ["news"] = news,
        };

        Directory.CreateDirectory(output);
        var bytes = Encoding.UTF8.GetBytes(index.ToJsonString(s_indented) + "\n");
        File.WriteAllBytes(Path.Combine(output, "index.json"), bytes);
        File.WriteAllText(Path.Combine(output, "index.json.minisig"), Minisign.Sign(bytes, key, SignedIndex.TrustedComment(version, expires)));
        Console.WriteLine($"index.json version {version}, {items.Count} items, expires {expires:yyyy-MM-dd}.");
        return 0;
    }

    private static int Update(string setup, string version, string output)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^[0-9]+\.[0-9]+\.[0-9]+$"))
        {
            throw new FormatException($"'{version}' is not a release version (x.y.z).");
        }

        var keyText = Environment.GetEnvironmentVariable(KeyVariable) ?? throw new InvalidOperationException($"Set {KeyVariable} to the registry signing key.");
        var key = MinisignSecretKey.Parse(keyText);
        var data = File.ReadAllBytes(setup);
        var manifest = new JsonObject
        {
            ["schema"] = 1,
            ["version"] = version,
            ["published"] = DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            ["setup"] = new JsonObject
            {
                ["name"] = Path.GetFileName(setup),
                ["size"] = data.LongLength,
                ["sha256"] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data)),
            },
        };

        Directory.CreateDirectory(output);
        var bytes = Encoding.UTF8.GetBytes(manifest.ToJsonString(s_indented) + "\n");
        File.WriteAllBytes(Path.Combine(output, "update.json"), bytes);
        File.WriteAllText(Path.Combine(output, "update.json.minisig"), Minisign.Sign(bytes, key, $"caelestia-update version={version}"));
        Console.WriteLine($"update.json for {version} ({Path.GetFileName(setup)}).");
        return 0;
    }

    private static (List<JsonObject> Items, List<string> Errors) Load(string registry)
    {
        var items = new List<JsonObject>();
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(registry, "items"), "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (JsonNode.Parse(File.ReadAllText(file)) is not JsonObject item)
            {
                errors.Add($"{name}: not a JSON object.");
                continue;
            }

            errors.AddRange(CatalogItems.Validate(item).Select(error => $"{name}: {error}"));
            var id = item["id"]?.GetValue<string>();
            if (id != name)
            {
                errors.Add($"{name}: the file must be named after its id.");
            }
            else if (!ids.Add(id))
            {
                errors.Add($"{name}: duplicate id.");
            }

            items.Add(item);
        }

        return (items, errors);
    }
}
