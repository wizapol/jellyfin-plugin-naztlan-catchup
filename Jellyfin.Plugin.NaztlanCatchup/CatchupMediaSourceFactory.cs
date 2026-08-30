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

        var beginUtc = startUtc.AddSeconds(-Math.Max(configuration.MarginBeforeSeconds, 0));
        var begin = new DateTimeOffset(beginUtc).ToUnixTimeSeconds();

        if (inProgress)
        {
            // Start-over "vivo" (M6): el CDN acepta begin sin end y sirve desde el inicio del
            // programa siguiendo al directo. Con IsInfiniteStream Jellyfin usa la ruta de directo
            // (playlist EVENT, -hls_list_size 0), asi que ffmpeg arranca en el inicio del programa y
            // la ventana crece hasta el vivo. El cliente web mapea el slider leyendo begin= del Path.
            var startoverPath = BuildStartoverPath(channel, beginUtc);
            if (startoverPath is null)
            {
                return null;
            }

            return new MediaSourceInfo
            {
                Id = $"naztlan-{itemId:N}-startover",
                Name = "Desde el inicio",
                Path = startoverPath,
                Protocol = MediaProtocol.Http,
                IsRemote = true,
                RunTimeTicks = null,
                IsInfiniteStream = true,
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

        var end = new DateTimeOffset(endUtc.Value.AddSeconds(Math.Max(configuration.MarginAfterSeconds, 0))).ToUnixTimeSeconds();
        var baseUrl = configuration.IptvBaseUrl.TrimEnd('/');
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"{baseUrl}/dispatcharr/catchup/{Uri.EscapeDataString(channel.LchId)}?begin={begin}&end={end}");
        var runtime = endUtc.Value - startUtc;

        return new MediaSourceInfo
        {
            Id = $"naztlan-{itemId:N}-catchup",
            Name = "Catchup",
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

    /// <summary>Formats an instant the way the Qwilt CDN expects (YYYYMMDDTHHMMSS, UTC).</summary>
    public static string FormatCdnTimestamp(DateTime utc)
        => utc.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Builds the direct CDN start-over URL (begin without end) from the map's catchup-source
    /// template (…/clear/index.m3u8?begin=${start}&amp;end=${end}). Returns null when the channel has
    /// no usable template.
    /// </summary>
    public static string? BuildStartoverPath(CatchupChannel channel, DateTime beginUtc)
    {
        var template = channel.CatchupSource;
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var query = template.IndexOf('?', StringComparison.Ordinal);
        var basePart = query >= 0 ? template[..query] : template;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{basePart}?begin={FormatCdnTimestamp(beginUtc)}");
    }
}
