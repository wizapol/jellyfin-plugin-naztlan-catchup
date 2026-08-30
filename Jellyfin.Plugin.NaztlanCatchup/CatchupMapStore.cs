using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NaztlanCatchup;

/// <summary>Thread-safe in-memory representation of catchup-map.json.</summary>
public sealed class CatchupMapStore
{
    private readonly ILogger<CatchupMapStore> _logger;
    private ImmutableDictionary<string, CatchupChannel> _channels = ImmutableDictionary<string, CatchupChannel>.Empty;

    /// <summary>Initializes a new instance of the <see cref="CatchupMapStore"/> class.</summary>
    public CatchupMapStore(ILogger<CatchupMapStore> logger)
    {
        _logger = logger;
        Reload();
    }

    /// <summary>Gets the currently loaded channels.</summary>
    public IReadOnlyDictionary<string, CatchupChannel> Channels => _channels;

    /// <summary>Reloads and validates the configured map atomically.</summary>
    public void Reload()
    {
        var path = Plugin.GetConfiguration().MapPath;
        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<CatchupMapDocument>(stream, JsonOptions)
                ?? throw new InvalidDataException("Empty catchup map");
            if (document.Version != 1 || document.Channels.Count == 0)
            {
                throw new InvalidDataException($"Unsupported or empty catchup map version {document.Version}");
            }

            _channels = document.Channels.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("Loaded {Count} Naztlan catchup channels from {Path}", _channels.Count, path);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not reload Naztlan catchup map {Path}; keeping {Count} channels", path, _channels.Count);
        }
    }

    /// <summary>Looks up the map entry associated with a Jellyfin programme.</summary>
    public bool TryGetByProgramExternalId(string? externalId, out CatchupChannel channel)
    {
        var separator = externalId?.IndexOf('_', StringComparison.Ordinal) ?? -1;
        var key = separator > 0 ? externalId![..separator] : externalId;
        return _channels.TryGetValue(key ?? string.Empty, out channel!);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}

/// <summary>Root map document.</summary>
public sealed record CatchupMapDocument(int Version, Dictionary<string, CatchupChannel> Channels);

/// <summary>Catchup capabilities for one tuner channel.</summary>
public sealed record CatchupChannel(
    string LchId,
    int Days,
    bool Startover,
    string Name,
    string TpCode,
    string CatchupSource);
