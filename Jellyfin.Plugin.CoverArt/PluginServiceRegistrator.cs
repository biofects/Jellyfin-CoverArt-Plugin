using Jellyfin.Plugin.CoverArt.Drawing;
using Jellyfin.Plugin.CoverArt.Caching;
using Jellyfin.Plugin.CoverArt.Detection;
using Jellyfin.Plugin.CoverArt.Services;
using Jellyfin.Plugin.CoverArt.Startup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CoverArt;

/// <summary>
/// Registers Cover Art services with Jellyfin.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CoverStyleResolver>();
        serviceCollection.AddSingleton<MediaFormatResolver>();
        serviceCollection.AddSingleton<CoverRenderer>();
        serviceCollection.AddSingleton<CoverArtCache>();
        serviceCollection.AddSingleton<IStartupFilter, CoverArtStartupFilter>();
    }
}