using System;
using Jellyfin.Plugin.CoverArt.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.CoverArt.Startup;

/// <summary>
/// Adds Cover Art to Jellyfin's HTTP pipeline.
/// </summary>
public sealed class CoverArtStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<CoverArtMiddleware>();
            next(builder);
        };
    }
}