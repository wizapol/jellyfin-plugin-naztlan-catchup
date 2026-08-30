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

    /// <summary>Gets or sets the margin before a programme, in seconds.</summary>
    public int MarginBeforeSeconds { get; set; } = 120;

    /// <summary>Gets or sets the margin after a programme, in seconds.</summary>
    public int MarginAfterSeconds { get; set; } = 120;
}
