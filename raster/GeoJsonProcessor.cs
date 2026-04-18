using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary>
/// 
/// </summary>
/// <see cref="StreamingGeoJsonProcessor"/>
public static class GeoJsonProcessor {
	public const string GeoJsonExtension = ".geoJson";
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
				var geom = ReadGeomJson(root.ToString(Formatting.None));
				var geomZ = elevationModel.AddElevationAsZ(geom);
				writer.Write(geomZ.WriteGeomJson());
				break;
			}
		}
	}

	static string SerializeElevatedFeature(GDalContext elevationModel, JToken feature) {
		var geometryElement = feature["geometry"];
		var geomZ = geometryElement == null || geometryElement.Type == JTokenType.Null
			? null
			: elevationModel.AddElevationAsZ(ReadGeomJson(geometryElement.ToString(Formatting.None)));
		var geomJson = geomZ == null ? "null" : geomZ.WriteGeomJson();
		geomJson = geomJson.Replace("]],[[", "]],\n[[");
		var propertiesJson = feature["properties"]?.ToString(Formatting.None) ?? "null";
		return $"{{\"type\":\"Feature\",\"properties\":{propertiesJson},\n\"geometry\":{geomJson}}}";
	}

	static readonly JsonSerializer GeoJsonSerializer3D =
		GeoJsonSerializer.Create(SerializerSettings, GeometryZ.GeometryFactory, 3);

	public static Geometry ReadGeomJson(string json) {
		using var sr = new StringReader(json);
		using var jr = new JsonTextReader(sr);
		return GeoJsonSerializer3D.Deserialize<Geometry>(jr)!;
	}

	public static string WriteGeomJson(this Geometry geom, int indentation = 0, string newLine = "\n") {
		var sb = new StringBuilder();
		var writer = sb.CreateJsonWriter(indentation, newLine);
		GeoJsonSerializer3D.Serialize(writer, geom);
		return sb.ToString();
	}

	public static JsonTextWriter CreateJsonWriter(this StringBuilder sb, int indentation = 0, string newLine = "\n")
		=> new JsonTextWriter(new StringWriter(sb) { NewLine = "", }) {
			Formatting = Formatting.Indented,
			Indentation = indentation
		};
}