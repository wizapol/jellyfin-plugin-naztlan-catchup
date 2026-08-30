using System.Globalization;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Builds deterministic HLS media sources for catchup and start-over.</summary>
public static class CatchupMediaSourceFactory
{
    /// <summary>Builds the source for a programme at a given instant.</summary>
    public static MediaSourceInfo? Create(
        Guid itemId,
        DateTime startUtc,
        DateTime? endUtc,
        CatchupChannel channel,
        DateTime nowUtc,
        Configuration.PluginConfiguration configuration)
    {
        if (startUtc > nowUtc || endUtc is null)
        {
            return null;
        }

        var availableDays = Math.Min(Math.Max(channel.Days, 0), Math.Clamp(configuration.MaximumDays, 0, 30));
        if (availableDays == 0 || startUtc < nowUtc.AddDays(-availableDays))
        {
            return null;
        }

        var inProgress = endUtc.Value > nowUtc;
        if (inProgress && !channel.Startover)
        {
            return null;
        }

        var begin = new DateTimeOffset(startUtc.AddSeconds(-Math.Max(configuration.MarginBeforeSeconds, 0))).ToUnixTimeSeconds();
        var effectiveEnd = inProgress ? nowUtc : endUtc.Value;
        var end = new DateTimeOffset(effectiveEnd.AddSeconds(Math.Max(configuration.MarginAfterSeconds, 0))).ToUnixTimeSeconds();
        var baseUrl = configuration.IptvBaseUrl.TrimEnd('/');
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"{baseUrl}/dispatcharr/catchup/{Uri.EscapeDataString(channel.LchId)}?begin={begin}&end={end}");
        var runtime = effectiveEnd - startUtc;

        return new MediaSourceInfo
        {
            Id = $"naztlan-{itemId:N}-{(inProgress ? "startover" : "catchup")}",
            Name = inProgress ? "Desde el inicio" : "Catchup",
            Path = path,
            Protocol = MediaProtocol.Http,
            IsRemote = true,
            RunTimeTicks = Math.Max(runtime.Ticks, TimeSpan.TicksPerSecond),
            IsInfiniteStream = false,
            SupportsDirectPlay = false,
            SupportsDirectStream = true,
            SupportsTranscoding = true,
            SupportsProbing = true,
            Container = "hls",
            RequiresOpening = false,
            Type = MediaSourceType.Default,
            MediaStreams = [],
        };
    }
}
