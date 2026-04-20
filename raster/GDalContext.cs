using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary>  Holds one worker-local GDAL and coordinate-transformation context for safe parallel processing. </summary>  
public sealed class GDalContext : IDisposable {

	/// <summary> common EPSG coordinate reference system codes used by the application. </summary>  
	public enum Epsg {
		/// <summary> Default: WGS 84 geographic coordinates in longitude and latitude. </summary> 
		Wgs84 = 4326,

		/// <summary> Web Mercator projected coordinates. </summary> 
		WebMercator = 3857,

		/// <summary> ETRS89 / LAEA Europe projected coordinates. </summary> 
		Etrs89LaeaEurope = 3035,

		/// <summary> ETRS89 / UTM zone 28N projected coordinates. </summary> 
		Etrs89UtmZone28N = 25828,

		/// <summary> ETRS89 / UTM zone 29N projected coordinates. </summary> 
		Etrs89UtmZone29N = 25829,

		/// <summary> ETRS89 / UTM zone 30N projected coordinates. </summary> 
		Etrs89UtmZone30N = 25830,

		/// <summary> ETRS89 / UTM zone 31N projected coordinates. </summary> 
		Etrs89UtmZone31N = 25831,

		/// <summary> ETRS89 / UTM zone 32N projected coordinates. </summary> 
		Etrs89UtmZone32N = 25832,

		/// <summary> ETRS89 / UTM zone 33N projected coordinates. </summary> 
		Etrs89UtmZone33N = 25833,

		/// <summary> ETRS89 / UTM zone 34N projected coordinates. </summary> 
		Etrs89UtmZone34N = 25834,

		/// <summary> ETRS89 / UTM zone 35N projected coordinates. </summary> 
		Etrs89UtmZone35N = 25835,

		/// <summary> WGS 84 / UTM zone 32N projected coordinates. </summary> 
		Wgs84UtmZone32N = 32632,

		/// <summary> WGS 84 / UTM zone 33N projected coordinates. </summary> 
		Wgs84UtmZone33N = 32633,

		/// <summary> WGS 84 / UTM zone 34N projected coordinates. </summary> 
		Wgs84UtmZone34N = 32634,

		/// <summary> WGS 84 / UTM zone 35N projected coordinates. </summary> 
		Wgs84UtmZone35N = 32635
	}

	/// <summary> Stores the shared histogram schema used by the worker. </summary>  
	private readonly HistogramSchema _histogram;

	/// <summary> Raster dataset. </summary>  
	public readonly Dataset Dataset;

	/// <summary> Raster band. </summary>  
	public readonly Band Band;

	/// <summary> 2x3 homogeneous Raster Matrix. </summary>
	/// <remarks>
	/// map_x=geoTransform[0] + pixel_column×geoTransform[1] + pixel_row×geoTransform[2]
	/// map_y=geoTransform[3] + pixel_column×geoTransform[4] + pixel_row×geoTransform[5]
	/// </remarks>
	private readonly double[] _GeoTransformMatrix;

	/// <summary> Indicates whether the raster band exposes a NoData value. </summary>  
	private readonly bool _hasNoDataValue;

	/// <summary> Stores the raster NoData value when one exists. </summary>  
	private readonly double _noDataValue;

	/// <summary> Stores the source spatial reference for the input GeoJSON. </summary>  
	private readonly SpatialReference _sourceSrs;

	/// <summary> Stores the raster spatial reference. </summary>  
	private readonly SpatialReference _rasterSrs;

	/// <summary> Stores the worker-local coordinate transformation into raster coordinates. </summary>  
	private readonly CoordinateTransformation _transformToRaster;

	/// <summary> Stores the raster extent envelope for quick rejection tests. </summary>  
	private readonly Envelope _rasterExtent;

	/// <summary> Guards all native GDAL calls that are not thread-safe (Open, ReadRaster). </summary>
	public static readonly object GdalLock = new object();

	public static void InitGdal() {
		Gdal.SetConfigOption("GDAL_DATA", @"C:\OSGeo4W\share\gdal");
		Gdal.SetConfigOption("PROJ_LIB", @"C:\OSGeo4W\share\proj");
		Gdal.AllRegister();
	}

	static GDalContext() {
		InitGdal();
	}

	/// <summary> Creates one worker-local processing context. </summary>  
	public GDalContext(string vrtPath, HistogramSchema histogram, Epsg geoJsonEpsg = Epsg.Wgs84, Access access = Access.GA_ReadOnly) {
		lock (GdalLock) {
			Dataset = Gdal.Open(vrtPath, access);
		}
		if (Dataset == null) {
			throw new InvalidOperationException("Could not open raster dataset: " + vrtPath);
		}

		Band = Dataset.GetRasterBand(1);
		if (Band == null) {
			throw new InvalidOperationException("Raster dataset does not contain band 1.");
		}

		//_dataset.ComputePolygonHistogramCounts()
		_histogram = histogram;// ?? HistogramSchemaFactory.Create(
		//	"copdem_vrt_histogram",
		//	vrtHistogram.MinimumValueM,
		//	vrtHistogram.MaximumValueM,
		//	vrtHistogram.BucketCount,
		//	labelDecimalPlaces: 2);

		_GeoTransformMatrix = new double[6];
		Dataset.GetGeoTransform(_GeoTransformMatrix);

		if (Math.Abs(_GeoTransformMatrix[2]) > 1.0e-12 ||
			Math.Abs(_GeoTransformMatrix[4]) > 1.0e-12) {
			throw new NotSupportedException("This sample assumes a north-up raster with no rotation.");
		}
		if (_GeoTransformMatrix[5] >= 0) {
			throw new NotSupportedException("This sample assumes a north-up raster with a negative pixel height.");
		}
		Band.GetNoDataValue(out var _noDataValue, out int hasNoDataFlag);
		_hasNoDataValue = hasNoDataFlag == 1;

		string rasterProjectionWkt = Dataset.GetProjection();
		if (string.IsNullOrWhiteSpace(rasterProjectionWkt)) {
			throw new InvalidOperationException("The raster dataset has no projection information.");
		}
		_sourceSrs = new SpatialReference(string.Empty);
		_sourceSrs.ImportFromEPSG((int) geoJsonEpsg);
		_rasterSrs = new SpatialReference(rasterProjectionWkt);
		_sourceSrs.TrySetTraditionalGisAxisOrder();
		_rasterSrs.TrySetTraditionalGisAxisOrder();
		_transformToRaster = new CoordinateTransformation(_sourceSrs, _rasterSrs);
		_rasterExtent = Dataset.CreateRasterExtentEnvelope(_GeoTransformMatrix);
	}

	/// <summary> Creates an empty histogram-count array for one feature. </summary> 
	private static long[] CreateEmptyCounts(int bucketCount) => new long[bucketCount];
	private static double[] CreateEmptyAreas(int bucketCount) => new double[bucketCount];

	[ThreadStatic]
	static double[] buffer;

	public double Sample(double lon, double lat) {
		double px = (lon - _GeoTransformMatrix[0]) / _GeoTransformMatrix[1]; //assumes no Rotation
		double py = (lat - _GeoTransformMatrix[3]) / _GeoTransformMatrix[5];

		var x = (int) Math.Round(px);
		var y = (int) Math.Round(py);
		try {
			buffer ??= new double[1];
			//lock (GdalLock)
				Band.ReadRaster(x, y, 1, 1, buffer, 1, 1, 0, 0);
			return buffer[0];
		} catch (Exception e) {
			Trace.TraceError(e + "");
			Console.Error.WriteLine(e);
			return 1;
		}
	}

	/// <summary> Calculates the histogram counts. </summary>
	public long[] GetHistogramCounts(IFeature feature) {
		if (feature == null || feature.Geometry == null) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		Geometry inputGeometry = feature.Geometry;
		if (!(inputGeometry is Polygon) && !(inputGeometry is MultiPolygon)) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		Geometry zoneInRasterCrs = inputGeometry.TransformPolygonalGeometry(_transformToRaster);
		if (zoneInRasterCrs == null || zoneInRasterCrs.IsEmpty) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		if (!zoneInRasterCrs.IsValid) {
			zoneInRasterCrs = zoneInRasterCrs.Buffer(0);
		}
		if (zoneInRasterCrs == null || zoneInRasterCrs.IsEmpty) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		if (!(zoneInRasterCrs is Polygon) && !(zoneInRasterCrs is MultiPolygon)) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		if (!zoneInRasterCrs.EnvelopeInternal.Intersects(_rasterExtent)) {
			return CreateEmptyCounts(_histogram.BucketCount);
		}
		return Dataset.PolygonHistogramByCounts(Band, zoneInRasterCrs, _GeoTransformMatrix, _hasNoDataValue, _noDataValue, _histogram);
	}

	/// <summary> Calculates the histogram Areas. </summary>
	public double[] GetHistogramAreas(IFeature feature, string context) {
		if (feature == null || feature.Geometry == null) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		Geometry inputGeometry = feature.Geometry;
		if (!(inputGeometry is Polygon) && !(inputGeometry is MultiPolygon)) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		Geometry zoneInRasterCrs = inputGeometry.TransformPolygonalGeometry(_transformToRaster);
		if (zoneInRasterCrs == null || zoneInRasterCrs.IsEmpty) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		if (!zoneInRasterCrs.IsValid) {
			zoneInRasterCrs = zoneInRasterCrs.Buffer(0);
		}
		if (zoneInRasterCrs == null || zoneInRasterCrs.IsEmpty) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		if (!(zoneInRasterCrs is Polygon) && !(zoneInRasterCrs is MultiPolygon)) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		if (!zoneInRasterCrs.EnvelopeInternal.Intersects(_rasterExtent)) {
			return CreateEmptyAreas(_histogram.BucketCount);
		}
		return Dataset.PolygonHistogramByArea(Band, zoneInRasterCrs, _GeoTransformMatrix, _hasNoDataValue, _noDataValue, _histogram, context);
	}

	/// <summary> Disposes all worker-local GDAL and spatial-reference resources. </summary>  
	public void Dispose() {
		if (_transformToRaster != null) {
			_transformToRaster.Dispose();
		}

		if (_sourceSrs != null) {
			_sourceSrs.Dispose();
		}

		if (_rasterSrs != null) {
			_rasterSrs.Dispose();
		}

		if (Band != null) {
			Band.Dispose();
		}

		if (Dataset != null) {
			Dataset.Dispose();
		}
	}

}
