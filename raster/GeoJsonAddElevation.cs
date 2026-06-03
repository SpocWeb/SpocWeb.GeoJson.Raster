using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary>Adds elevation (Z) coordinates to every geometry in a GeoJSON file,<br/>
/// reading height values from a GDAL raster model such as a Copernicus DEM VRT.</summary>
/// <remarks>
/// <see cref="StreamingGeoJsonProcessor"/>
///
/// ## Meta
/// pass: 2
/// mtime: 2026-05-16T07:12:22Z
/// digest: 0f6a243ef484d835e4f85a9818515c4f8d14061e91e82aff94546b2b55fa4684
/// updated: 2026-05-19
/// </remarks>
public static class GeoJsonAddElevation {
	/// <summary>Specifies the constant geo Json Extension.</summary>
	public const string GeoJsonExtension = ".geoJson";
	/// <summary>Specifies the constant geo Json Pattern.</summary>
	public const string GeoJsonPattern = "*" + GeoJsonExtension;

	public static JsonSerializerSettings SerializerSettings = new() {
		Formatting = Formatting.Indented,
	};

	/// <summary> Adds elevation data as Z coordinates to all GeoJSON files in the <paramref name="geoJsonDirectory"/>
	/// and its subdirectories, using the <paramref name="vrtElevationFile"/> model.
	/// </summary>
	/// <remarks>Each GeoJSON file is updated in place by adding elevation values as Z coordinates,
	/// replacing the original file.
	/// </remarks>
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\SpocWeb\_Standards\Earth\Continent\")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Africa\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\_Standards.Asia\Earth\Continent")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\Obsidian.SpocWeb\_Standards\Earth\Continent")]
	public static void AddElevationAsZ(string vrtElevationFile, string geoJsonDirectory) {//, int parallelism = 8) {
		using var elevationModel = new GDalContext(vrtElevationFile, new HistogramSchema());
		var dir = new DirectoryInfo(geoJsonDirectory);
		foreach (var geoJsonFile in dir.EnumerateFiles(GeoJsonPattern, SearchOption.AllDirectories)) {
			Trace.WriteLine(geoJsonFile.FullName);
			Debug.WriteLine(geoJsonFile.FullName);
			Console.WriteLine(geoJsonFile.FullName);
			var outputPath = geoJsonFile.FullName
				.Substring(0, geoJsonFile.FullName.Length - GeoJsonExtension.Length) + "Z" + GeoJsonExtension;
			try {
				elevationModel.AddElevationAsZ(geoJsonFile, outputPath); //ca. 46000 geojson Files down to province Level
				geoJsonFile.Delete();
				File.Move(outputPath, geoJsonFile.FullName);
			} catch (Exception x) {
				Debug.WriteLine(geoJsonFile.FullName + x);
				Trace.TraceError(geoJsonFile.FullName + x);
				Console.Error.WriteLine(geoJsonFile.FullName + x);
			}
		}
	}

	/// <inheritdoc cref="AddElevationAsZ(string, string)"/>
	public static void AddElevationAsZ(this GDalContext elevationModel, FileInfo inputPath, string outputPath) {//y, int parallelism = 80) {
		//var options = new ParallelOptions { MaxDegreeOfParallelism = parallelism };

		using var reader = new StreamReader(inputPath.FullName);
		using var writer = new StreamWriter(outputPath);

		var root = JObject.Parse(reader.ReadToEnd());
		var rootType = (string)root["type"];

		switch (rootType) {
			case nameof(FeatureCollection): {
				var features = (JArray)root["features"];
				writer.Write("{\"type\":\"FeatureCollection\",\"features\":\n[");
				bool first = true;
				features.ForEach(feature => {
					var serialized = SerializeElevatedFeature(elevationModel, feature);
					lock (writer) {
						if (!first) {
							writer.Write(",\n");
						}
						writer.Write(serialized);
						first = false;
					}
				});
				writer.Write("]}");
				break;
			}
			case nameof(Feature): {
				writer.Write(SerializeElevatedFeature(elevationModel, root));
				break;
			}
			default: {
				var geom = GeoJsonDeserialize<Geometry>(root.ToString(Formatting.None));
				var geomZ = elevationModel.AddElevationAsZ(geom);
				writer.Write(geomZ.GeoJsonSerialize());
				break;
			}
		}
	}

	/// <summary> Adds elevation Z to the geometry of <paramref name="feature"/> and returns the serialized GeoJSON Feature string. </summary>
	static string SerializeElevatedFeature(GDalContext elevationModel, JToken feature) {
		var geometryElement = feature["geometry"];
		var geomZ = geometryElement == null || geometryElement.Type == JTokenType.Null
			? null
			: elevationModel.AddElevationAsZ(GeoJsonDeserialize<Geometry>(geometryElement.ToString(Formatting.None)));
		var geomJson = geomZ == null ? "null" : geomZ.GeoJsonSerialize();
		geomJson = geomJson.Replace("]],[[", "]],\n[[");
		var propertiesJson = feature["properties"]?.ToString(Formatting.None) ?? "null";
		return $"{{\"type\":\"Feature\",\"properties\":{propertiesJson},\n\"geometry\":{geomJson}}}";
	}

	/// <summary>Gets the geo Json Serializer3 D.</summary>
	public static readonly JsonSerializer GeoJsonSerializer3D
		= GeoJsonSerializer.Create(SerializerSettings, GeometryZ.GeometryFactory, 3);

	/// <summary> Deserializes a GeoJSON string into a <typeparamref name="T"/> instance using <see cref="GeoJsonSerializer3D"/>. </summary>
	public static T GeoJsonDeserialize<T>(string json) {
		using TextReader sr = new StringReader(json);
		return GeoJsonDeserialize<T>(sr);
	}

	/// <inheritdoc cref="GeoJsonDeserialize(string)"/>
	public static T GeoJsonDeserialize<T>(this FileInfo geoJsonPath) {
		using var sr = new StreamReader(geoJsonPath.FullName);
		return GeoJsonDeserialize<T>(sr);
	}

	/// <inheritdoc cref="GeoJsonDeserialize(string)"/>
	public static T GeoJsonDeserialize<T>(this TextReader sr) {
		using var jr = new JsonTextReader(sr);
		return GeoJsonDeserialize<T>(jr);
	}

	/// <inheritdoc cref="GeoJsonDeserialize(string)"/>
	public static T GeoJsonDeserialize<T>(this JsonTextReader jr) => GeoJsonSerializer3D.Deserialize<T>(jr)!;

	/// <summary> Serializes <paramref name="geoJson"/> to an indented GeoJSON string using <see cref="GeoJsonSerializer3D"/>. </summary>
	public static string GeoJsonSerialize(this Geometry geoJson, int indentation = 2, string newLine = "\n")
		=> new StringBuilder().GeoJsonSerialize(geoJson, indentation, newLine).ToString();

	/// <inheritdoc cref="GeoJsonSerialize(Geometry, int, string)"/>
	public static StringBuilder GeoJsonSerialize(this StringBuilder sb, object geoJson, int indentation = 2, string newLine = "\n") {
		using var writer = sb.CreateJsonWriter(indentation, newLine);
		GeoJsonSerializer3D.Serialize(writer, geoJson);
		return sb;
	}

	/// <inheritdoc cref="GeoJsonSerialize(Geometry, int, string)"/>
	public static void GeoJsonSerialize(this FileInfo geoJsonPath, object geoJson, int indentation = 2, string newLine = "\n") {
		using var sw = new StreamWriter(geoJsonPath.FullName) { NewLine = newLine };
		GeoJsonSerialize(sw, geoJson, indentation);
	}

	/// <inheritdoc cref="GeoJsonSerialize(Geometry, int, string)"/>
	public static void GeoJsonSerialize(this TextWriter writer, object geoJson, int indentation = 2) {
		using var jsonWriter = writer.CreateJsonWriter(indentation);
		GeoJsonSerializer3D.Serialize(jsonWriter, geoJson);
	}

	/// <inheritdoc cref="CreateJsonWriter(TextWriter, int)"/>
	public static JsonTextWriter CreateJsonWriter(this StringBuilder sb, int indentation = 2, string newLine = "\n")
		=> new StringWriter(sb) { NewLine = newLine, }.CreateJsonWriter(indentation);


	/// <summary> Creates an indented <see cref="JsonTextWriter"/> wrapping <paramref name="sb"/>. </summary>
	public static JsonTextWriter CreateJsonWriter(this TextWriter sb, int indentation = 2)
		=> new JsonTextWriter(sb) {
			Formatting = Formatting.Indented,
			Indentation = indentation
		};
}
