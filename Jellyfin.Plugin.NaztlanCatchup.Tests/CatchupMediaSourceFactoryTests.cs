using Jellyfin.Plugin.NaztlanCatchup.Configuration;
using Xunit;

namespace Jellyfin.Plugin.NaztlanCatchup.Tests;

public sealed class CatchupMediaSourceFactoryTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc);
    private static readonly CatchupChannel Channel = new("2169", 7, true, "Azteca uno", "tp112169", "http://cdn/index.m3u8");
    private static readonly PluginConfiguration Configuration = new()
    {
        IptvBaseUrl = "http://iptv_tp:8801/",
        MaximumDays = 7,
        MarginBeforeSeconds = 120,
        MarginAfterSeconds = 120,
    };

    [Fact]
    public void PastProgrammeBuildsFiniteSeekableSource()
    {
        var source = CatchupMediaSourceFactory.Create(Guid.Parse("95ff7db7-cdf7-4a43-a8f5-d7740da48b49"), Now.AddHours(-2), Now.AddHours(-1), Channel, Now, Configuration);

        Assert.NotNull(source);
        Assert.Equal("Catchup", source.Name);
        Assert.False(source.IsInfiniteStream);
        Assert.Equal(TimeSpan.FromHours(1).Ticks, source.RunTimeTicks);
        Assert.Equal("http://iptv_tp:8801/dispatcharr/catchup/2169?begin=1788044280&end=1788048120", source.Path);
    }

    [Fact]
    public void AiringProgrammeBuildsStartOverSnapshot()
    {
        var source = CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddMinutes(-30), Now.AddMinutes(30), Channel, Now, Configuration);

        Assert.NotNull(source);
        Assert.Equal("Desde el inicio", source.Name);
        Assert.Equal(TimeSpan.FromMinutes(30).Ticks, source.RunTimeTicks);
        Assert.Contains("end=1788051720", source.Path, StringComparison.Ordinal);
    }

    [Fact]
    public void FutureOrExpiredProgrammeHasNoSource()
    {
        Assert.Null(CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddMinutes(1), Now.AddHours(1), Channel, Now, Configuration));
        Assert.Null(CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddDays(-8), Now.AddDays(-8).AddHours(1), Channel, Now, Configuration));
    }

    [Fact]
    public void StartOverRequiresChannelCapability()
    {
        var withoutStartover = Channel with { Startover = false };
        Assert.Null(CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddMinutes(-30), Now.AddMinutes(30), withoutStartover, Now, Configuration));
    }
}
