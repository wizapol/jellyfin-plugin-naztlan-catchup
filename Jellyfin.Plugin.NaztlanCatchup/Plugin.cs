using System.Globalization;
using Jellyfin.Plugin.NaztlanCatchup.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Naztlan IPTV catchup and start-over plugin.</summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="Plugin"/> class.</summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Naztlan Catchup";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("9f9a5c5e-6d36-4cb0-9e0b-764c5dd58441");

    /// <summary>Gets the active plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>Gets a stable configuration snapshot.</summary>
    public static PluginConfiguration GetConfiguration()
        => Instance?.Configuration ?? new PluginConfiguration();
}
