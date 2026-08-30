using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Scheduled hourly map reload.</summary>
public sealed class ReloadCatchupMapTask : IScheduledTask
{
    private readonly CatchupMapStore _store;

    /// <summary>Initializes a new instance of the <see cref="ReloadCatchupMapTask"/> class.</summary>
    public ReloadCatchupMapTask(CatchupMapStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "Recargar mapa Naztlan Catchup";

    /// <inheritdoc />
    public string Key => "NaztlanCatchupReloadMap";

    /// <inheritdoc />
    public string Description => "Recarga catchup-map.json sin reiniciar Jellyfin.";

    /// <inheritdoc />
    public string Category => "Live TV";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Reload();
        progress.Report(100);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        => [new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(1).Ticks }];
}
