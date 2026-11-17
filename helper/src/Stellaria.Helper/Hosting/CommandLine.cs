namespace Stellaria.Helper.Hosting;

internal sealed record CommandLine(
    bool Quit,
    bool Verbose,
    string? PackDirectory,
    bool RestoreShell = false,
    string? Setup = null,
    string? Zebar = null,
    string? Switcher = null)
{
    public static readonly string[] SetupVerbs = ["install", "uninstall", "shutdown", "start"];

    public static CommandLine Parse(IReadOnlyList<string> arguments)
    {
        var quit = false;
        var verbose = false;
        var restoreShell = false;
        string? pack = null;
        string? setup = null;
        string? zebar = null;
        string? switcher = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            switch (argument)
            {
                case "--quit":
                    quit = true;
                    break;
                case "--restore-shell":
                    restoreShell = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--pack" when index + 1 < arguments.Count:
                    pack = Path.GetFullPath(arguments[++index]);
                    break;
                case "--zebar" when index + 1 < arguments.Count:
                    zebar = Path.GetFullPath(arguments[++index]);
                    break;
                case "--switcher" when index + 1 < arguments.Count:
                    switcher = Path.GetFullPath(arguments[++index]);
                    break;
                case not null when argument.StartsWith("--", StringComparison.Ordinal) && SetupVerbs.Contains(argument[2..]):
                    if (setup is not null)
                    {
                        throw new ArgumentException("Only one setup command can be given.");
                    }

                    setup = argument[2..];
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        return new CommandLine(quit, verbose, pack, restoreShell, setup, zebar, switcher);
    }
}
