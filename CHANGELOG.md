# Changelog

All notable changes to Jellyfin Cover Art are documented here.

## [1.0.0] - 2026-08-27

### Added

- Photographic DVD, Blu-ray, and 4K UHD case overlays selected by resolution.
- Transparent case silhouettes with perspective-mapped portrait artwork.
- Per-media-type toggles for movies, series, seasons, episodes, albums, and collections.
- Image caching with HTTP ETag support.
- Prefiltered overlays for smoother rendering on small cards.

### Behavior

- Processes primary portrait artwork only.
- Leaves square and landscape artwork, thumbnails, and backdrops unchanged.
- Falls back to the original Jellyfin response when an image cannot be processed.

[1.0.0]: https://github.com/biofects/Jellyfin-CoverArt-Plugin/releases/tag/v1.0.0