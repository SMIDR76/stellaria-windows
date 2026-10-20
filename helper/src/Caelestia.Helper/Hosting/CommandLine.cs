namespace Caelestia.Helper.Hosting;

internal sealed record CommandLine(bool Quit, bool Verbose, string? PackDirectory)
{
    public static CommandLine Parse(IReadOnlyList<string> arguments)
    {
        var quit = false;
        var verbose = false;
        string? pack = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "--quit":
                    quit = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--pack" when index + 1 < arguments.Count:
                    pack = Path.GetFullPath(arguments[++index]);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arguments[index]}'.");
            }
        }

        return new CommandLine(quit, verbose, pack);
    }
}
