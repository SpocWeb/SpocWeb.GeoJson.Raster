---
facet-complexity: 3
facet-status: active
facet-layer: domain
concepts:
  - Mathematics\Statistics.md
  - digital_elevation_model
  - gdal_raster_processing
tags:
  - code/elevation_enrichment
  - code/histogram_computation
  - code/raster_processing
description: "GDAL-backed raster processing classes for elevation enrichment and histogram computation. All classes reside in the `org.SpocWeb.root.files.Tests.raster` namespace."
digest:
  local-classes:
    Epsg:
      mtime: "2026-08-03T19:24:45Z"
      digest: "f992d79265e14a0b9021c0fb424ed6a92b8066cf71699a06012cf37d57fea239"
    GDalContext:
      mtime: "2026-08-03T19:24:45Z"
      digest: "b148f6976d8f56fe47232dc6cb3bba8c17bc765f7de78ceabb70fe784d1b51af"
    GeoJsonAddElevation:
      mtime: "2026-08-03T19:24:45Z"
      digest: "4bb7bc989c0272f496c8ede21b555be18b4ddb57e4218598fd61080ae93a1558"
    GeoJsonHistogramEnricher:
      mtime: "2026-08-16T22:12:17Z"
      digest: "4ca284e0bb778ec17b33ab031b3d877c911a0ced1f6674ebda2f1ce23649c803"
    GeometryZ:
      mtime: "2026-08-03T19:24:45Z"
      digest: "614d471a784aba104c9923263832b3f0d34646572a31bf8e501e2fbf68cbb08d"
    HistogramBin:
      mtime: "2026-08-16T22:12:17Z"
      digest: "9bbda65e43164d84c53064634085f069774ae6079d0a0eec2db7b8eaab158e2a"
    HistogramSchema:
      mtime: "2026-08-16T22:12:17Z"
      digest: "b6829b271168e12fd0d71b100c611f873457f3bfa05b179b170d282a06153f1c"
    HistogramSchemaFactory:
      mtime: "2026-08-16T22:12:17Z"
      digest: "bfe761ccdca398e6e43f2ecbdab9e10dd8f42136b479b23a086aaf33adcacfc7"
    StreamingGeoJsonProcessor:
      mtime: "2026-08-03T19:24:45Z"
      digest: "ec6b0e56b414e0d88def3631b0599d4b8e5926f7f43037b56bc47d561860ce7e"
  folders: {}
related:
  - path: ../_Matthias/Code/NET/_SpocWeb.Root/_std/SpocWeb.GeoJson.Raster
    shared-tags: [code/elevation_enrichment, code/raster_processing]
dv_has_:
  sub_:
    folders: 0
    files: 5
    units: 9
    facet_:
      layer_:
        domain: 7
        infrastructure: 2
      status_:
        active: 9
      complexity_:
        "1": 3
        "2": 3
        "3": 1
        "4": 2
    tag_:
      code_:
        elevation_enrichment: 3
        histogram_computation: 2
        geojson_serialization: 1
        raster_processing: 1
        data_model: 2
        context_management: 1
        geometry_transformation: 1
        coordinate_transformation: 1
        streaming_parser: 1
        parallel_processing: 1
    concept_:
      "Earth\\Geography\\GIS(Geographic_Information_System).md": 3
      digital_elevation_model: 2
      gdal_raster_processing: 2
      "Mathematics\\Geometry\\Coordinate_System.md": 2
      "Mathematics\\Statistics.md": 4
      geometry_processing: 1
      streaming_json_processing: 1
has_sub_folders: 0
has_sub_files: 5
has_sub_units: 9
has_sub_facet_layer_domain: 7
has_sub_facet_layer_infrastructure: 2
has_sub_facet_status_active: 9
has_sub_facet_complexity_1: 3
has_sub_facet_complexity_2: 3
has_sub_facet_complexity_3: 1
has_sub_facet_complexity_4: 2
has_sub_tag_code_elevation_enrichment: 3
has_sub_tag_code_histogram_computation: 2
has_sub_tag_code_geojson_serialization: 1
has_sub_tag_code_raster_processing: 1
has_sub_tag_code_data_model: 2
has_sub_tag_code_context_management: 1
has_sub_tag_code_geometry_transformation: 1
has_sub_tag_code_coordinate_transformation: 1
has_sub_tag_code_streaming_parser: 1
has_sub_tag_code_parallel_processing: 1
has_sub_concept_earth_geography_gis_geographic_information_system_md: 3
has_sub_concept_digital_elevation_model: 2
has_sub_concept_gdal_raster_processing: 2
has_sub_concept_mathematics_geometry_coordinate_system_md: 2
has_sub_concept_mathematics_statistics_md: 4
has_sub_concept_geometry_processing: 1
has_sub_concept_streaming_json_processing: 1
---
# raster

GDAL-backed raster processing classes for elevation enrichment and histogram computation.
All classes reside in the `org.SpocWeb.root.files.Tests.raster` namespace.

## Architecture

```mermaid
flowchart TD
    subgraph Context
        GDal["GDalContext
    (worker-local GDAL + OSR transform)"]
        Epsg["Epsg
    (EPSG CRS constants)"]
    end

    subgraph Elevation
        AddElev["GeoJsonAddElevation
    (batch file Z enrichment)"]
        Stream["StreamingGeoJsonProcessor
    (token-by-token Z enrichment)"]
        GeomZ["GeometryZ
    (NTS geometry extension methods)"]
    end

    subgraph Histogram
        Factory["HistogramSchemaFactory
    (creates HistogramSchema)"]
        Schema["HistogramSchema
    (shared bin definitions)"]
        Bin["HistogramBin
    (single bin)"]
        Enrich["GeoJsonHistogramEnricher
    (per-feature histogram enrichment)"]
    end

    GDal -->|"uses codes from"| Epsg
    AddElev -->|"samples via"| GDal
    Stream -->|"samples via"| GDal
    GeomZ -->|"samples via"| GDal
    Enrich -->|"samples via"| GDal

    linkStyle 4 opacity:1

    Enrich -->|"uses"| Schema
    Factory -->|"creates"| Schema
    Schema -->|"composed of"| Bin
```

## Classes

| Class | Responsibility |
|---|---|
| [GDalContext](GDalContext.cs) | Holds one worker-local GDAL and coordinate-transformation context for safe parallel processing. |
| [Epsg](GDalContext.cs) | common EPSG coordinate reference system codes used by the application. |
| [GeoJsonAddElevation](GeoJsonAddElevation.cs) | Adds elevation (Z) coordinates to every geometry in a GeoJSON file,  reading height values from a GDAL raster model such as a Copernicus DEM VRT. |
| [GeometryZ](GeometryZ.cs) | Adds z-Component to a Geometry |
| [HistogramBin](HistogramSchema.cs) | (shared) histogram bin definition. |
| [HistogramSchema](HistogramSchema.cs) | shared histogram schema used by all features. |
| [HistogramSchemaFactory](HistogramSchema.cs) | Creates HistogramSchema definitions from an explicit bucket width or value range. |
| [GeoJsonHistogramEnricher](HistogramSchema.cs) | Enriches GeoJSON polygon features with compact per-feature histograms derived from a Copernicus DEM VRT or tile directory. |
| [StreamingGeoJsonProcessor](StreamingGeoJsonProcessor.cs) | Low-memory streaming processor that adds elevation Z coordinates to GeoJSON FeatureCollections  by reading and writing the JSON token-by-token without loading the entire document into memory. |

## Relationships

```mermaid
flowchart TD
    GDalContext["GDalContext"]
    GeoJsonAddElevation["GeoJsonAddElevation"]
    StreamingGeoJsonProcessor["StreamingGeoJsonProcessor"]
    GeometryZ["GeometryZ"]
    GeoJsonHistogramEnricher["GeoJsonHistogramEnricher"]
    HistogramSchema["HistogramSchema"]
    HistogramSchemaFactory["HistogramSchemaFactory"]

    GeoJsonAddElevation -->|"samples via"| GDalContext
    StreamingGeoJsonProcessor -->|"samples via"| GDalContext
    GeometryZ -->|"samples via"| GDalContext
    GeoJsonHistogramEnricher -->|"samples via"| GDalContext
    GeoJsonHistogramEnricher -->|"uses"| HistogramSchema
    HistogramSchemaFactory -->|"creates"| HistogramSchema

    linkStyle 0 opacity:1
    linkStyle 1 opacity:1
    linkStyle 2 opacity:1
    linkStyle 3 opacity:1
    linkStyle 4 opacity:1
    linkStyle 5 opacity:1
```

See also: parent project summary in [../ReadMe.md](../ReadMe.md).
