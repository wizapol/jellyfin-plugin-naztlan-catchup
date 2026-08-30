using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Injects catchup sources into retained Live TV programmes.</summary>
public sealed class NaztlanMediaSourceProvider : IMediaSourceProvider
{
    private readonly CatchupMapStore _map;
    private readonly ILogger<NaztlanMediaSourceProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="NaztlanMediaSourceProvider"/> class.</summary>
    public NaztlanMediaSourceProvider(CatchupMapStore map, ILogger<NaztlanMediaSourceProvider> logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not LiveTvProgram program
            || !_map.TryGetByProgramExternalId(program.ExternalId, out var channel))
        {
            return Task.FromResult<IEnumerable<MediaSourceInfo>>([]);
        }

        var source = CatchupMediaSourceFactory.Create(
            program.Id,
            program.StartDate,
            program.EndDate,
            channel,
            DateTime.UtcNow,
            Plugin.GetConfiguration());
        if (source is null)
        {
            return Task.FromResult<IEnumerable<MediaSourceInfo>>([]);
        }

        _logger.LogInformation("Serving {Mode} source for programme {ProgramId} on IPTV channel {ChannelId}", source.Name, program.Id, channel.LchId);
        return Task.FromResult<IEnumerable<MediaSourceInfo>>([source]);
    }

    /// <inheritdoc />
    public Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        => throw new NotSupportedException("Naztlan catchup sources do not require opening");
}
