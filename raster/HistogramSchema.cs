namespace org.SpocWeb.root.files.Tests.raster;

using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using OSGeo.GDAL;
using OSGeo.OSR;
using static GDalContext;

/// <summary> (shared) histogram bin definition.  </summary>  
public sealed class HistogramBin {
	/// <summary> zero-based index of the bin. </summary>  
	public int BinIndex { get; set; }

	/// <summary> inclusive lower bound of the bin </summary>  
	public double MinimumValueM { get; set; }

	/// <summary> upper bound of the bin </summary>  
	public double MaximumValueM { get; set; }

	/// <summary> exact interval notation for the bin. </summary>  
	public string IntervalNotation { get; set; }

	/// <summary> human-readable label for the bin. </summary>  
	public string Label { get; set; }
}

/// <summary> shared histogram schema used by all features. </summary>  
public sealed class HistogramSchema {

	/// <summary> identifier that links features to this schema. </summary>  
	public string HistogramSchemaId { get; set; }

	/// <summary> measurement unit for bin values, e.g. meters. </summary>  
	public string Unit { get; set; }

	/// <summary> textual description of the interval boundary convention. </summary>  
	public string IntervalConvention { get; set; }

	/// <summary> textual description of the out-of-range handling policy. </summary>  
	public string OutOfRangePolicy { get; set; }

	/// <summary> textual description of the cell inclusion rule. </summary>  
	public string CellInclusionRule { get; set; }

	/// <summary> global histogram minimum </summary>  
	public double MinimumValueM { get; set; }

	/// <summary> global histogram maximum </summary>  
	public double MaximumValueM { get; set; }

	/// <summary> total number of bins in the histogram. </summary>  
	public int BucketCount { get; set; }

	/// <summary> width of each histogram bin </summary>  
	public double BucketWidthM { get; set; }

	/// <summary> ordered collection of <see cref="HistogramBin"/>. </summary>  
	public List<HistogramBin> Bins { get; set; }
}

/// <inheritdoc cref="Create(string, double, double, int, int)"/>
public static class HistogramSchemaFactory {

	/// <summary>  Creates shared histogram schema definitions. </summary>
	public static HistogramSchema Create(
		string histogramSchemaId,
		double histogramMinM,
		double histogramMaxM,
		int bucketCount,
		int labelDecimalPlaces = 2) {
		if (string.IsNullOrWhiteSpace(histogramSchemaId)) {
			throw new ArgumentException("histogramSchemaId is required.", nameof(histogramSchemaId));
		}

		if (double.IsNaN(histogramMinM) || double.IsInfinity(histogramMinM)) {
			throw new ArgumentOutOfRangeException(nameof(histogramMinM), "histogramMinM must be finite.");
		}

		if (double.IsNaN(histogramMaxM) || double.IsInfinity(histogramMaxM)) {
			throw new ArgumentOutOfRangeException(nameof(histogramMaxM), "histogramMaxM must be finite.");
		}

		if (histogramMaxM <= histogramMinM) {
			throw new ArgumentOutOfRangeException(nameof(histogramMaxM), "histogramMaxM must be greater than histogramMinM.");
		}

		if (bucketCount <= 0) {
			throw new ArgumentOutOfRangeException(nameof(bucketCount), "bucketCount must be greater than zero.");
		}

		if (labelDecimalPlaces < 0) {
			throw new ArgumentOutOfRangeException(nameof(labelDecimalPlaces), "labelDecimalPlaces must be zero or greater.");
		}

		var bucketWidthM = (histogramMaxM - histogramMinM) / bucketCount;
		var bins = new List<HistogramBin>(bucketCount);

		var labelFormat = labelDecimalPlaces > 0
			? "0." + new string('0', labelDecimalPlaces)
			: "0";
		var exactFormat = "0.###############";

		for (var binIndex = 0; binIndex < bucketCount; binIndex++) {
			var isLastBin = binIndex == bucketCount - 1;
			var minimumValueM = histogramMinM + (binIndex * bucketWidthM);
			var maximumValueM = isLastBin
				? histogramMaxM
				: histogramMinM + ((binIndex + 1) * bucketWidthM);

			var intervalNotation = isLastBin
				? "[" + minimumValueM.ToString(exactFormat, CultureInfo.InvariantCulture) + ", " + histogramMaxM.ToString(exactFormat, CultureInfo.InvariantCulture) + "]"
				: "[" + minimumValueM.ToString(exactFormat, CultureInfo.InvariantCulture) + ", " + maximumValueM.ToString(exactFormat, CultureInfo.InvariantCulture) + ")";

			var label = minimumValueM.ToString(labelFormat, CultureInfo.InvariantCulture)
				+ " to "
				+ maximumValueM.ToString(labelFormat, CultureInfo.InvariantCulture)
				+ " m";

			bins.Add(new HistogramBin {
				BinIndex = binIndex,
				MinimumValueM = minimumValueM,
				MaximumValueM = maximumValueM,
				IntervalNotation = intervalNotation,
				Label = label
			});
		}

		return new HistogramSchema {
			HistogramSchemaId = histogramSchemaId,
			Unit = "m",
			IntervalConvention = "LeftClosedRightOpenExceptLastClosed",
			OutOfRangePolicy = "FoldIntoEdgeBins",
			CellInclusionRule = "PixelCenterInsideOrBoundary",
			MinimumValueM = histogramMinM,
			MaximumValueM = histogramMaxM,
			BucketCount = bucketCount,
			BucketWidthM = bucketWidthM,
			Bins = bins
		};
	}
}

public static class HistogramSchemaFileWriter {

	/// <summary> Writes the <paramref name="schema"/> to the <paramref name="csvPath"/> </summary>
	public static void WriteCsv(this HistogramSchema schema, string csvPath) {
		if (schema == null) {
			throw new ArgumentNullException(nameof(schema));
		}

		if (string.IsNullOrWhiteSpace(csvPath)) {
			throw new ArgumentException("csvPath is required.", nameof(csvPath));
		}

		using (var writer = new StreamWriter(csvPath, false)) {
			writer.WriteLine("HistogramSchemaId,BinIndex,MinimumValueM,MaximumValueM,IntervalNotation,Label");

			foreach (var bin in schema.Bins) {
				writer.WriteLine(
					EscapeCsv(schema.HistogramSchemaId) + "," +
					bin.BinIndex.ToString(CultureInfo.InvariantCulture) + "," +
					bin.MinimumValueM.ToString("0.###############", CultureInfo.InvariantCulture) + "," +
					bin.MaximumValueM.ToString("0.###############", CultureInfo.InvariantCulture) + "," +
					EscapeCsv(bin.IntervalNotation) + "," +
					EscapeCsv(bin.Label));
			}
		}
	}

	/// <summary> Writes the <paramref name="schema"/> to the <paramref name="jsonPath"/> </summary>
	public static void WriteJson(HistogramSchema schema, string jsonPath) {
		if (schema == null) {
			throw new ArgumentNullException(nameof(schema));
		}

		if (string.IsNullOrWhiteSpace(jsonPath)) {
			throw new ArgumentException("jsonPath is required.", nameof(jsonPath));
		}

		var json = JsonConvert.SerializeObject(schema, Formatting.Indented);
		File.WriteAllText(jsonPath, json);
	}

	/// <summary> Escapes the <paramref name="text"/> for safe CSV output. </summary>  
	public static string EscapeCsv(string text) {
		if (text == null) {
			return string.Empty;
		}
		var requiresQuotes = text.Contains(",") || text.Contains("\"") || text.Contains("\r") || text.Contains("\n");
		if (!requiresQuotes) {
			return text;
		}
		return "\"" + text.Replace("\"", "\"\"") + "\"";
	}
}

/// <summary> feature result containing area totals per histogram bin. </summary>
public sealed class AreaHistogramFeatureResult {

	/// <summary> Gets or sets the area totals for each histogram bin in square meters. </summary>
	public double[] BinAreasM2 { get; init; }

	/// <summary> Gets or sets the total included area in square meters. </summary>
	public double TotalAreaM2 { get; set; }

	/// <summary> Creates an empty area-histogram result for a given bucket count. </summary>
	public static AreaHistogramFeatureResult CreateEmpty(int bucketCount)
		=> new() {
			BinAreasM2 = new double[bucketCount],
			TotalAreaM2 = 0.0
		};
}


/// <summary> Enriches GeoJSON polygon features with compact per-feature histograms derived from a Copernicus DEM VRT or tile directory. </summary>  
public static class CopernicusDemGeoJsonParallelCompactHistogramEnricher {

	public const string GeoJsonExtension = ".geoJson";
	public const string GeoJsonPattern = "*" + GeoJsonExtension;

	/// <summary> 
	/// Uses an existing VRT and writes compact histograms into the output GeoJSON in parallel.  
	/// </summary> 
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Africa\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Asia\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\Obsidian.SpocWeb\_Standards\Earth\Continent")]
	public static void AddHistogram(string vrtElevationFile, string geoJsonDirectory, Epsg geoJsonEpsg = Epsg.Wgs84) {//, int parallelism = 8) {
		var histogram = HistogramSchemaFactory.Create("Elevation0-9000", 0, 9000, 9000/50); //8850m Mt Everest
		var dir = new DirectoryInfo(geoJsonDirectory);
		using var gDal = new GDalContext(vrtElevationFile, histogram, geoJsonEpsg);
		foreach (var geoJsonFile in dir.EnumerateFiles(GeoJsonPattern, SearchOption.AllDirectories)) {
			Trace.WriteLine(geoJsonFile.FullName);
			Debug.WriteLine(geoJsonFile.FullName);
			Console.WriteLine(geoJsonFile.FullName);
			var outputPath = geoJsonFile.FullName
				.Substring(0, geoJsonFile.FullName.Length - GeoJsonExtension.Length) + "Z" + GeoJsonExtension;
			gDal.ProcessGeoJsonAgainstRasterParallel(histogram,
				vrtElevationFile,
				geoJsonFile,
				outputPath,
				geoJsonEpsg); //, ResolveMaxDegreeOfParallelism(maxDegreeOfParallelism), featureBatchSize);
			geoJsonFile.Delete();
			File.Move(outputPath, geoJsonFile.FullName);
		}
	}

	/// <summary> Creates the raster extent envelope from the dataset dimensions and geotransform. </summary>  
	public static Envelope CreateRasterExtentEnvelope(this Dataset dataset, double[]? geoTransform = null) {
		geoTransform ??= new double[6];
		dataset.GetGeoTransform(geoTransform);
		var minX = geoTransform[0];
		var maxX = geoTransform[0] + (dataset.RasterXSize * geoTransform[1]);
		var maxY = geoTransform[3];
		var minY = geoTransform[3] + (dataset.RasterYSize * geoTransform[5]);
		return new Envelope(minX, maxX, minY, maxY);
	}

	/// <summary> Loads the GeoJSON, computes histogram counts in parallel, and writes the compact output file. </summary> 
	private static void ProcessGeoJsonAgainstRasterParallel(this GDalContext gDal
		, HistogramSchema schema,
		string rasterPath,
		FileInfo inputGeoJsonPath,
		string outputGeoJsonPath,
		Epsg geoJsonEpsg = Epsg.Wgs84
		//int effectiveMaxDegreeOfParallelism,
		//int featureBatchSize
		) {
		var featureCollection = LoadFeatureCollection(inputGeoJsonPath);
		if (featureCollection == null) {
			throw new InvalidOperationException("Could not read FeatureCollection from GeoJSON.");
		}

		IFeature[] features = featureCollection.ToArray();
		var results = new long[features.Length][];

		if (features.Length == 0) {
			SaveFeatureCollection(featureCollection, outputGeoJsonPath);
			return;
		}

		features.ForEach((f,i) => results[i] = gDal.GetHistogram(f));

		//ParallelOptions options = new ParallelOptions {
		//	MaxDegreeOfParallelism = effectiveMaxDegreeOfParallelism
		//};

		//var partitions = Partitioner.Create(0, features.Length, featureBatchSize);

		//Parallel.ForEach(
		//	partitions,
		//	options,
		//	() =>
		//(range, loopState, worker) => {
				//for (int index = range.Item1; index < range.Item2; index++) {
				//	results[index] = worker.GetHistogram(features[index]);
				//}

			//	return worker;
			//},
			//worker => worker.Dispose());

		for (var index = 0; index < features.Length; index++) {
			var feature = features[index];
			if (feature == null) {
				continue;
			}

			var attributes = GetOrCreateAttributes(feature);
			var counts = results[index] ?? new long[schema.BucketCount];

			SetAttribute(attributes, "hist_schema_id", schema.HistogramSchemaId);
			SetAttribute(attributes, "elev_hist_counts", counts);
		}

		SaveFeatureCollection(featureCollection, outputGeoJsonPath);
	}

	/// <summary> Validates that the histogram schema is internally consistent. </summary> 
	public static string Validate(HistogramSchema schema) {
		if (schema == null) {
			throw new ArgumentNullException(nameof(schema));
		}

		if (string.IsNullOrWhiteSpace(schema.HistogramSchemaId)) {
			return "schema.HistogramSchemaId is required.";
		}

		if (double.IsNaN(schema.MinimumValueM) || double.IsInfinity(schema.MinimumValueM)) {
			return "schema.MinimumValueM must be finite.";
		}

		if (double.IsNaN(schema.MaximumValueM) || double.IsInfinity(schema.MaximumValueM)) {
			return "schema.MaximumValueM must be finite.";
		}

		if (schema.MaximumValueM <= schema.MinimumValueM) {
			return "schema.MaximumValueM must be greater than schema.MinimumValueM.";
		}

		if (schema.BucketCount <= 0) {
			return "schema.BucketCount must be greater than zero.";
		}

		if (schema.Bins == null || schema.Bins.Count != schema.BucketCount) {
			return "schema.Bins must contain exactly schema.BucketCount items.";
		}

		var expectedBucketWidthM = (schema.MaximumValueM - schema.MinimumValueM) / schema.BucketCount;
		if (Math.Abs(schema.BucketWidthM - expectedBucketWidthM) > 1.0e-12) {
			return "schema.BucketWidthM is inconsistent with the min, max, and bucket count.";
		}
		return "";
	}

	/// <summary> Resolves the effective degree of parallelism for the processing run. </summary> 
	private static int ResolveMaxDegreeOfParallelism(int maxDegreeOfParallelism) {
		if (maxDegreeOfParallelism > 0) {
			return maxDegreeOfParallelism;
		}

		var logicalProcessors = Environment.ProcessorCount;
		return Math.Max(1, Math.Min(logicalProcessors, 6));
	}

	/// <summary> Collects DEM tile paths from a directory. </summary> 
	private static string[] CollectDemTilePaths(string demDirectory, bool recursive) {
		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		var result = new List<string>();
		result.AddRange(Directory.GetFiles(demDirectory, "*.tif", searchOption));
		result.AddRange(Directory.GetFiles(demDirectory, "*.tiff", searchOption));
		result.Sort(StringComparer.OrdinalIgnoreCase);
		return result.ToArray();
	}

	/// <summary> Builds a temporary VRT using the gdalbuildvrt executable. </summary>  
	public static void BuildTemporaryVrtWithGdalBuildVrt(string vrtPath, string listPath, string gdalBuildVrtExePath
		, params string[] tilePaths) {
		File.WriteAllLines(listPath, tilePaths);

		var arguments =
			"-overwrite " +
			"-resolution highest " +
			"-input_file_list " + QuoteArgument(listPath) + " " +
			QuoteArgument(vrtPath);

		var startInfo = new ProcessStartInfo {
			FileName = gdalBuildVrtExePath,
			Arguments = arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = Path.GetDirectoryName(vrtPath)
		};

		using (var process = new Process()) {
			process.StartInfo = startInfo;
			process.Start();

			var standardOutput = process.StandardOutput.ReadToEnd();
			var standardError = process.StandardError.ReadToEnd();

			process.WaitForExit();

			if (process.ExitCode != 0 || !File.Exists(vrtPath)) {
				throw new InvalidOperationException(
					"gdalbuildvrt failed." + Environment.NewLine +
					"Executable: " + gdalBuildVrtExePath + Environment.NewLine +
					"Arguments: " + arguments + Environment.NewLine +
					"ExitCode: " + process.ExitCode + Environment.NewLine +
					"StdOut: " + standardOutput + Environment.NewLine +
					"StdErr: " + standardError);
			}
		}
	}

	/// <summary> Quotes one command-line argument. </summary> 
	private static string QuoteArgument(string value) {
		return "\"" + value + "\"";
	}

	/// <summary> Attempts to delete a file and suppresses deletion errors. </summary> 
	private static void TryDeleteFile(string path) {
		try {
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) {
				File.Delete(path);
			}
		} catch {
		}
	}

	/// <summary> Loads a GeoJSON feature collection from disk. </summary> 
	private static FeatureCollection LoadFeatureCollection(FileInfo path) {
		var serializer = GeoJsonSerializer.Create();
		using (var textReader = new StreamReader(path.FullName))
		using (var jsonReader = new JsonTextReader(textReader)) {
			return serializer.Deserialize<FeatureCollection>(jsonReader);
		}
	}

	/// <summary> Saves a GeoJSON feature collection to disk. </summary> 
	private static void SaveFeatureCollection(FeatureCollection featureCollection, string path) {
		var serializer = GeoJsonSerializer.Create();
		using (var textWriter = new StreamWriter(path))
		using (var jsonWriter = new JsonTextWriter(textWriter)) {
			jsonWriter.Formatting = Formatting.Indented;
			serializer.Serialize(jsonWriter, featureCollection);
		}
	}

	/// <summary> Gets the feature attribute table or creates one if it does not exist. </summary> 
	private static IAttributesTable GetOrCreateAttributes(IFeature feature) {
		if (feature == null) {
			throw new InvalidOperationException("Feature is null.");
		}

		if (feature.Attributes == null) {
			feature.Attributes = new AttributesTable();
		}

		return feature.Attributes;
	}

	/// <summary> Sets or adds one feature attribute value. </summary> 
	private static void SetAttribute(IAttributesTable attributes, string name, object value) {
		if (attributes.Exists(name)) {
			attributes[name] = value;
		} else {
			attributes.Add(name, value);
		}
	}

	/// <summary> Determines whether a raster value should be treated as NoData. </summary> 
	private static bool IsNoData(double value, bool hasNoDataValue, double noDataValue) {
		if (!hasNoDataValue) {
			return false;
		}

		if (double.IsNaN(noDataValue)) {
			return double.IsNaN(value);
		}

		return value.Equals(noDataValue);
	}

	/// <summary> Transforms a polygonal geometry into the raster coordinate reference system. </summary> 
	public static Geometry TransformPolygonalGeometry(this Geometry geometry, CoordinateTransformation transform) {
		var geometryFactory = geometry.Factory;

		if (geometry is Polygon polygon) {
			return polygon.TransformPolygon( transform, geometryFactory);
		}

		if (geometry is MultiPolygon multiPolygon) {
			var polygons = new Polygon[multiPolygon.NumGeometries];
			for (var i = 0; i < multiPolygon.NumGeometries; i++) {
				polygons[i] = ((Polygon) multiPolygon.GetGeometryN(i)).TransformPolygon( transform, geometryFactory);
			}
			return geometryFactory.CreateMultiPolygon(polygons);
		}

		throw new NotSupportedException("Only Polygon and MultiPolygon are supported.");
	}

	/// <summary> Transforms one polygon into the raster coordinate reference system. </summary> 
	public static Polygon TransformPolygon(this Polygon polygon, CoordinateTransformation transform, GeometryFactory geometryFactory) {
		var shell = TransformLinearRing((LinearRing) polygon.ExteriorRing, transform, geometryFactory);

		var holes = new LinearRing[polygon.NumInteriorRings];
		for (var i = 0; i < polygon.NumInteriorRings; i++) {
			holes[i] = TransformLinearRing((LinearRing) polygon.GetInteriorRingN(i), transform, geometryFactory);
		}

		return geometryFactory.CreatePolygon(shell, holes);
	}

	[ThreadStatic]
	static double[]? _Point;

	/// <summary> Transforms one linear ring into the raster coordinate reference system. </summary> 
	private static LinearRing TransformLinearRing(LinearRing ring, CoordinateTransformation transform, GeometryFactory geometryFactory) {
		Coordinate[] source = ring.Coordinates;
		var target = new Coordinate[source.Length];
		_Point ??= new double[2];
		for (var i = 0; i < source.Length; i++) {
			source[i].ToArray(_Point);
			transform.TransformPoint(_Point);
			target[i] = new CoordinateZ(_Point[0], _Point[1], source[i].Z);
		}

		return geometryFactory.CreateLinearRing(target);
	}

	public static void ToArray(this Coordinate coordinate, double[] point) {
		if (point.Length > 2)
			point[2] = coordinate.Z;
		point[1] = coordinate.Y;
		point[0] = coordinate.X;
	}

	/// <summary> Attempts to force traditional GIS axis order on a spatial reference. </summary> 
	public static void TrySetTraditionalGisAxisOrder(this SpatialReference spatialReference) {
		if (spatialReference == null) {
			return;
		}

		try {
			var method = spatialReference.GetType().GetMethod("SetAxisMappingStrategy");
			if (method == null) {
				return;
			}

			var enumType = method.GetParameters()[0].ParameterType;
			var enumValue = Enum.Parse(enumType, "OAMS_TRADITIONAL_GIS_ORDER");
			method.Invoke(spatialReference, new [] { enumValue });
		} catch {
		}
	}


	private const double RadPerDegree = Math.PI / 180.0;

	/// <summary> geographic ground area of one raster cell row element in rad². </summary>  
	private static double ComputeGeographicPixelArea(
		double pixelWidthDegrees,
		double northEdgeLatitudeDegrees,
		double southEdgeLatitudeDegrees) {
		var northLatitudeRadians = ClampLatitudeDegrees(northEdgeLatitudeDegrees) * RadPerDegree;
		var southLatitudeRadians = ClampLatitudeDegrees(southEdgeLatitudeDegrees) * RadPerDegree;

		var deltaLongitudeRadians = pixelWidthDegrees * RadPerDegree;
		var areaM2 = deltaLongitudeRadians * (Math.Sin(northLatitudeRadians) - Math.Sin(southLatitudeRadians));
		return Math.Abs(areaM2);
	}

	/// <summary> Clamps a latitude value into the valid geographic range. </summary>  
	public static double ClampLatitudeDegrees(double latitudeDegrees) =>
		latitudeDegrees > 90.0 ? 90.0 :
		latitudeDegrees < -90.0 ? -90.0 : latitudeDegrees;

	/// <summary> Computes the area-weighted histogram for a polygon in raster coordinates. </summary>  
	public static double[] PolygonHistogramByArea(this Dataset dem, Band band, Geometry polygonInRasterCrs
		, double[] geoTransform, bool hasNoDataValue, double noDataValue, HistogramSchema schema) {
		var areas = new double[schema.BucketCount];

		var envelope = polygonInRasterCrs.EnvelopeInternal;
		var pixelWidth = geoTransform[1];
		var pixelHeightAbs = Math.Abs(geoTransform[5]);

		var rasterWidth = dem.RasterXSize;
		var rasterHeight = dem.RasterYSize;

		var xOff = Math.Max(0, (int) Math.Floor((envelope.MinX - geoTransform[0]) / pixelWidth));
		var xEnd = Math.Min(rasterWidth, (int) Math.Ceiling((envelope.MaxX - geoTransform[0]) / pixelWidth));

		var yOff = Math.Max(0, (int) Math.Floor((geoTransform[3] - envelope.MaxY) / pixelHeightAbs));
		var yEnd = Math.Min(rasterHeight, (int) Math.Ceiling((geoTransform[3] - envelope.MinY) / pixelHeightAbs));

		var windowWidth = xEnd - xOff;
		var windowHeight = yEnd - yOff;

		if (windowWidth <= 0 || windowHeight <= 0) {
			return areas;
		}

		var values = new double[windowWidth * windowHeight];
		band.ReadRaster(xOff, yOff, windowWidth, windowHeight, values, windowWidth, windowHeight, 0, 0);

		var locator = new IndexedPointInAreaLocator(polygonInRasterCrs);
		var histogramMinM = schema.MinimumValueM;
		var histogramMaxM = schema.MaximumValueM;
		var bucketWidthM = schema.BucketWidthM;
		var bucketCount = schema.BucketCount;

		for (var row = 0; row < windowHeight; row++) {
			var absoluteRowIndex = yOff + row;
			var northEdgeLatitudeDegrees = geoTransform[3] + (absoluteRowIndex * geoTransform[5]);
			var southEdgeLatitudeDegrees = geoTransform[3] + ((absoluteRowIndex + 1) * geoTransform[5]);
			var pixelArea = ComputeGeographicPixelArea(pixelWidth, northEdgeLatitudeDegrees, southEdgeLatitudeDegrees);
			var centerY = geoTransform[3] + ((absoluteRowIndex + 0.5) * geoTransform[5]);
			var rowStart = row * windowWidth;

			for (var col = 0; col < windowWidth; col++) {
				var value = values[rowStart + col];
				if (IsNoData(value, hasNoDataValue, noDataValue)) {
					continue;
				}
				var centerX = geoTransform[0] + ((xOff + col + 0.5) * geoTransform[1]);
				var location = locator.Locate(new Coordinate(centerX, centerY));
				if (location == Location.Exterior) {
					continue;
				}
				int bucketIndex;
				if (value < histogramMinM) {
					bucketIndex = 0;
				} else if (value >= histogramMaxM) {
					bucketIndex = bucketCount - 1;
				} else {
					bucketIndex = (int) Math.Floor((value - histogramMinM) / bucketWidthM);
				}
				areas[bucketIndex] += pixelArea; //aggregate the areas
			}
		}
		return areas;
	}

	/// <summary> Computes one compact histogram-count array for a polygon in raster coordinates. </summary> 
	public static long[] PolygonHistogramByCounts(this Dataset dem, Band band, Geometry polygonInRasterCrs
		, double[] geoTransform, bool hasNoDataValue, double noDataValue, HistogramSchema schema) {
		var counts = new long[schema.BucketCount];

		var envelope = polygonInRasterCrs.EnvelopeInternal;
		var pixelWidth = geoTransform[1];
		var pixelHeightAbs = Math.Abs(geoTransform[5]);

		var rasterWidth = dem.RasterXSize;
		var rasterHeight = dem.RasterYSize;

		var xOff = Math.Max(0, (int) Math.Floor((envelope.MinX - geoTransform[0]) / pixelWidth));
		var xEnd = Math.Min(rasterWidth, (int) Math.Ceiling((envelope.MaxX - geoTransform[0]) / pixelWidth));

		var yOff = Math.Max(0, (int) Math.Floor((geoTransform[3] - envelope.MaxY) / pixelHeightAbs));
		var yEnd = Math.Min(rasterHeight, (int) Math.Ceiling((geoTransform[3] - envelope.MinY) / pixelHeightAbs));

		var windowWidth = xEnd - xOff;
		var windowHeight = yEnd - yOff;

		if (windowWidth <= 0 || windowHeight <= 0) {
			return counts;
		}

		var values = new double[windowWidth * windowHeight];
		band.ReadRaster(xOff, yOff, windowWidth, windowHeight, values, windowWidth, windowHeight, 0, 0);

		var locator = new IndexedPointInAreaLocator(polygonInRasterCrs);
		var bucketWidthM = schema.BucketWidthM;
		var histogramMinM = schema.MinimumValueM;
		var histogramMaxM = schema.MaximumValueM;
		var bucketCount = schema.BucketCount;

		for (var row = 0; row < windowHeight; row++) {
			var centerY = geoTransform[3] + ((yOff + row + 0.5) * geoTransform[5]);
			var rowStart = row * windowWidth;

			for (var col = 0; col < windowWidth; col++) {
				var value = values[rowStart + col];
				if (IsNoData(value, hasNoDataValue, noDataValue)) {
					continue;
				}

				var centerX = geoTransform[0] + ((xOff + col + 0.5) * geoTransform[1]);
				var location = locator.Locate(new Coordinate(centerX, centerY));
				if (location == Location.Exterior) {
					continue;
				}

				int bucketIndex;
				if (value < histogramMinM) {
					bucketIndex = 0;
				} else if (value >= histogramMaxM) {
					bucketIndex = bucketCount - 1;
				} else {
					bucketIndex = (int) Math.Floor((value - histogramMinM) / bucketWidthM);
				}
				counts[bucketIndex]++;
			}
		}
		return counts;
	}
}
