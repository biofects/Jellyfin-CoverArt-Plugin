using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.CoverArt.Caching;
using Jellyfin.Plugin.CoverArt.Configuration;
using Jellyfin.Plugin.CoverArt.Detection;
using Jellyfin.Plugin.CoverArt.Drawing;
using Jellyfin.Plugin.CoverArt.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.CoverArt.Middleware;

/// <summary>
/// Applies configured treatments to Jellyfin image responses.
/// </summary>
public sealed partial class CoverArtMiddleware
{
    private const long MaxImageBytes = 25 * 1024 * 1024;
    private readonly RequestDelegate _next;
    private readonly ILibraryManager _libraryManager;
    private readonly CoverStyleResolver _styleResolver;
    private readonly MediaFormatResolver _formatResolver;
    private readonly CoverRenderer _renderer;
    private readonly CoverArtCache _cache;
    private readonly ILogger<CoverArtMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverArtMiddleware"/> class.
    /// </summary>
    public CoverArtMiddleware(
        RequestDelegate next,
        ILibraryManager libraryManager,
        CoverStyleResolver styleResolver,
        MediaFormatResolver formatResolver,
        CoverRenderer renderer,
        CoverArtCache cache,
        ILogger<CoverArtMiddleware> logger)
    {
        _next = next;
        _libraryManager = libraryManager;
        _styleResolver = styleResolver;
        _formatResolver = formatResolver;
        _renderer = renderer;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Processes an HTTP request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryParseRequest(context, out var request)
            || Plugin.Instance?.Configuration is not PluginConfiguration configuration
            || !request.ImageType.Equals("Primary", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var item = _libraryManager.GetItemById(request.ItemId);
        if (item is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var style = _styleResolver.Resolve(item, configuration);
        if (style == CoverStyle.None)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        string? formatResolution;
        try
        {
            formatResolution = _formatResolver.Resolve(request.ItemId);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Cover Art could not resolve case format for {ItemId}", request.ItemId);
            formatResolution = null;
        }

        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;
        var cacheKey = _cache.BuildKey(request.ItemId, request.ImageType, request.ImageIndex, query, item.DateModified.ToUniversalTime().Ticks, style, formatResolution);
        var etag = $"\"ca-{cacheKey[..32]}\"";
        if (context.Request.Headers.IfNoneMatch.ToString().Contains(etag, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            SetCacheHeaders(context, etag);
            return;
        }

        var cached = _cache.Get(cacheKey);
        if (cached is not null)
        {
            await WriteImageAsync(context, cached.Value.Bytes, cached.Value.ContentType, etag).ConfigureAwait(false);
            return;
        }

        context.Request.Headers.Remove(HeaderNames.IfNoneMatch);
        context.Request.Headers.Remove(HeaderNames.IfModifiedSince);
        context.Request.Headers.Remove(HeaderNames.IfRange);
        context.Request.Headers.Remove(HeaderNames.Range);

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);
            context.Response.Body = originalBody;

            var contentType = context.Response.ContentType ?? string.Empty;
            if (context.Response.StatusCode != StatusCodes.Status200OK
                || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || buffer.Length == 0
                || buffer.Length > MaxImageBytes)
            {
                await FlushOriginalAsync(context, buffer, originalBody).ConfigureAwait(false);
                return;
            }

            var rendered = _renderer.Render(buffer.ToArray(), contentType, style, formatResolution);
            if (rendered is null)
            {
                await FlushOriginalAsync(context, buffer, originalBody).ConfigureAwait(false);
                return;
            }

            var outputContentType = CoverRenderer.GetOutputContentType(contentType, style);
            _cache.Set(cacheKey, rendered, outputContentType);
            context.Response.ContentType = outputContentType;
            context.Response.ContentLength = rendered.Length;
            SetCacheHeaders(context, etag);
            await originalBody.WriteAsync(rendered, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.Response.Body = originalBody;
            _logger.LogWarning(exception, "Cover Art could not process {Path}", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                await FlushOriginalAsync(context, buffer, originalBody).ConfigureAwait(false);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static async Task FlushOriginalAsync(HttpContext context, MemoryStream buffer, Stream originalBody)
    {
        buffer.Position = 0;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task WriteImageAsync(HttpContext context, byte[] bytes, string contentType, string etag)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.ContentLength = bytes.Length;
        SetCacheHeaders(context, etag);
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    private static void SetCacheHeaders(HttpContext context, string etag)
    {
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Remove(HeaderNames.LastModified);
    }

    private static bool TryParseRequest(HttpContext context, out ImageRequest request)
    {
        request = default;
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var match = ImagePathRegex().Match(context.Request.Path.Value ?? string.Empty);
        if (!match.Success || !Guid.TryParse(match.Groups["id"].Value, out var itemId))
        {
            return false;
        }

        var imageIndex = match.Groups["index"].Success ? match.Groups["index"].Value : null;
        request = new ImageRequest(itemId, match.Groups["type"].Value, imageIndex);
        return true;
    }

    [GeneratedRegex(@"^/Items/(?<id>[0-9a-fA-F-]{32,36})/Images/(?<type>[A-Za-z]+)(?:/(?<index>\d+))?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex ImagePathRegex();

    private readonly record struct ImageRequest(Guid ItemId, string ImageType, string? ImageIndex);
}