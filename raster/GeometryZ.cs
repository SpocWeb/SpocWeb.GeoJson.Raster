using NetTopologySuite.Geometries;
using org.SpocWeb.root.Attributes;
using System.ComponentModel;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary> Adds z-Component to a <see cref="Geometry"/> </summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-04-18T14:41:04Z
/// digest: 748286817e01994ab7231a86addf5d776442f03c95ca4f8ec034cacb079f929d
/// updated: 2026-05-19
/// </remarks>
[Facets(Layer = "domain", Status = "active", Complexity = 2)]
[Tags("code/geometry_transformation", "code/elevation_enrichment")]
[DocState(Pass = 2, MTime = "2026-08-26T09:15:49Z", Digest = "614d471a784aba104c9923263832b3f0d34646572a31bf8e501e2fbf68cbb08d", Stale = false, Path = "raster/GeometryZ.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("Adds z-Component to a Geometry")]
[Concept("digital_elevation_model")]
[Concept("geometry_processing")]
public static class GeometryZ {

	public static GeometryFactory GeometryFactory = new();

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="geometry"/> </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 2)]
	[Tags("code/geometry_transformation")]
	[System.ComponentModel.Description("Adds the Z Dimension from the elevationModel to the geometry")]
	[Concept("digital_elevation_model")]
	public static Geometry AddElevationAsZ(this GDalContext elevationModel, Geometry geometry)
		=> geometry switch {
		Point p => GeometryFactory.CreatePoint(elevationModel.AddElevationAsZ(p.Coordinate)),
		LinearRing lr => GeometryFactory.CreateLinearRing(elevationModel.AddElevationAsZ(lr.Coordinates)),
		LineString ls => GeometryFactory.CreateLineString(elevationModel.AddElevationAsZ(ls.Coordinates)),
		Polygon poly => elevationModel.AddElevationAsZ( poly),
		MultiPoint mp => GeometryFactory.CreateMultiPoint(mp.Geometries.Cast<Point>()
							.Select(p => (Point) elevationModel.AddElevationAsZ(p)).ToArray()),
		MultiLineString mls => GeometryFactory.CreateMultiLineString(mls.Geometries.Cast<LineString>()
							.Select(ls => (LineString) elevationModel.AddElevationAsZ(ls)).ToArray()),
		MultiPolygon mpoly => GeometryFactory.CreateMultiPolygon(mpoly.Geometries.Cast<Polygon>()
							.Select(poly => (Polygon) elevationModel.AddElevationAsZ(poly)).ToArray()),
		GeometryCollection gc => GeometryFactory.CreateGeometryCollection(gc.Geometries
							.Select(elevationModel.AddElevationAsZ).ToArray()),
		_ => geometry,
	};

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="polygon"/> </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 2)]
	[Tags("code/geometry_transformation")]
	[System.ComponentModel.Description("Adds the Z Dimension from the elevationModel to the polygon")]
	[Concept("digital_elevation_model")]
	public static Polygon AddElevationAsZ(this GDalContext elevationModel, Polygon polygon) {
		var shell = GeometryFactory.CreateLinearRing(elevationModel.AddElevationAsZ(polygon.Shell.Coordinates));
		var holes = polygon.Holes
			.Select(h => GeometryFactory.CreateLinearRing(elevationModel.AddElevationAsZ(h.Coordinates)))
			.ToArray();
		return GeometryFactory.CreatePolygon(shell, holes);
	}

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="coordinates"/> </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 1)]
	[Tags("code/geometry_transformation")]
	[System.ComponentModel.Description("Adds the Z Dimension from the elevationModel to the coordinates")]
	[Concept("digital_elevation_model")]
	public static Coordinate[] AddElevationAsZ(this GDalContext elevationModel, Coordinate[] coordinates)
		=> double.IsNaN(coordinates[0].Z)
			? coordinates.Select(elevationModel.AddElevationAsZ).ToArray()
			: coordinates;

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="coordinates"/> </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 1)]
	[Tags("code/geometry_transformation")]
	[System.ComponentModel.Description("Adds the Z Dimension from the elevationModel to the coordinates")]
	[Concept("digital_elevation_model")]
	public static Coordinate AddElevationAsZ(this GDalContext elevationModel, Coordinate coordinates)
		=> double.IsNaN(coordinates.Z)
		? new CoordinateZ(coordinates.X, coordinates.Y, Math.Round(elevationModel.Sample(coordinates.X, coordinates.Y), 4))
		: coordinates;
}
