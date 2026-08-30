using Jellyfin.Plugin.NaztlanCatchup.Configuration;
using Xunit;

namespace Jellyfin.Plugin.NaztlanCatchup.Tests;

public sealed class CatchupMediaSourceFactoryTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc);
    private static readonly CatchupChannel Channel = new("2169", 7, true, "Azteca uno", "tp112169", "http://cdn/bpk-tv/tp112169/clear/index.m3u8?begin=${start}&end=${end}");
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
        Assert.True(Guid.TryParse(source.Id, out _));
        Assert.False(CatchupMediaSourceFactory.IsStartoverPath(source.Path));
        Assert.False(source.IsInfiniteStream);
        Assert.Equal(TimeSpan.FromHours(1).Ticks, source.RunTimeTicks);
        Assert.Equal("http://iptv_tp:8801/dispatcharr/catchup/2169?begin=1788044280&end=1788048120", source.Path);
    }

    [Fact]
    public void AiringProgrammeBuildsLiveStartOverFromCdn()
    {
        var source = CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddMinutes(-30), Now.AddMinutes(30), Channel, Now, Configuration);

        Assert.NotNull(source);
        Assert.Equal("Desde el inicio", source.Name);
        Assert.True(source.IsInfiniteStream);
        Assert.True(Guid.TryParse(source.Id, out _));
        Assert.True(CatchupMediaSourceFactory.IsStartoverPath(source.Path));
        Assert.Null(source.RunTimeTicks);
        // inicio 00:30 UTC menos 120 s de margen, begin sin end (el CDN sigue al vivo)
        Assert.Equal("http://cdn/bpk-tv/tp112169/clear/index.m3u8?begin=20260830T002800", source.Path);
    }

    [Fact]
    public void AiringProgrammeWithoutTemplateHasNoSource()
    {
        var noTemplate = Channel with { CatchupSource = string.Empty };
        Assert.Null(CatchupMediaSourceFactory.Create(Guid.NewGuid(), Now.AddMinutes(-30), Now.AddMinutes(30), noTemplate, Now, Configuration));
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
