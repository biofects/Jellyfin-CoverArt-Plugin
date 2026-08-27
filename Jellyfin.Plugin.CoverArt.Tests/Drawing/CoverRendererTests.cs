using Jellyfin.Plugin.CoverArt.Configuration;
using Jellyfin.Plugin.CoverArt.Drawing;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Plugin.CoverArt.Tests.Drawing;

public sealed class CoverRendererTests
{
    private readonly CoverRenderer _renderer = new();

    public static TheoryData<CoverStyle> RenderedStyles => new()
    {
        CoverStyle.Flat,
        CoverStyle.ThickCase,
        CoverStyle.MetroCase,
        CoverStyle.Television,
        CoverStyle.Disc
    };

    [Theory]
    [MemberData(nameof(RenderedStyles))]
    public void Render_Style_ReturnsDecodableImage(CoverStyle style)
    {
        var source = CreateImage(SKEncodedImageFormat.Png);

        var rendered = _renderer.Render(source, "image/png", style);

        Assert.NotNull(rendered);
        Assert.NotEqual(source, rendered);
        using var bitmap = SKBitmap.Decode(rendered);
        Assert.NotNull(bitmap);
        Assert.Equal(300, bitmap.Width);
        Assert.Equal(450, bitmap.Height);
    }

    [Fact]
    public void Render_ThickCaseFromJpeg_ReturnsPng()
    {
        var source = CreateImage(SKEncodedImageFormat.Jpeg);

        var rendered = _renderer.Render(source, "image/jpeg", CoverStyle.ThickCase);

        Assert.NotNull(rendered);
        Assert.Equal(0x89, rendered[0]);
        Assert.Equal(0x50, rendered[1]);
        Assert.Equal("image/png", CoverRenderer.GetOutputContentType("image/jpeg", CoverStyle.ThickCase));
    }

    [Fact]
    public void Render_None_ReturnsNull()
    {
        var source = CreateImage(SKEncodedImageFormat.Png);

        Assert.Null(_renderer.Render(source, "image/png", CoverStyle.None));
    }

    [Fact]
    public void Render_InvalidImage_ReturnsNull()
    {
        Assert.Null(_renderer.Render([1, 2, 3, 4], "image/png", CoverStyle.Flat));
    }

    [Theory]
    [InlineData(450, 300)]
    [InlineData(300, 300)]
    public void Render_NonPortraitImage_ReturnsNull(int width, int height)
    {
        var source = CreateImage(SKEncodedImageFormat.Png, width, height);

        Assert.Null(_renderer.Render(source, "image/png", CoverStyle.ThickCase, "1080p"));
    }

    [Theory]
    [InlineData("SD", 1)]
    [InlineData("480p", 1)]
    [InlineData("720p", 1)]
    [InlineData("1080p", 2)]
    [InlineData("1440p", 2)]
    [InlineData("4K", 3)]
    [InlineData("8K", 3)]
    public void ResolveDiscFormat_ReturnsExpectedFormat(string resolution, int expected)
    {
        Assert.Equal((DiscFormat)expected, CoverRenderer.ResolveDiscFormat(resolution));
    }

    [Fact]
    public void Render_ThickCase_UsesDistinctFormatTreatments()
    {
        var source = CreateImage(SKEncodedImageFormat.Png);

        var dvd = _renderer.Render(source, "image/png", CoverStyle.ThickCase, "480p");
        var bluRay = _renderer.Render(source, "image/png", CoverStyle.ThickCase, "1080p");
        var ultraHd = _renderer.Render(source, "image/png", CoverStyle.ThickCase, "4K");

        Assert.NotNull(dvd);
        Assert.NotNull(bluRay);
        Assert.NotNull(ultraHd);
        Assert.NotEqual(dvd, bluRay);
        Assert.NotEqual(dvd, ultraHd);
        Assert.NotEqual(bluRay, ultraHd);
    }

    [Fact]
    public void Render_CaseCover_UsesFormatMetadata()
    {
        var source = CreateImage(SKEncodedImageFormat.Png);

        var rendered = _renderer.Render(
            source,
            "image/png",
            CoverStyle.ThickCase,
            "1080p");

        Assert.NotNull(rendered);
        using var bitmap = SKBitmap.Decode(rendered);
        Assert.NotNull(bitmap);
        var rail = bitmap.GetPixel(20, 100);
        Assert.True(rail.Alpha > 200);
        Assert.True(rail.Blue > rail.Red * 5);
    }

    [Fact]
    public void Render_ThickCase_DrawsPhotographicOverlay()
    {
        var source = CreateImage(SKEncodedImageFormat.Png);

        var rendered = _renderer.Render(source, "image/png", CoverStyle.ThickCase, "1080p");

        Assert.NotNull(rendered);
        using var bitmap = SKBitmap.Decode(rendered);
        Assert.NotNull(bitmap);
        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        var leftRail = bitmap.GetPixel(20, bitmap.Height / 2);
        var rightRail = bitmap.GetPixel(bitmap.Width - 15, bitmap.Height / 2);
        Assert.True(leftRail.Alpha > 240);
        Assert.True(leftRail.Blue > leftRail.Red);
        Assert.True(rightRail.Alpha > 240);
        Assert.True(rightRail.Blue > rightRail.Red);

        var posterCenter = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.Equal(new SKColor(35, 90, 140), posterCenter);
    }

    private static byte[] CreateImage(SKEncodedImageFormat format, int width = 300, int height = 450)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(35, 90, 140));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 95);
        return data.ToArray();
    }
}