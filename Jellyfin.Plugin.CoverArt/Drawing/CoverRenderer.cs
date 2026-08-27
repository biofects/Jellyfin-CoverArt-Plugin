using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.CoverArt.Configuration;
using SkiaSharp;

namespace Jellyfin.Plugin.CoverArt.Drawing;

/// <summary>
/// Draws cover treatments using embedded case overlays and original vector geometry.
/// </summary>
public sealed class CoverRenderer
{
    /// <summary>
    /// Applies a cover treatment to an encoded image.
    /// </summary>
    /// <param name="source">The original encoded image.</param>
    /// <param name="contentType">The original response content type.</param>
    /// <param name="style">The treatment to apply.</param>
    /// <param name="formatResolution">The media resolution used to choose the physical case format.</param>
    /// <returns>The rendered image, or <see langword="null"/> if it cannot be rendered.</returns>
    public byte[]? Render(byte[] source, string contentType, CoverStyle style, string? formatResolution = null)
    {
        if (style == CoverStyle.None)
        {
            return null;
        }

        using var sourceBitmap = Decode(source);
        if (sourceBitmap is null
            || sourceBitmap.Width < 32
            || sourceBitmap.Height < 32
            || sourceBitmap.Width >= sourceBitmap.Height)
        {
            return null;
        }

        using var bitmap = new SKBitmap(sourceBitmap.Width, sourceBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        DrawCaseCover(canvas, sourceBitmap, ResolveDiscFormat(formatResolution));

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(GetFormat(contentType, style), 92);
        return encoded?.ToArray();
    }

    /// <summary>Gets the MIME type produced for a rendered style.</summary>
    public static string GetOutputContentType(string sourceContentType, CoverStyle style)
    {
        return style != CoverStyle.None || sourceContentType.Contains("png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";
    }

    private static void DrawCaseCover(
        SKCanvas canvas,
        SKBitmap source,
        DiscFormat format)
    {
        canvas.Clear(SKColors.Transparent);
        using var overlay = LoadCaseOverlay(format, source.Width <= 400);
        if (overlay is null)
        {
            canvas.DrawBitmap(source, 0, 0);
            return;
        }

        var aperture = GetCaseAperture(format, source.Width, source.Height);
        using var aperturePath = CreateQuadPath(aperture);
    #pragma warning disable CS0618 // Bitmap overloads in the pinned SkiaSharp version still use FilterQuality.
        using var posterPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
    #pragma warning restore CS0618
        canvas.Save();
        canvas.ClipPath(aperturePath, SKClipOperation.Intersect, true);
        canvas.SetMatrix(CreatePosterMatrix(source.Width, source.Height, aperture));
        canvas.DrawBitmap(source, 0, 0, posterPaint);
        canvas.Restore();

        var destination = new SKRect(0, 0, source.Width, source.Height);
        canvas.DrawBitmap(overlay, destination, posterPaint);
    }

    private static SKBitmap? LoadCaseOverlay(DiscFormat format, bool useSmallOverlay)
    {
        var name = format switch
        {
            DiscFormat.BluRay => "bluray",
            DiscFormat.UltraHd => "uhd",
            _ => "dvd"
        };
        var sizePath = useSmallOverlay ? "Small." : string.Empty;
        using var stream = typeof(CoverRenderer).Assembly.GetManifestResourceStream(
            $"Jellyfin.Plugin.CoverArt.Assets.Cases.{sizePath}{name}.png");
        return stream is null ? null : SKBitmap.Decode(stream);
    }

    private static SKPoint[] GetCaseAperture(DiscFormat format, int width, int height)
    {
        var points = format == DiscFormat.Dvd
            ? new[]
            {
                new SKPoint(0.125F, 0.130F),
                new SKPoint(0.910F, 0.185F),
                new SKPoint(0.910F, 0.895F),
                new SKPoint(0.125F, 0.950F)
            }
            : new[]
            {
                new SKPoint(0.116F, 0.116F),
                new SKPoint(0.900F, 0.184F),
                new SKPoint(0.900F, 0.876F),
                new SKPoint(0.116F, 0.942F)
            };
        return points.Select(point => new SKPoint(point.X * width, point.Y * height)).ToArray();
    }

    private static SKPath CreateQuadPath(IReadOnlyList<SKPoint> points)
    {
        var path = new SKPath();
        path.MoveTo(points[0]);
        path.LineTo(points[1]);
        path.LineTo(points[2]);
        path.LineTo(points[3]);
        path.Close();
        return path;
    }

    private static SKMatrix CreatePosterMatrix(float sourceWidth, float sourceHeight, IReadOnlyList<SKPoint> points)
    {
        var topLeft = points[0];
        var topRight = points[1];
        var bottomRight = points[2];
        var bottomLeft = points[3];
        var deltaX1 = topRight.X - bottomRight.X;
        var deltaX2 = bottomLeft.X - bottomRight.X;
        var deltaX3 = topLeft.X - topRight.X + bottomRight.X - bottomLeft.X;
        var deltaY1 = topRight.Y - bottomRight.Y;
        var deltaY2 = bottomLeft.Y - bottomRight.Y;
        var deltaY3 = topLeft.Y - topRight.Y + bottomRight.Y - bottomLeft.Y;
        var denominator = (deltaX1 * deltaY2) - (deltaX2 * deltaY1);
        var perspectiveX = ((deltaX3 * deltaY2) - (deltaX2 * deltaY3)) / denominator;
        var perspectiveY = ((deltaX1 * deltaY3) - (deltaX3 * deltaY1)) / denominator;

        return new SKMatrix
        {
            ScaleX = (topRight.X - topLeft.X + (perspectiveX * topRight.X)) / sourceWidth,
            SkewX = (bottomLeft.X - topLeft.X + (perspectiveY * bottomLeft.X)) / sourceHeight,
            TransX = topLeft.X,
            SkewY = (topRight.Y - topLeft.Y + (perspectiveX * topRight.Y)) / sourceWidth,
            ScaleY = (bottomLeft.Y - topLeft.Y + (perspectiveY * bottomLeft.Y)) / sourceHeight,
            TransY = topLeft.Y,
            Persp0 = perspectiveX / sourceWidth,
            Persp1 = perspectiveY / sourceHeight,
            Persp2 = 1F
        };
    }

    internal static DiscFormat ResolveDiscFormat(string? resolution)
    {
        return resolution?.ToUpperInvariant() switch
        {
            "4K" or "8K" => DiscFormat.UltraHd,
            "1080P" or "1440P" => DiscFormat.BluRay,
            "SD" or "480P" or "720P" => DiscFormat.Dvd,
            _ => DiscFormat.Unknown
        };
    }

    private static SKEncodedImageFormat GetFormat(string contentType, CoverStyle style)
    {
        return GetOutputContentType(contentType, style) == "image/png"
            ? SKEncodedImageFormat.Png
            : SKEncodedImageFormat.Jpeg;
    }

    private static SKBitmap? Decode(byte[] source)
    {
        try
        {
            return SKBitmap.Decode(source);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>The physical media format represented by a case cover.</summary>
internal enum DiscFormat
{
    Unknown,
    Dvd,
    BluRay,
    UltraHd
}