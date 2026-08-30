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
            // El ExternalId de un LiveTvChannel del tuner M3U es "m3u_<hash>", no el tvg-id; lo que
            // coincide con la clave del mapa (tvg-id de Dispatcharr = numero de canal) es Number,
            // que es tambien el prefijo del ExternalId de sus programas (2026-08-29).
            .Where(channel => !string.IsNullOrEmpty(channel.Number) && _map.Channels.ContainsKey(channel.Number))
            .ToDictionary(
                channel => channel.Id.ToString("N"),
                channel =>
                {
                    var capability = _map.Channels[channel.Number];
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
