using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CoverArt.Configuration;

/// <summary>
/// Cover Art plugin settings.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        MovieStyle = CoverStyle.ThickCase;
        SeriesStyle = CoverStyle.ThickCase;
        SeasonStyle = CoverStyle.ThickCase;
        EpisodeStyle = CoverStyle.ThickCase;
        AlbumStyle = CoverStyle.ThickCase;
        CollectionStyle = CoverStyle.ThickCase;
    }

    /// <summary>Gets or sets the movie cover style.</summary>
    public CoverStyle MovieStyle { get; set; }

    /// <summary>Gets or sets the series cover style.</summary>
    public CoverStyle SeriesStyle { get; set; }

    /// <summary>Gets or sets the season cover style.</summary>
    public CoverStyle SeasonStyle { get; set; }

    /// <summary>Gets or sets the episode cover style.</summary>
    public CoverStyle EpisodeStyle { get; set; }

    /// <summary>Gets or sets the music album cover style.</summary>
    public CoverStyle AlbumStyle { get; set; }

    /// <summary>Gets or sets the collection cover style.</summary>
    public CoverStyle CollectionStyle { get; set; }

}