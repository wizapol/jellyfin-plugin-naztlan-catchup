using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NaztlanCatchup.Configuration;

/// <summary>Configuration for the Naztlan catchup plugin.</summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the channel map path visible inside Jellyfin.</summary>
    public string MapPath { get; set; } = "/media/IPTV/catchup-map.json";

    /// <summary>Gets or sets the iptv_tp URL visible inside Jellyfin.</summary>
    public string IptvBaseUrl { get; set; } = "http://192.168.18.2:8801";

    /// <summary>Gets or sets the maximum catchup age.</summary>
    public int MaximumDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the nominal source bitrate, in bits per second, declared on the placeholder
    /// video stream. Jellyfin caps the encoder bitrate at the source's (EncodingHelper
    /// .GetVideoBitrateParamValue); without it a LAN client that measures gigabit throughput asks
    /// for "no limit" and ffmpeg is launched with -b:v 1073741823, whose segments no client can
    /// pull in time (measured 2026-08-30: playback stalled a few seconds in).
    /// Totalplay's CDN variants top out around 5.4 Mbps; 8 Mbps leaves headroom without lying.
    /// </summary>
    public int SourceVideoBitrate { get; set; } = 8_000_000;

    /// <summary>Gets or sets the nominal source width declared on the placeholder video stream.</summary>
    public int SourceWidth { get; set; } = 1920;

    /// <summary>Gets or sets the nominal source height declared on the placeholder video stream.</summary>
    public int SourceHeight { get; set; } = 1080;

    /// <summary>Gets or sets the margin before a programme, in seconds.</summary>
    public int MarginBeforeSeconds { get; set; } = 120;

    /// <summary>Gets or sets the margin after a programme, in seconds.</summary>
    public int MarginAfterSeconds { get; set; } = 120;
}
