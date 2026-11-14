using System.Xml;
using Caelestia.Helper.Modules;

namespace Caelestia.Helper.Tests.Modules;

public sealed class NotificationsTests
{
    [Fact]
    public void The_first_text_line_is_the_title_and_the_others_the_body()
    {
        var (title, body) = NotificationsModule.ParsePayload(
            "<toast><visual><binding template=\"ToastGeneric\"><text>Nouveau message</text><text>Salut !</text><text>À plus</text></binding></visual></toast>");

        Assert.Equal("Nouveau message", title);
        Assert.Equal("Salut !\nÀ plus", body);
    }

    [Fact]
    public void The_group_header_is_used_when_there_is_no_text()
    {
        var (title, body) = NotificationsModule.ParsePayload(
            "<toast><header id=\"a\" title=\"Projet\" arguments=\"\"/><visual><binding template=\"ToastGeneric\"></binding></visual></toast>");

        Assert.Equal("Projet", title);
        Assert.Equal("", body);
    }

    [Fact]
    public void Document_type_definitions_are_refused()
    {
        Assert.ThrowsAny<XmlException>(() => NotificationsModule.ParsePayload(
            "<!DOCTYPE toast [<!ENTITY x \"boom\">]><toast><visual><binding><text>&x;</text></binding></visual></toast>"));
    }

    [Fact]
    public void Long_texts_are_clipped()
    {
        var (title, _) = NotificationsModule.ParsePayload($"<toast><visual><binding><text>{new string('a', 5000)}</text></binding></visual></toast>");

        Assert.Equal(NotificationsModule.MaxTextLength, title.Length);
    }

    [Fact]
    public void File_times_become_unix_milliseconds()
    {
        var expected = new DateTimeOffset(2026, 11, 15, 0, 4, 16, TimeSpan.Zero);

        Assert.Equal(expected.ToUnixTimeMilliseconds(), NotificationsModule.FileTimeToUnixMilliseconds(expected.UtcDateTime.ToFileTimeUtc()));
    }
}
