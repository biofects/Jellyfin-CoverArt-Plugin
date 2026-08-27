using Jellyfin.Plugin.CoverArt.Detection;
using Xunit;

namespace Jellyfin.Plugin.CoverArt.Tests.Detection;

public sealed class MediaFormatResolverTests
{
    [Theory]
    [InlineData(7680, 4320, "8K")]
    [InlineData(3840, 2160, "4K")]
    [InlineData(2560, 1440, "1440p")]
    [InlineData(1920, 1080, "1080p")]
    [InlineData(1280, 720, "720p")]
    [InlineData(720, 480, "480p")]
    [InlineData(640, 360, "SD")]
    [InlineData(0, 0, null)]
    public void FormatResolution_ReturnsExpectedLabel(int width, int height, string? expected)
    {
        Assert.Equal(expected, MediaFormatResolver.FormatResolution(width, height));
    }
}