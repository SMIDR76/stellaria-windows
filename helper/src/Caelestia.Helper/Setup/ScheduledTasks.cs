using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace Caelestia.Helper.Setup;

internal sealed record LogonTask(string Name, string Executable, string WorkingDirectory, TimeSpan Delay);

internal static class ScheduledTasks
{
    public const string HelperTask = "Caelestia - Helper";
    public const string SwitcherTask = "Caelestia - Selecteur Alt+Tab";
    public const string ZebarTask = "Caelestia - Zebar";

    public static readonly string[] All = [HelperTask, SwitcherTask, ZebarTask];

    public static string Xml(LogonTask task, string user)
    {
        var delay = $"PT{(int)task.Delay.TotalSeconds}S";
        return $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Description>Caelestia for Windows</Description>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{Escape(user)}</UserId>
                  <Delay>{delay}</Delay>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{Escape(user)}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <StartWhenAvailable>true</StartWhenAvailable>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <Priority>7</Priority>
                <Enabled>true</Enabled>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{Escape(task.Executable)}</Command>
                  <WorkingDirectory>{Escape(task.WorkingDirectory)}</WorkingDirectory>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    public static string CurrentUser() => WindowsIdentity.GetCurrent().Name;

    public static void Register(LogonTask task)
    {
        var file = Path.Combine(Path.GetTempPath(), $"caelestia-task-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(file, Xml(task, CurrentUser()), Encoding.Unicode);
            Run("/Create", "/TN", task.Name, "/XML", file, "/F");
        }
        finally
        {
            File.Delete(file);
        }
    }

    public static void Delete(string name) => Run(true, "/Delete", "/TN", name, "/F");

    public static void End(string name) => Run(true, "/End", "/TN", name);

    public static void Start(string name) => Run("/Run", "/TN", name);

    public static bool Exists(string name) => Run(true, "/Query", "/TN", name) == 0;

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static void Run(params string[] arguments)
    {
        var exit = Run(false, arguments);
        if (exit != 0)
        {
            throw new InvalidOperationException($"schtasks {arguments[0]} failed for '{arguments[2]}' (exit code {exit}).");
        }
    }

    private static int Run(bool quiet, params string[] arguments)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("schtasks could not be started.");
        process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 && !quiet && error.Length > 0)
        {
            Console.Error.WriteLine(error.Trim());
        }

        return process.ExitCode;
    }
}
