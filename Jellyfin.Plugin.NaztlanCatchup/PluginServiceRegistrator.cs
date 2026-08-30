using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Registers plugin services with Jellyfin.</summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CatchupMapStore>();
        serviceCollection.AddSingleton<IMediaSourceProvider, NaztlanMediaSourceProvider>();
        serviceCollection.AddSingleton<IScheduledTask, ReloadCatchupMapTask>();
    }
}
