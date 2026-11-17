using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Modules;

public sealed class AudioSessionsTests
{
    private static AudioSession Session(string displayName) => new("id", 42, displayName, false, 0.5f, false);

    [Fact]
    public void A_session_display_name_wins()
    {
        Assert.Equal("Spotify", AudioModule.SessionName(Session("Spotify"), new ProcessIdentity(42, @"C:\Apps\spotify.exe", 1)));
    }

    [Fact]
    public void Resource_references_fall_back_to_the_process_name()
    {
        Assert.Equal("chrome", AudioModule.SessionName(Session("@%SystemRoot%\\System32\\AudioSrv.Dll,-202"), new ProcessIdentity(42, @"C:\Apps\chrome.exe", 1)));
    }

    [Fact]
    public void Without_a_process_the_id_is_shown()
    {
        Assert.Equal("PID 42", AudioModule.SessionName(Session(""), null));
    }
}
