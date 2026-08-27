using Jellyfin.Plugin.CoverArt.Configuration;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.CoverArt.Services;

/// <summary>
/// Selects the configured treatment for a Jellyfin item.
/// </summary>
public sealed class CoverStyleResolver
{
    /// <summary>
    /// Gets the configured style for an item.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="configuration">The active plugin configuration.</param>
    /// <returns>The selected cover style.</returns>
    public CoverStyle Resolve(BaseItem item, PluginConfiguration configuration)
    {
        var style = item.GetType().Name switch
        {
            "Movie" or "Video" => configuration.MovieStyle,
            "Series" => configuration.SeriesStyle,
            "Season" => configuration.SeasonStyle,
            "Episode" => configuration.EpisodeStyle,
            "MusicAlbum" or "Audio" => configuration.AlbumStyle,
            "BoxSet" or "CollectionFolder" => configuration.CollectionStyle,
            _ => CoverStyle.None
        };
        return style == CoverStyle.None ? CoverStyle.None : CoverStyle.ThickCase;
    }
}