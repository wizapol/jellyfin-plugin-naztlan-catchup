using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
                Id = DeriveId(itemId, "startover"),
                Name = "Desde el inicio",
                Path = startoverPath,
                Protocol = MediaProtocol.Http,
                IsRemote = true,
                // Duracion del programa: sin ella el cliente dibuja el OSD de un directo (sin
                // tiempos, slider clavado en 0 y sin forma de retroceder). Con ella el start-over
                // se maneja como el catchup, que es lo que es. IsInfiniteStream sigue en true
                // porque el transporte si es de directo (playlist EVENT que crece hasta el vivo),
                // y el retroceso dentro de lo ya emitido lo resuelve hls.js sobre esa playlist.
                RunTimeTicks = Math.Max((endUtc.Value - startUtc).Ticks, TimeSpan.TicksPerSecond),
                IsInfiniteStream = true,
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                SupportsProbing = true,
                Container = "hls",
                RequiresOpening = false,
                Type = MediaSourceType.Default,
                MediaStreams = PlaceholderStreams(configuration),
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
            Id = DeriveId(itemId, "catchup"),
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
            MediaStreams = PlaceholderStreams(configuration),
        };
    }

    /// <summary>
    /// Marcadores de pista que Jellyfin necesita para construir el comando de ffmpeg.
    /// Sin ellos <c>state.VideoStream</c>/<c>AudioStream</c> son null y EncodingHelper devuelve
    /// argumentos de video y audio VACIOS: ffmpeg sale sin -hwaccel, sin -codec:v, sin -b:v y sin
    /// mapeo, cae a libx264 por CPU con CRF por defecto y el cliente se queda en negro
    /// (medido el 2026-08-30; el directo funcionaba porque M3UTunerHost si declara estos dos).
    /// El indice es -1 porque no conocemos la posicion real dentro del contenedor, igual que hace
    /// M3UTunerHost.CreateMediaSourceInfo. IsInterlaced se deja en false a proposito: el material
    /// de Totalplay es 1080p progresivo y marcarlo entrelazado anadiria un yadif innecesario.
    /// </summary>
    private static MediaStream[] PlaceholderStreams(Configuration.PluginConfiguration configuration) =>
    [
        new MediaStream
        {
            Type = MediaStreamType.Video,
            Index = -1,
            IsInterlaced = false,
            // El codec se deja sin declarar (como M3UTunerHost) para que Jellyfin transcodifique
            // en vez de intentar un copy sobre una fuente cuyo codec real no hemos sondeado.
            // Ancho, alto y bitrate SI se declaran: Jellyfin acota el bitrate de salida al de
            // origen, y sin ese tope pide -b:v 1073741823 y la reproduccion se atasca.
            Width = configuration.SourceWidth,
            Height = configuration.SourceHeight,
            BitRate = configuration.SourceVideoBitrate,
        },
        new MediaStream
        {
            Type = MediaStreamType.Audio,
            Index = -1,
            Channels = 2,
            BitRate = 192_000,
        },
    ];

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

    /// <summary>
    /// Deterministic GUID per (programme, mode). DynamicHlsHelper parses MediaSourceId with Guid.Parse
    /// (500 with any other format, seen 2026-08-29), so the id cannot carry a readable suffix; the
    /// start-over mode is recognised by the Path (begin= without end=) instead.
    /// </summary>
    public static string DeriveId(Guid itemId, string mode)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"naztlan-{itemId:N}-{mode}"));
        return new Guid(hash).ToString("N");
    }

    /// <summary>True when the path is a live start-over playlist (begin without end).</summary>
    public static bool IsStartoverPath(string? path)
        => !string.IsNullOrEmpty(path)
           && path.Contains("begin=", StringComparison.Ordinal)
           && !path.Contains("end=", StringComparison.Ordinal);
}
