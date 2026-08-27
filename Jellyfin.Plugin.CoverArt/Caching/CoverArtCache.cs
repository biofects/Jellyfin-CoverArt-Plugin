using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Plugin.CoverArt.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CoverArt.Caching;

/// <summary>Stores rendered images in Jellyfin's cache directory.</summary>
public sealed class CoverArtCache
{
    private const string RenderSchemaVersion = "13";
    private readonly string _cacheRoot;
    private readonly ILogger<CoverArtCache> _logger;

    /// <summary>Initializes a new instance of the <see cref="CoverArtCache"/> class.</summary>
    public CoverArtCache(IApplicationPaths applicationPaths, ILogger<CoverArtCache> logger)
    {
        _logger = logger;
        _cacheRoot = Path.Combine(applicationPaths.CachePath, "coverart");
        Directory.CreateDirectory(_cacheRoot);
    }

    /// <summary>Builds a stable key from the source version and all visual settings.</summary>
    public string BuildKey(Guid itemId, string imageType, string? imageIndex, string? query, long versionTicks, CoverStyle style, string? formatResolution)
    {
        var value = new StringBuilder(RenderSchemaVersion)
            .Append('|').Append(itemId.ToString("N"))
            .Append('|').Append(imageType).Append('|').Append(imageIndex).Append('|').Append(query)
            .Append('|').Append(versionTicks.ToString(CultureInfo.InvariantCulture)).Append('|').Append(style)
            .Append('|').Append(formatResolution);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString())));
    }

    /// <summary>Reads a cached image.</summary>
    public CachedCover? Get(string key)
    {
        try
        {
            var path = FindPath(key);
            if (path is null) return null;
            return new CachedCover(File.ReadAllBytes(path), path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Cover Art cache read failed for {Key}", key);
            return null;
        }
    }

    /// <summary>Atomically stores a rendered image.</summary>
    public void Set(string key, byte[] bytes, string contentType)
    {
        try
        {
            var path = Path.Combine(_cacheRoot, key + (contentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg"));
            var temporaryPath = path + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, true);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Cover Art cache write failed for {Key}", key);
        }
    }

    private string? FindPath(string key)
    {
        var png = Path.Combine(_cacheRoot, key + ".png");
        if (File.Exists(png)) return png;
        var jpeg = Path.Combine(_cacheRoot, key + ".jpg");
        return File.Exists(jpeg) ? jpeg : null;
    }
}

/// <summary>A cached rendered cover.</summary>
/// <param name="Bytes">Encoded image data.</param>
/// <param name="ContentType">Image MIME type.</param>
public readonly record struct CachedCover(byte[] Bytes, string ContentType);