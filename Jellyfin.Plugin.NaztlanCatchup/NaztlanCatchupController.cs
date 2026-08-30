using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>REST API consumed by jellyfin-web-naztlan.</summary>
[ApiController]
[Authorize]
[Route("Naztlan/Catchup")]
public sealed class NaztlanCatchupController : ControllerBase
{
    private readonly CatchupMapStore _map;
    private readonly ILibraryManager _libraryManager;

    /// <summary>Initializes a new instance of the <see cref="NaztlanCatchupController"/> class.</summary>
    public NaztlanCatchupController(CatchupMapStore map, ILibraryManager libraryManager)
    {
        _map = map;
        _libraryManager = libraryManager;
    }

    /// <summary>Returns catchup capabilities keyed by the tuner channel id.</summary>
    [HttpGet("Map")]
    public ActionResult<object> GetMap()
    {
        var channels = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.LiveTvChannel],
                EnableTotalRecordCount = false,
            })
            .OfType<LiveTvChannel>()
            .Where(channel => !string.IsNullOrEmpty(channel.ExternalId) && _map.Channels.ContainsKey(channel.ExternalId))
            .ToDictionary(
                channel => channel.Id.ToString("N"),
                channel =>
                {
                    var capability = _map.Channels[channel.ExternalId];
                    return new CatchupCapability(capability.Days, capability.Startover, capability.Name);
                },
                StringComparer.OrdinalIgnoreCase);

        return Ok(new
        {
            Version = 1,
            Channels = channels,
        });
    }

    private sealed record CatchupCapability(int Days, bool Startover, string Name);
}
