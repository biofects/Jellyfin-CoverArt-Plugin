namespace Jellyfin.Plugin.CoverArt.Configuration;

/// <summary>
/// Available original cover treatments.
/// </summary>
public enum CoverStyle
{
    /// <summary>No additional frame.</summary>
    None,

    /// <summary>A compact flat border.</summary>
    Flat,

    /// <summary>A deep media case with a visible spine.</summary>
    ThickCase,

    /// <summary>A clean modern case.</summary>
    MetroCase,

    /// <summary>A television display frame.</summary>
    Television,

    /// <summary>A compact disc treatment.</summary>
    Disc
}