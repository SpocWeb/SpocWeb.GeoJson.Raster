using System.Text.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace org.SpocWeb.root.files.Tests.raster;

public static class StreamingGeoJsonProcessor {


	[TestCase(@"D:\Copernicus_DSM\global_dem.vrt", @"D:\_Obsidian\SpocWeb\_Standards\Earth\Continent\Europe\Europe~Central\Germany\Germany~West\Hessen\counties~Hessen")]
	public static void StreamGeoJsonProcessor(string vrtPath, string geoJsonPath) {
		var gf = new GeometryFactory();
		using var elevationModel = new GDalContext(vrtPath, new HistogramSchema());
		foreach (var geoJsonFile in Directory.EnumerateFiles(geoJsonPath, "*.geoJson", SearchOption.AllDirectories)) {
			elevationModel.ProcessFile(geoJsonFile, geoJsonFile + ".json"); //ca. 46000 geojson Files down to province Level
		}
	}

	[Test]
	public static void TestGeometryFactory() {
		try {
			var gf = new GeometryFactory();
		} catch (Exception ex) {
			Console.WriteLine(ex.ToString());
		}
	}

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

	private static byte[] ReadAllBytes(Stream stream) {
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	}

	// Extract one JSON object (feature) without full parsing
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
