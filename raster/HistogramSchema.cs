using org.SpocWeb.root.db.stream.ado;

namespace org.SpocWeb.root.files.Tests.raster;

using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using OSGeo.GDAL;
using OSGeo.OSR;
using static GDalContext;
using org.SpocWeb.root.Attributes;
using System.ComponentModel;

/// <summary> (shared) histogram bin definition.  </summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-05-04T19:51:06Z
/// digest: ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
/// updated: 2026-05-19
/// </remarks>
[DocState(Pass = 2, MTime = "2026-08-22T20:36:31Z", Digest = "9bbda65e43164d84c53064634085f069774ae6079d0a0eec2db7b8eaab158e2a", Stale = false, Path = "raster/HistogramSchema.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("(shared) histogram bin definition.")]
public sealed class HistogramBin {

	/// <summary> zero-based index of this bin. </summary>  
	[System.ComponentModel.Description("zero-based index of this bin.")]
	public int BinIndex { get; set; }

	/// <summary> inclusive lower bound of this bin </summary>  
	[System.ComponentModel.Description("inclusive lower bound of this bin")]
	public double MinimumValue { get; set; }

	/// <summary> upper bound of this bin </summary>  
	[System.ComponentModel.Description("upper bound of this bin")]
	public double MaximumValue { get; set; }

	/// <summary> Mid Value of this bin </summary>  
	[System.ComponentModel.Description("Mid Value of this bin")]
	public double MidValue => (MaximumValue + MinimumValue) * 0.5;

	/// <summary> Width of this bin </summary>  
	[System.ComponentModel.Description("Width of this bin")]
	public double Width => (MaximumValue - MinimumValue) * 0.5;

	/// <summary> exact interval notation for this bin. </summary>  
	[System.ComponentModel.Description("exact interval notation for this bin.")]
	public string IntervalNotation { get; set; }

	/// <summary> human-readable label for this bin. </summary>  
	[System.ComponentModel.Description("human-readable label for this bin.")]
	public string Label { get; set; }

	/// <inheritdoc />
	public override string ToString() => Label;
}

/// <summary> shared histogram schema used by all features. </summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-05-04T19:51:06Z
/// digest: ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
/// updated: 2026-05-19
/// </remarks>
[DocState(Pass = 2, MTime = "2026-08-22T20:36:31Z", Digest = "b6829b271168e12fd0d71b100c611f873457f3bfa05b179b170d282a06153f1c", Stale = false, Path = "raster/HistogramSchema.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("shared histogram schema used by all features.")]
public sealed class HistogramSchema {

	/// <summary> identifier that links features to this schema. </summary>  
	[System.ComponentModel.Description("identifier that links features to this schema.")]
	public string HistogramSchemaId { get; set; }

	/// <summary> measurement unit for bin values, e.g. meters. </summary>  
	[System.ComponentModel.Description("measurement unit for bin values, e.g.")]
	public string Unit { get; set; }

	/// <summary> textual description of the interval boundary convention. </summary>  
	[System.ComponentModel.Description("textual description of the interval boundary convention.")]
	public string IntervalConvention { get; set; }

	/// <summary> textual description of the out-of-range handling policy. </summary>  
	[System.ComponentModel.Description("textual description of the out-of-range handling policy.")]
	public string OutOfRangePolicy { get; set; }

	/// <summary> textual description of the cell inclusion rule. </summary>  
	[System.ComponentModel.Description("textual description of the cell inclusion rule.")]
	public string CellInclusionRule { get; set; }

	/// <summary> global histogram minimum </summary>  
	[System.ComponentModel.Description("global histogram minimum")]
	public double MinimumValue { get; set; }

	/// <summary> global histogram maximum </summary>  
	[System.ComponentModel.Description("global histogram maximum")]
	public double MaximumValue { get; set; }

	/// <summary> total number of bins in the histogram. </summary>  
	[System.ComponentModel.Description("total number of bins in the histogram.")]
	public int BucketCount { get; set; }

	/// <summary> width of each histogram bin </summary>  
	[System.ComponentModel.Description("width of each histogram bin")]
	public double BucketWidth { get; set; }

	/// <summary> ordered collection of <see cref="HistogramBin"/>. </summary>  
	[System.ComponentModel.Description("ordered collection of HistogramBin.")]
	public List<HistogramBin> Bins { get; set; }
}

/// <summary> Creates <see cref="HistogramSchema"/> definitions from an explicit bucket width or value range. </summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-05-04T19:51:06Z
/// digest: ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
/// updated: 2026-05-19
/// </remarks>
[DocState(Pass = 2, MTime = "2026-08-22T20:36:31Z", Digest = "bfe761ccdca398e6e43f2ecbdab9e10dd8f42136b479b23a086aaf33adcacfc7", Stale = false, Path = "raster/HistogramSchema.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("Creates HistogramSchema definitions from an explicit bucket width or value range.")]
public static class HistogramSchemaFactory {

	/// <summary>  Creates shared histogram schema definitions. </summary>
	[System.ComponentModel.Description("Creates shared histogram schema definitions.")]
	public static HistogramSchema CreateFromWidth(
		string histogramSchemaId,
		double histogramMin,
		int bucketCount,
		double bucketWidth,
		int labelDecimalPlaces = 2) => CreateFromRange(histogramSchemaId,
		histogramMin,
		histogramMin + bucketCount * bucketWidth,
		bucketCount,
		labelDecimalPlaces = 2);

	/// <summary>  Creates shared histogram schema definitions. </summary>
	[System.ComponentModel.Description("Creates shared histogram schema definitions.")]
	public static HistogramSchema CreateFromRange(
		string histogramSchemaId,
		double histogramMin,
		double histogramMax,
		int bucketCount,
		int labelDecimalPlaces = 2) {
		if (string.IsNullOrWhiteSpace(histogramSchemaId)) {
			throw new ArgumentException("histogramSchemaId is required.", nameof(histogramSchemaId));
		}
		if (double.IsNaN(histogramMin) || double.IsInfinity(histogramMin)) {
			throw new ArgumentOutOfRangeException(nameof(histogramMin), "histogramMinM must be finite.");
		}
		if (double.IsNaN(histogramMax) || double.IsInfinity(histogramMax)) {
			throw new ArgumentOutOfRangeException(nameof(histogramMax), "histogramMaxM must be finite.");
		}
		if (histogramMax <= histogramMin) {
			throw new ArgumentOutOfRangeException(nameof(histogramMax), "histogramMaxM must be greater than histogramMinM.");
		}
		if (bucketCount <= 0) {
			throw new ArgumentOutOfRangeException(nameof(bucketCount), "bucketCount must be greater than zero.");
		}
		if (labelDecimalPlaces < 0) {
			throw new ArgumentOutOfRangeException(nameof(labelDecimalPlaces), "labelDecimalPlaces must be zero or greater.");
		}
		var bucketWidth = (histogramMax - histogramMin) / bucketCount;


		var bins = new List<HistogramBin>(bucketCount);
		var labelFormat = labelDecimalPlaces > 0
			? "0." + new string('0', labelDecimalPlaces)
			: "0";
		var exactFormat = "0.###############";

		for (var binIndex = 0; binIndex < bucketCount; binIndex++) {
			var isLastBin = binIndex == bucketCount - 1;
			var minimumValue = histogramMin + (binIndex * bucketWidth);
			var maximumValue = isLastBin
				? histogramMax
				: histogramMin + ((binIndex + 1) * bucketWidth);

			var intervalNotation = isLastBin
				? "[" + minimumValue.ToString(exactFormat, CultureInfo.InvariantCulture) + ", " + histogramMax.ToString(exactFormat, CultureInfo.InvariantCulture) + "]"
				: "[" + minimumValue.ToString(exactFormat, CultureInfo.InvariantCulture) + ", " + maximumValue.ToString(exactFormat, CultureInfo.InvariantCulture) + ")";

			var label = minimumValue.ToString(labelFormat, CultureInfo.InvariantCulture)
				+ " to "
				+ maximumValue.ToString(labelFormat, CultureInfo.InvariantCulture)
				+ " m";

			bins.Add(new HistogramBin {
				BinIndex = binIndex,
				MinimumValue = minimumValue,
				MaximumValue = maximumValue,
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
			MinimumValue = histogramMin,
			MaximumValue = histogramMax,
			BucketCount = bucketCount,
			BucketWidth = bucketWidth,
			Bins = bins
		};
	}
}

/// <summary> Enriches GeoJSON polygon features with compact per-feature histograms derived from a Copernicus DEM VRT or tile directory. </summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-05-04T19:51:06Z
/// digest: ad370b8549690c526772074b2919bc6c31c75b6aff07686962a6f911cb882fc1
/// updated: 2026-05-19
/// </remarks>
[DocState(Pass = 2, MTime = "2026-08-23T20:45:06Z", Digest = "51a8275f98d70ec24d072e4b6282e71216d72c2786f95a27e1a89b37994d6b48", Stale = false, Path = "raster/HistogramSchema.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("Enriches GeoJSON polygon features with compact per-feature histograms derived from a Copernicus DEM VRT or tile directory.")]
public static class GeoJsonHistogramEnricher {

	/// <summary>Specifies the constant geo Json Pattern.</summary>
	public const string GeoJsonExtension = ".geoJson";
	/// <summary>Specifies the constant geo Json Pattern.</summary>
	public const string GeoJsonPattern = "*" + GeoJsonExtension;

	/// <summary> Mean Earth radius used by the spherical pixel-area approximation. </summary>  
	public const double EarthRadiusKM = 6_371.008_8;

	/// <summary> Uses an existing VRT and writes compact histograms into the output GeoJSON </summary> 
	[System.ComponentModel.Description("Uses an existing VRT and writes compact histograms into the output GeoJSON")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Africa\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Asia\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\Obsidian.SpocWeb\_Standards\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\SpocWeb\_Standards\Earth\Continent\")]
	public static void AddHistogram(string vrtElevationFile, string geoJsonDirectory
		, Epsg geoJsonEpsg = Epsg.Wgs84, int parallelism = 0) {
		var halfWidth = 25;
		var histogram = HistogramSchemaFactory.CreateFromWidth("Elevation0-9000", -100 + halfWidth * 0.5, 9000/ halfWidth, halfWidth); //8850m Mt Everest
		double areaKM2 = EarthRadiusKM * EarthRadiusKM;
		var dir = new DirectoryInfo(geoJsonDirectory);
		var files = dir.EnumerateFiles(GeoJsonPattern, SearchOption.AllDirectories).ToList();
		var effectiveParallelism = parallelism > 0 ? parallelism : 5 * Environment.ProcessorCount;
		Parallel.ForEach(
			files,
			new ParallelOptions { MaxDegreeOfParallelism = effectiveParallelism },
			() => new GDalContext(vrtElevationFile, histogram, geoJsonEpsg),
			(geoJsonFile, loopState, localGDal) => {
				//var message = DateTime.Now + " " + geoJsonFile.FullName;
				//Trace.WriteLine(message);
				//Debug.WriteLine(message);
				//Console.WriteLine(message);
				var outputPath = new FileInfo(geoJsonFile.FullName
					.Substring(0, geoJsonFile.FullName.Length - GeoJsonExtension.Length) + "H" + GeoJsonExtension);
				try {
					if (localGDal.AddHistogramAreas(histogram, geoJsonFile, outputPath, areaKM2, "area_km2")) {
						geoJsonFile.Delete();
						outputPath.MoveTo(geoJsonFile.FullName);
					} else {
						Trace.WriteLine($"Ignored {geoJsonFile.FullName}");
					}
				} catch (Exception x) {
					var errorMessage = geoJsonFile.FullName + x;
					Debug.WriteLine(errorMessage);
					Trace.TraceError(errorMessage);
					Console.Error.WriteLine(errorMessage);
				}
				return localGDal;
			},
			localGDal => localGDal.Dispose());
	}

	/// <summary> Creates the raster extent envelope from the dataset dimensions and geoTransform. </summary>  
	[System.ComponentModel.Description("Creates the raster extent envelope from the dataset dimensions and geoTransform.")]
	public static Envelope CreateRasterExtentEnvelope(this Dataset dataset, double[]? geoTransform = null) {
		geoTransform ??= new double[6];
		dataset.GetGeoTransform(geoTransform);
		var minX = geoTransform[0];
		var maxX = geoTransform[0] + (dataset.RasterXSize * geoTransform[1]);
		var maxY = geoTransform[3];
		var minY = geoTransform[3] + (dataset.RasterYSize * geoTransform[5]);
		return new Envelope(minX, maxX, minY, maxY);
	}

	/// <summary> Loads the GeoJSON, computes histogram Areas, and writes the output file. </summary> 
	[System.ComponentModel.Description("Loads the GeoJSON, computes histogram Areas, and writes the output file.")]
	public static bool AddHistogramAreas(this GDalContext gDal
		, HistogramSchema histogram, FileInfo inputGeoJsonPath, FileInfo outputGeoJsonPath, double scale, string label) {
		var feature = inputGeoJsonPath.GeoJsonDeserialize<Feature>();
		if (feature == null) {
			throw new InvalidOperationException("Could not read FeatureCollection from GeoJSON " + inputGeoJsonPath.FullName);
		}
		var attributes = feature.GetOrCreateAttributes();
		var oldHist = attributes.GetAttribute("hist_" + label);
		if (oldHist != null) {
			return false;
		}

		var areas = gDal.GetHistogramAreas(feature, inputGeoJsonPath.FullName);
		var totalArea = areas.Sum();
		if (totalArea <= 0) {
			return false;
		}
		var avgArea = totalArea / areas.Length;
		var minArea = avgArea / areas.Length;
		var digits = 2 - (int) Math.Log10(minArea * scale);
		var areaByElevation = new Dictionary<int, double>();
		for (var i = 0; i < areas.Length; i++) {
			var area = areas[i];
			if (area <= minArea) {
				continue;
			}
			var histogramBin = histogram.Bins[i];
			var rounded = Round(area * scale, digits);
			areaByElevation[(int) histogramBin.MidValue] = (double)rounded;
		}

		SetAttribute(attributes, label, Round(totalArea * scale, digits));
		SetAttribute(attributes, "hist_schema_id", histogram.HistogramSchemaId);
		SetAttribute(attributes, "hist_" + label, areaByElevation);

		var sb = new StringBuilder();
		sb.GeoJsonSerialize(feature, 0, "");
		sb.Replace(@"]],[[", "]],\n[[");
		sb.Replace(@"},", "}\n,");
		//outputGeoJsonPath.GeoJsonSerialize(feature, 0, "");
		outputGeoJsonPath.WriteAllText(sb.ToString());
		return true;
	}

	/// <summary> Rounds <paramref name="value"/> to the given number of decimal <paramref name="digits"/> using away-from-zero midpoint rounding, returned as <see cref="decimal"/>. </summary>
	[System.ComponentModel.Description("Rounds value to the given number of decimal digits using away-from-zero midpoint rounding, returned as decimal.")]
	public static decimal Round(this double value, int digits) {
		var pow10 = Math.Pow(10, -digits);
		var result = Math.Round(value / pow10, 0, MidpointRounding.AwayFromZero) * pow10;
		return (decimal) result;
	}

	/// <summary> Loads the GeoJSON, computes histogram counts in parallel, and writes the compact output file. </summary> 
	[System.ComponentModel.Description("Loads the GeoJSON, computes histogram counts in parallel, and writes the compact output file.")]
	public static void AddHistogramCounts(this GDalContext gDal
		, HistogramSchema schema,
		string rasterPath,
		FileInfo inputGeoJsonPath,
		FileInfo outputGeoJsonPath,
		Epsg geoJsonEpsg = Epsg.Wgs84
		//int effectiveMaxDegreeOfParallelism,
		//int featureBatchSize
		) {
		var featureCollection = inputGeoJsonPath.GeoJsonDeserialize<Feature>();
		if (featureCollection == null) {
			throw new InvalidOperationException("Could not read FeatureCollection from GeoJSON.");
		}
		IFeature[] features = [featureCollection]; //.ToArray();
		var results = new long[features.Length][];
		if (features.Length == 0) {
			outputGeoJsonPath.GeoJsonSerialize(featureCollection, 0);
			return;
		}
		features.ForEach((f,i) => results[i] = gDal.GetHistogramCounts(f));

		//ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = effectiveMaxDegreeOfParallelism };
		//var partitions = Partitioner.Create(0, features.Length, featureBatchSize);
		//Parallel.ForEach(partitions, options, () => //(range, loopState, worker) => {
				//for (int index = range.Item1; index < range.Item2; index++) {
				//	results[index] = worker.GetHistogram(features[index]);
				//}
			//	return worker;
			//},
			//worker => worker.Dispose());

		for (var index = 0; index < features.Length; index++) {
			var feature = features[index];
			if (feature is null) {
				continue;
			}

			var attributes = GetOrCreateAttributes(feature);
			var counts = results[index] ?? new long[schema.BucketCount];

			SetAttribute(attributes, "hist_schema_id", schema.HistogramSchemaId);
			SetAttribute(attributes, "elev_hist_counts", counts);
		}
		outputGeoJsonPath.GeoJsonSerialize(featureCollection, 0, "");
	}

	/// <summary> Validates that the histogram schema is internally consistent. </summary> 
	[System.ComponentModel.Description("Validates that the histogram schema is internally consistent.")]
	public static string Validate(HistogramSchema schema) {
		if (schema == null) {
			throw new ArgumentNullException(nameof(schema));
		}

		if (string.IsNullOrWhiteSpace(schema.HistogramSchemaId)) {
			return "schema.HistogramSchemaId is required.";
		}

		if (double.IsNaN(schema.MinimumValue) || double.IsInfinity(schema.MinimumValue)) {
			return "schema.MinimumValueM must be finite.";
		}

		if (double.IsNaN(schema.MaximumValue) || double.IsInfinity(schema.MaximumValue)) {
			return "schema.MaximumValueM must be finite.";
		}

		if (schema.MaximumValue <= schema.MinimumValue) {
			return "schema.MaximumValueM must be greater than schema.MinimumValueM.";
		}

		if (schema.BucketCount <= 0) {
			return "schema.BucketCount must be greater than zero.";
		}

		if (schema.Bins == null || schema.Bins.Count != schema.BucketCount) {
			return "schema.Bins must contain exactly schema.BucketCount items.";
		}

		var expectedBucketWidthM = (schema.MaximumValue - schema.MinimumValue) / schema.BucketCount;
		if (Math.Abs(schema.BucketWidth - expectedBucketWidthM) > 1.0e-12) {
			return "schema.BucketWidthM is inconsistent with the min, max, and bucket count.";
		}
		return "";
	}

	/// <summary> Resolves the effective degree of parallelism for the processing run. </summary> 
	[System.ComponentModel.Description("Resolves the effective degree of parallelism for the processing run.")]
	private static int ResolveMaxDegreeOfParallelism(int maxDegreeOfParallelism) {
		if (maxDegreeOfParallelism > 0) {
			return maxDegreeOfParallelism;
		}

		var logicalProcessors = Environment.ProcessorCount;
		return Math.Max(1, Math.Min(logicalProcessors, 6));
	}

	/// <summary> Collects DEM tile paths from a directory. </summary> 
	[System.ComponentModel.Description("Collects DEM tile paths from a directory.")]
	private static string[] CollectDemTilePaths(string demDirectory, bool recursive) {
		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		var result = new List<string>();
		result.AddRange(Directory.GetFiles(demDirectory, "*.tif", searchOption));
		result.AddRange(Directory.GetFiles(demDirectory, "*.tiff", searchOption));
		result.Sort(StringComparer.OrdinalIgnoreCase);
		return result.ToArray();
	}

	/// <summary> Builds a temporary VRT using the gdalbuildvrt executable. </summary>  
	[System.ComponentModel.Description("Builds a temporary VRT using the gdalbuildvrt executable.")]
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
	[System.ComponentModel.Description("Quotes one command-line argument.")]
	private static string QuoteArgument(string value) => "\"" + value + "\"";

	/// <summary> Attempts to delete a file and suppresses deletion errors. </summary> 
	[System.ComponentModel.Description("Attempts to delete a file and suppresses deletion errors.")]
	private static void TryDeleteFile(string path) {
		try {
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) {
				File.Delete(path);
			}
		} catch (Exception x) {
			Trace.TraceError("Failed to delete File " + path + "\n" + x);
		}
	}

	/// <summary> Gets the feature attribute table or creates one if it does not exist. </summary> 
	[System.ComponentModel.Description("Gets the feature attribute table or creates one if it does not exist.")]
	private static IAttributesTable GetOrCreateAttributes(this IFeature feature) {
		if (feature == null) {
			throw new InvalidOperationException("Feature is null.");
		}
		if (feature.Attributes == null) {
			feature.Attributes = new AttributesTable();
		}
		return feature.Attributes;
	}

	/// <summary> Sets or adds one feature attribute value. </summary> 
	[System.ComponentModel.Description("Sets or adds one feature attribute value.")]
	public static void SetAttribute(this IAttributesTable attributes, string name, object value) {
		if (attributes.Exists(name)) {
			attributes[name] = value;
		} else {
			attributes.Add(name, value);
		}
	}

	/// <summary> Sets or adds one feature attribute value. </summary> 
	[System.ComponentModel.Description("Sets or adds one feature attribute value.")]
	public static object? GetAttribute(this IAttributesTable attributes, string name, object? fallBack = null)
		=> attributes.Exists(name) ? attributes[name] : fallBack;

	/// <summary> Determines whether a raster value should be treated as NoData. </summary> 
	[System.ComponentModel.Description("Determines whether a raster value should be treated as NoData.")]
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
	[System.ComponentModel.Description("Transforms a polygonal geometry into the raster coordinate reference system.")]
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
	[System.ComponentModel.Description("Transforms one polygon into the raster coordinate reference system.")]
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
	[System.ComponentModel.Description("Transforms one linear ring into the raster coordinate reference system.")]
	private static LinearRing TransformLinearRing(this LinearRing ring, CoordinateTransformation transform, GeometryFactory geometryFactory) {
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

	/// <summary> Copies the X, Y (and optionally Z) components of <paramref name="coordinate"/> into <paramref name="point"/>. </summary>
	[System.ComponentModel.Description("Copies the X, Y (and optionally Z) components of coordinate into point.")]
	public static void ToArray(this Coordinate coordinate, double[] point) {
		if (point.Length > 2)
			point[2] = coordinate.Z;
		point[1] = coordinate.Y;
		point[0] = coordinate.X;
	}

	/// <summary> Attempts to force traditional GIS axis order on a spatial reference. </summary> 
	[System.ComponentModel.Description("Attempts to force traditional GIS axis order on a spatial reference.")]
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


	/// <summary>Specifies the constant rad Per Degree.</summary>
	private const double RadPerDegree = Math.PI / 180.0;

	/// <summary> geographic ground area of one raster cell row element in rad². </summary>  
	[System.ComponentModel.Description("geographic ground area of one raster cell row element in rad².")]
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
	[System.ComponentModel.Description("Clamps a latitude value into the valid geographic range.")]
	public static double ClampLatitudeDegrees(double latitudeDegrees) => latitudeDegrees > 90.0 ? 90.0 :
		latitudeDegrees < -90.0 ? -90.0 : latitudeDegrees;

	/// <summary> Computes the area-weighted histogram for a polygon in raster coordinates. </summary>  
	[System.ComponentModel.Description("Computes the area-weighted histogram for a polygon in raster coordinates.")]
	public static double[] PolygonHistogramByArea(this Dataset dem, Band band, Geometry polygonInRasterCrs
		, double[] geoTransform, bool hasNoDataValue, double noDataValue, HistogramSchema schema, string context) {
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

		var locator = new IndexedPointInAreaLocator(polygonInRasterCrs);
		var histogramMinM = schema.MinimumValue;
		var histogramMaxM = schema.MaximumValue;
		var bucketWidthM = schema.BucketWidth;
		var bucketCount = schema.BucketCount;
		var maxPageHeight = Math.Max(1, 88_388_608 / windowWidth); //stay below 64 MB

		for (var pageStart = 0; pageStart < windowHeight; pageStart += maxPageHeight) {
			var message = DateTime.Now + ": " + pageStart + " of " + windowHeight + " for " + context;
			Trace.WriteLine(message);
			Debug.WriteLine(message);
			Console.WriteLine(message);
			var pageHeight = Math.Min(maxPageHeight, windowHeight - pageStart);
			var values = new double[windowWidth * pageHeight];
			//lock (GDalContext.GdalLock)
				band.ReadRaster(xOff, yOff + pageStart, windowWidth, pageHeight, values, windowWidth, pageHeight, 0, 0);

			for (var row = 0; row < pageHeight; row++) {
				var absoluteRowIndex = yOff + pageStart + row;
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
		}
		return areas;
	}

	/// <summary> Computes one compact histogram-count array for a polygon in raster coordinates. </summary> 
	[System.ComponentModel.Description("Computes one compact histogram-count array for a polygon in raster coordinates.")]
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

		if (windowWidth <= 0 ||
			windowHeight <= 0) {
			return counts;
		}

		var locator = new IndexedPointInAreaLocator(polygonInRasterCrs);
		var bucketWidthM = schema.BucketWidth;
		var histogramMinM = schema.MinimumValue;
		var histogramMaxM = schema.MaximumValue;
		var bucketCount = schema.BucketCount;
		var maxPageHeight = Math.Max(1, 168_435_456 / windowWidth);

		for (var pageStart = 0; pageStart < windowHeight; pageStart += maxPageHeight) {
			var pageHeight = Math.Min(maxPageHeight, windowHeight - pageStart);
			var elevations = new double[windowWidth * pageHeight];
			//lock (GDalContext.GdalLock)
				band.ReadRaster(xOff, yOff + pageStart, windowWidth, pageHeight, elevations, windowWidth, pageHeight, 0, 0);

			for (var row = 0; row < pageHeight; row++) {
				var centerY = geoTransform[3] + ((yOff + pageStart + row + 0.5) * geoTransform[5]);
				var rowStart = row * windowWidth;

				for (var col = 0; col < windowWidth; col++) {
					var elevation = elevations[rowStart + col];
					if (IsNoData(elevation, hasNoDataValue, noDataValue)) {
						continue;
					}

					var centerX = geoTransform[0] + ((xOff + col + 0.5) * geoTransform[1]);
					var location = locator.Locate(new Coordinate(centerX, centerY));
					if (location == Location.Exterior) {
						continue;
					}

					int bucketIndex;
					if (elevation < histogramMinM) {
						bucketIndex = 0;
					} else if (elevation >= histogramMaxM) {
						bucketIndex = bucketCount - 1;
					} else {
						bucketIndex = (int) Math.Floor((elevation - histogramMinM) / bucketWidthM);
					}
					counts[bucketIndex]++;
				}
			}
		}
		return counts;
	}

	/// <summary> Writes the <paramref name="schema"/> to the <paramref name="csvPath"/> </summary>
	[System.ComponentModel.Description("Writes the schema to the csvPath")]
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
					Csv.Escaped(schema.HistogramSchemaId) + "," +
					bin.BinIndex.ToString(CultureInfo.InvariantCulture) + "," +
					bin.MinimumValue.ToString("0.###############", CultureInfo.InvariantCulture) + "," +
					bin.MaximumValue.ToString("0.###############", CultureInfo.InvariantCulture) + "," +
					Csv.Escaped(bin.IntervalNotation) + "," +
					Csv.Escaped(bin.Label));
			}
		}
	}

	/// <summary> Writes the <paramref name="schema"/> to the <paramref name="jsonPath"/> </summary>
	[System.ComponentModel.Description("Writes the schema to the jsonPath")]
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
}

