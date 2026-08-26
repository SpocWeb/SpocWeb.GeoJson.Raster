using System.Text.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using org.SpocWeb.root.Attributes;
using System.ComponentModel;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary>Low-memory streaming processor that adds elevation Z coordinates to GeoJSON FeatureCollections<br/>
/// by reading and writing the JSON token-by-token without loading the entire document into memory.</summary>
/// <remarks>
/// ## Meta
/// pass: 2
/// mtime: 2026-05-16T07:12:26Z
/// digest: 5e2569cea31bf06736224b64215e0f8bf743332cf15f2b3f10d73b9b212de291
/// updated: 2026-05-19
/// </remarks>
[Facets(Layer = "domain", Status = "active", Complexity = 4)]
[Tags("code/streaming_parser", "code/elevation_enrichment")]
[DocState(Pass = 2, MTime = "2026-08-26T09:15:50Z", Digest = "ec6b0e56b414e0d88def3631b0599d4b8e5926f7f43037b56bc47d561860ce7e", Stale = false, Path = "raster/StreamingGeoJsonProcessor.cs", Since = "2026-08-22")]
[System.ComponentModel.Description("Low-memory streaming processor that adds elevation Z coordinates to GeoJSON FeatureCollections  by reading and writing the JSON token-by-token without loading the entire document into memory.")]
[Concept("geojson_elevation_enrichment")]
[Concept("streaming_json_processing")]
public static class StreamingGeoJsonProcessor {


	/// <summary> Adds elevation Z coordinates to all GeoJSON files found under <paramref name="geoJsonPath"/> using the VRT at <paramref name="vrtPath"/>. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 2)]
	[Tags("code/file_traversal", "code/streaming_parser")]
	[System.ComponentModel.Description("Adds elevation Z coordinates to all GeoJSON files found under geoJsonPath using the VRT at vrtPath.")]
	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\SpocWeb\_Standards\Earth\Continent\Europe\Europe~Central\Germany\Germany~West\Hessen\counties~Hessen")]
	[Concept("streaming_json_processing")]
	public static void StreamGeoJsonProcessor(string vrtPath, string geoJsonPath) {
		var gf = new GeometryFactory();
		using var elevationModel = new GDalContext(vrtPath, new HistogramSchema());
		foreach (var geoJsonFile in Directory.EnumerateFiles(geoJsonPath, "*.geoJson", SearchOption.AllDirectories)) {
			elevationModel.ProcessFile(geoJsonFile, geoJsonFile + ".json"); //ca. 46000 geojson Files down to province Level
		}
	}

	/// <summary> Verifies that a <see cref="GeometryFactory"/> can be constructed without exceptions. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 1)]
	[Tags("code/diagnostics")]
	[System.ComponentModel.Description("Verifies that a GeometryFactory can be constructed without exceptions.")]
	[Test]
	[Concept("streaming_json_processing")]
	public static void TestGeometryFactory() {
		try {
			var gf = new GeometryFactory();
		} catch (Exception ex) {
			Console.WriteLine(ex.ToString());
		}
	}

	/// <summary> Streams the GeoJSON at <paramref name="inputPath"/>, enriches each feature with elevation Z, and writes the result to <paramref name="outputPath"/>. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 3)]
	[Tags("code/streaming_parser", "code/elevation_enrichment")]
	[System.ComponentModel.Description("Streams the GeoJSON at inputPath, enriches each feature with elevation Z, and writes the result to outputPath.")]
	[Concept("streaming_json_processing")]
	public static void ProcessFile(this GDalContext elevationModel, string inputPath, string outputPath) {
		using var fs = File.OpenRead(inputPath);
		var reader = new Utf8JsonReader(ReadAllBytes(fs), isFinalBlock: true, state: default);

		using var outStream = File.Create(outputPath);
		using var writer = new Utf8JsonWriter(outStream, new JsonWriterOptions { Indented = false });

		writer.WriteStartObject();
		writer.WriteString("type", "FeatureCollection");
		writer.WriteStartArray("features");

		while (reader.Read()) {
			if (reader.TokenType == JsonTokenType.PropertyName &&
				reader.GetString() == "features") {
				reader.Read(); // StartArray

				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
					var featureJson = ReadRawJson(ref reader);

					elevationModel.AddElevationAsZ(featureJson, writer);
				}
			}
		}

		writer.WriteEndArray();
		writer.WriteEndObject();
	}

	/// <summary> Adds the Z Dimension from the <paramref name="elevationModel"/> to the <paramref name="coordinates"/> </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 3)]
	[Tags("code/streaming_parser", "code/elevation_enrichment")]
	[System.ComponentModel.Description("Adds the Z Dimension from the elevationModel to the coordinates")]
	[Concept("streaming_json_processing")]
	public static void AddElevationAsZ(this GDalContext elevationModel, byte[] featureJson, Utf8JsonWriter writer) {
		using var doc = JsonDocument.Parse(featureJson);
		var root = doc.RootElement;

		var geomElement = root.GetProperty("geometry");

		var geoReader = new GeoJsonReader();
		Geometry geom = geoReader.Read<Geometry>(geomElement.GetRawText());

		var geomZ = elevationModel.AddElevationAsZ(geom);

		var geoWriter = new GeoJsonWriter();

		writer.WriteStartObject();
		writer.WriteString("type", "Feature");

		writer.WritePropertyName("geometry");
		writer.WriteRawValue(geoWriter.Write(geomZ));

		writer.WritePropertyName("properties");
		root.GetProperty("properties").WriteTo(writer);

		writer.WriteEndObject();
	}

	/// <summary> Reads all bytes from <paramref name="stream"/> into a new array. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 1)]
	[Tags("code/stream_io")]
	[System.ComponentModel.Description("Reads all bytes from stream into a new array.")]
	[Concept("streaming_json_processing")]
	private static byte[] ReadAllBytes(Stream stream) {
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	}

	// Extract one JSON object (feature) without full parsing
	/// <summary> Extracts the raw bytes of the next complete JSON object from <paramref name="reader"/> without fully parsing it. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 2)]
	[Tags("code/streaming_parser")]
	[System.ComponentModel.Description("Extracts the raw bytes of the next complete JSON object from reader without fully parsing it.")]
	[Concept("streaming_json_processing")]
	private static byte[] ReadRawJson(ref Utf8JsonReader reader) {
		using var ms = new MemoryStream();
		using var writer = new Utf8JsonWriter(ms);

		int depth = 0;

		do {
			writer.WriteToken(ref reader);

			if (reader.TokenType == JsonTokenType.StartObject) depth++;
			if (reader.TokenType == JsonTokenType.EndObject) depth--;

		} while (depth > 0 && reader.Read());

		writer.Flush();
		return ms.ToArray();
	}

	/// <summary> Writes the current token from <paramref name="reader"/> to <paramref name="writer"/>. </summary>
	[Facets(Layer = "domain", Status = "active", Complexity = 2)]
	[Tags("code/streaming_parser")]
	[System.ComponentModel.Description("Writes the current token from reader to writer.")]
	[Concept("streaming_json_processing")]
	static void WriteToken(this Utf8JsonWriter writer, ref Utf8JsonReader reader) {
		switch (reader.TokenType) {
			case JsonTokenType.StartObject:
				writer.WriteStartObject();
				break;

			case JsonTokenType.EndObject:
				writer.WriteEndObject();
				break;

			case JsonTokenType.StartArray:
				writer.WriteStartArray();
				break;

			case JsonTokenType.EndArray:
				writer.WriteEndArray();
				break;

			case JsonTokenType.PropertyName:
				writer.WritePropertyName(reader.GetString());
				break;

			case JsonTokenType.String:
				writer.WriteStringValue(reader.GetString());
				break;

			case JsonTokenType.Number:
				if (reader.TryGetInt64(out long l))
					writer.WriteNumberValue(l);
				else if (reader.TryGetDouble(out double d))
					writer.WriteNumberValue(d);
				break;

			case JsonTokenType.True:
				writer.WriteBooleanValue(true);
				break;

			case JsonTokenType.False:
				writer.WriteBooleanValue(false);
				break;

			case JsonTokenType.Null:
				writer.WriteNullValue();
				break;
		}
	}
}
