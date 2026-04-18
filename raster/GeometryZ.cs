using NetTopologySuite.Geometries;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary> Adds z-Component to a <see cref="Geometry"/> </summary>
public static class GeometryZ {

	public static GeometryFactory GeometryFactory = new();

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="geometry"/> </summary>
	public static Geometry AddElevationAsZ(this ElevationModelSampler elevationModel, Geometry geometry)
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
	public static Polygon AddElevationAsZ(this ElevationModelSampler elevationModel, Polygon polygon) {
		var shell = GeometryFactory.CreateLinearRing(elevationModel.AddElevationAsZ(polygon.Shell.Coordinates));
		var holes = polygon.Holes
			.Select(h => GeometryFactory.CreateLinearRing(elevationModel.AddElevationAsZ(h.Coordinates)))
			.ToArray();
		return GeometryFactory.CreatePolygon(shell, holes);
	}

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="coordinates"/> </summary>
	public static Coordinate[] AddElevationAsZ(this ElevationModelSampler elevationModel, Coordinate[] coordinates)
		=> double.IsNaN(coordinates[0].Z)
			? coordinates.Select(elevationModel.AddElevationAsZ).ToArray()
			: coordinates;

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="coordinates"/> </summary>
	public static Coordinate AddElevationAsZ(this ElevationModelSampler elevationModel, Coordinate coordinates)
		=> double.IsNaN(coordinates.Z)
		? new CoordinateZ(coordinates.X, coordinates.Y, Math.Round(elevationModel.Sample(coordinates.X, coordinates.Y), 4))
		: coordinates;
}
