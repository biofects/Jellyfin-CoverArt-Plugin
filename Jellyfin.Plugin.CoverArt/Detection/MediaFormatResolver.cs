using System;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.CoverArt.Detection;

/// <summary>Resolves the physical case format from Jellyfin media streams.</summary>
public sealed class MediaFormatResolver
{
    private readonly IMediaSourceManager _mediaSourceManager;

    /// <summary>Initializes a new instance of the <see cref="MediaFormatResolver"/> class.</summary>
    public MediaFormatResolver(IMediaSourceManager mediaSourceManager)
    {
        _mediaSourceManager = mediaSourceManager;
    }

    /// <summary>Gets the resolution used to select a DVD, Blu-ray, or UHD case.</summary>
    public string? Resolve(Guid itemId)
    {
        var streams = _mediaSourceManager.GetMediaStreams(itemId);
        if (streams is null || streams.Count == 0)
        {
            return null;
        }

        var video = streams.Where(stream => stream.Type == MediaStreamType.Video && !stream.IsExternal)
            .OrderByDescending(stream => (long)(stream.Width ?? 0) * (stream.Height ?? 0))
            .FirstOrDefault();
        return video is null ? null : FormatResolution(video.Width ?? 0, video.Height ?? 0);
    }

    internal static string? FormatResolution(int width, int height)
    {
        if (width <= 0 && height <= 0)
        {
            return null;
        }

        if (width >= 7000 || height >= 4000) return "8K";
        if (width >= 3500 || height >= 2000) return "4K";
        if (width >= 2400 || height >= 1300) return "1440p";
        if (width >= 1800 || height >= 1000) return "1080p";
        if (width >= 1200 || height >= 700) return "720p";
        if (width >= 700 || height >= 460) return "480p";
        return "SD";
    }

}