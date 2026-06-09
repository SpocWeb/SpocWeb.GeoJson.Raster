---
digest:
  local-classes:
    Program:
      mtime: "2026-06-09T16:04:44Z"
      digest: "e1623107bf1d964a526b588adc259035fbc65d746201544be5e5926fddd0dbb9"
  folders: {}
---
# SpocWeb.GeoJson.Raster

<!-- digest-map
local-classes:
  GDalContext: mtime=2026-04-20T16:33:28Z digest=dc2c111eaa794b356a1cad08948bb30fed2f2e2f0c90e9375caab34f268588a0
  Epsg: mtime=2026-04-20T16:33:28Z digest=dc2c111eaa794b356a1cad08948bb30fed2f2e2f0c90e9375caab34f268588a0
  GeoJsonAddElevation: mtime=2026-05-16T07:12:22Z digest=0f6a243ef484d835e4f85a9818515c4f8d14061e91e82aff94546b2b55fa4684
  GeoJsonHistogramEnricher: mtime=2026-05-04T19:51:06Z digest=ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
  GeometryZ: mtime=2026-04-18T14:41:04Z digest=748286817e01994ab7231a86addf5d776442f03c95ca4f8ec034cacb079f929d
  HistogramBin: mtime=2026-05-04T19:51:06Z digest=ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
  HistogramSchema: mtime=2026-05-04T19:51:06Z digest=ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
  HistogramSchemaFactory: mtime=2026-05-04T19:51:06Z digest=ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
  Program: mtime=2026-04-18T09:57:08Z digest=e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
  StreamingGeoJsonProcessor: mtime=2026-05-16T07:12:26Z digest=5e2569cea31bf06736224b64215e0f8bf743332cf15f2b3f10d73b9b212de291
folders:
folder_digest: 66464345559a961a566de61cf8e7865e22d4f8741a223ac279e7e3908a74048d
folder_mtime: 2026-05-16T07:12:26Z
-->

Adds elevation information from Digital Elevation Models (DEM) to GeoJSON files,
and computes per-feature elevation histograms using GDAL raster datasets
such as the Copernicus DEM VRT.

## Architecture

```mermaid
flowchart TD
    subgraph Entry
        Program["Program\n(entry-point placeholder)"]
    end

    subgraph raster["raster/ subsystem"]
        GDal["GDalContext\n(GDAL dataset + band + transform)"]
        Epsg["Epsg\n(EPSG CRS codes)"]
        AddElev["GeoJsonAddElevation\n(batch Z enrichment)"]
        Stream["StreamingGeoJsonProcessor\n(token-by-token Z enrichment)"]
        GeomZ["GeometryZ\n(geometry extension methods)"]
        Enrich["GeoJsonHistogramEnricher\n(per-feature histogram)"]
        Schema["HistogramSchema\n(bin definitions)"]
        Factory["HistogramSchemaFactory\n(schema builder)"]
        Bin["HistogramBin\n(single bin definition)"]
    end

    AddElev -->|"samples via"| GDal
    Stream -->|"samples via"| GDal
    GeomZ -->|"samples via"| GDal
    Enrich -->|"samples via"| GDal
    Enrich -->|"uses schema"| Schema

    linkStyle 4 opacity:1

    Factory -->|"creates"| Schema
    Schema -->|"contains"| Bin
    GDal -->|"uses codes from"| Epsg
```

## Classes

| Class | Responsibility |
|---|---|
| [Program](Program.cs) | Entry point placeholder for the SpocWeb. |

## Relationships

```mermaid
flowchart TD
  GDalContext["GDalContext"]
  GeoJsonAddElevation["GeoJsonAddElevation"]
  StreamingGeoJsonProcessor["StreamingGeoJsonProcessor"]
  GeometryZ["GeometryZ (extensions)"]
  GeoJsonHistogramEnricher["GeoJsonHistogramEnricher"]
  HistogramSchema["HistogramSchema"]
  HistogramSchemaFactory["HistogramSchemaFactory"]

  GeoJsonAddElevation -->|"uses"| GDalContext
  linkStyle 0 opacity:1

  StreamingGeoJsonProcessor -->|"uses"| GDalContext
  linkStyle 1 opacity:1

  GeometryZ -->|"uses"| GDalContext
  linkStyle 2 opacity:1

  GeoJsonHistogramEnricher -->|"uses"| GDalContext
  linkStyle 3 opacity:1

  GeoJsonHistogramEnricher -->|"uses"| HistogramSchema
  linkStyle 4 opacity:1

  HistogramSchemaFactory -->|"creates"| HistogramSchema
  linkStyle 5 opacity:1
```

## Entry Points

| Method | Description |
|---|---|
| `GeoJsonAddElevation.AddElevationAsZ(vrtFile, geoJsonDirectory)` | Adds Z coordinates to all GeoJSON files in a directory tree, updating each file in-place. |
| `StreamingGeoJsonProcessor.StreamGeoJsonProcessor(vrtPath, geoJsonPath)` | Streams elevation-enriched GeoJSON output for a single file without loading it fully. |
| `GeoJsonHistogramEnricher.AddHistogram(vrtFile, geoJsonDirectory)` | Adds compact elevation histograms to all GeoJSON polygon features in a directory tree in parallel. |
| `HistogramSchemaFactory.CreateFromRange(id, min, max, bucketCount)` | Creates a histogram schema from explicit min/max bounds and bucket count. |
| `HistogramSchemaFactory.CreateFromWidth(id, min, bucketCount, width)` | Creates a histogram schema from a starting value and fixed bucket width. |

## Quick Start

```csharp
// 1. Create a histogram schema (elevation −100 m to +9 000 m, 360 bins of 25 m each).
var schema = HistogramSchemaFactory.CreateFromWidth(
    "Elevation0-9000", -100, 360, 25);

// 2. Add Z elevation to all GeoJSON files in a directory tree.
GeoJsonAddElevation.AddElevationAsZ(
    @"D:\Copernicus_DSM\global_dem.vrt",
    @"D:\GeoData\Continent");

// 3. Add per-feature elevation histograms in parallel.
GeoJsonHistogramEnricher.AddHistogram(
    @"D:\Copernicus_DSM\global_dem.vrt",
    @"D:\GeoData\Continent");
```

## Key Concepts

### Elevation Z enrichment
[GeoJsonAddElevation](raster/GeoJsonAddElevation.cs) iterates every GeoJSON file in a directory,
reads the geometry, samples the raster at each coordinate via
[GDalContext.Sample](raster/GDalContext.cs),
and appends a Z value rounded to four decimal places.
[StreamingGeoJsonProcessor](raster/StreamingGeoJsonProcessor.cs) does the same
token-by-token to keep memory use constant regardless of file size.

### Raster sampling
[GDalContext](raster/GDalContext.cs) encapsulates one GDAL `Dataset` and `Band`,
together with the affine geo-transform matrix and an OSR coordinate transformation.
Each worker thread holds its own `GDalContext` (thread-safe, lock-free reads after open).
The static `GdalLock` serializes the non-thread-safe `Gdal.Open` call.

### Histogram computation
[GeoJsonHistogramEnricher](raster/HistogramSchema.cs) rasterizes each polygon's
bounding box, applies point-in-polygon tests via
`IndexedPointInAreaLocator`, and accumulates raster pixel counts or
spherical-area sums into a `long[]` or `double[]` array indexed by bin.
The schema ([HistogramSchema](raster/HistogramSchema.cs)) carries the bin definitions
and is shared across all parallel workers.

### Coordinate extension
[GeometryZ](raster/GeometryZ.cs) provides extension methods over the full
`NetTopologySuite` geometry hierarchy (`Point`, `LineString`, `Polygon`,
`Multi*`, `GeometryCollection`), dispatching via `switch` expression.

## Further Reading

- [GDAL VRT Format](https://gdal.org/drivers/raster/vrt.html) — virtual raster tiles used as the DEM source.
- [Copernicus DEM](https://spacedata.copernicus.eu/collections/copernicus-digital-elevation-model) — elevation model used in the test cases.
- [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) — .NET geometry library used for all spatial operations.
- [OSGeo.GDAL NuGet](https://www.nuget.org/packages/GDAL) — managed bindings to GDAL/OGR/OSR.

## Subsystems

| Folder | Domain Role |
|---|---|
| [`raster/`](raster/ReadMe.md) | GDAL-backed raster processing classes for elevation enrichment and histogram computation. |
