using OSGeo.GDAL;

namespace org.SpocWeb.root.files.Tests.raster;

/// <summary> Keeps the <see cref="_Dataset"/> open for high Performance</summary>
public sealed class ElevationModelSampler : IDisposable {

	private readonly Dataset _Dataset;
	private readonly Band _band;
	private readonly double[] _geoTransformMatrix = new double[6];

	static ElevationModelSampler() {
		InitGdal();
	}

	public static void InitGdal() {
		Gdal.SetConfigOption("GDAL_DATA", @"C:\OSGeo4W64\share\gdal");
		Gdal.SetConfigOption("PROJ_LIB", @"C:\OSGeo4W64\share\proj");
		Gdal.AllRegister();
	}

	public ElevationModelSampler(string vrtPath) {
		_Dataset = Gdal.Open(vrtPath, Access.GA_ReadOnly);
		_band = _Dataset.GetRasterBand(1);
		_Dataset.GetGeoTransform(_geoTransformMatrix);
	}

	public double Sample(double lon, double lat) {
		var px = (lon - _geoTransformMatrix[0]) / _geoTransformMatrix[1];
		var py = (lat - _geoTransformMatrix[3]) / _geoTransformMatrix[5];

		var x = (int) Math.Round(px);
		var y = (int) Math.Round(py);
		try {
			var buffer = new double[1];
			_band.ReadRaster(x, y, 1, 1, buffer, 1, 1, 0, 0);
			return buffer[0];
		} catch (Exception e) {
			Trace.TraceError(e + "");
			Console.Error.WriteLine(e);
			return 1;
		}
	}

	public void Dispose() {
		_band.Dispose();
		_Dataset.Dispose();
	}
}
