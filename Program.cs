using System.Xml.Linq;
using System.Xml.Serialization;
using Geolocation;

namespace GPXDistCheck
{
	internal class Program
	{
		public struct Point
		{
			public string Name;
			public int ID;
			public Coordinate Coordinate;
		}

		static double DistanceLineToPoint(Coordinate pnt, Coordinate ln1, Coordinate ln2)
		{
			//https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
			Coordinate center = new Coordinate((pnt.Latitude+ln1.Latitude+ln2.Latitude)/3,(pnt.Longitude+ln1.Longitude+ln2.Longitude)/3);
			//Calc ellipsoid for converting coordinates to meters
			double xm = center.Latitude * Math.PI / 180.0;
			double klat = (111.13209 - 0.56605 * Math.Cos(2 * xm) + 0.00120 * Math.Cos(4 * xm));
			double klon = (111.41513 * Math.Cos(xm) - 0.09455 * Math.Cos(3 * xm) + 0.00012 * Math.Cos(5 * xm));
			//recalc all points coord to the center val
			Coordinate nln1 = new Coordinate((ln1.Latitude - pnt.Latitude), (ln1.Longitude - pnt.Longitude));
			Coordinate nln2 = new Coordinate((ln2.Latitude - pnt.Latitude), (ln2.Longitude - pnt.Longitude));
			//double dist = ((nln2.Longitude - nln1.Longitude)*npnt.Latitude-(nln2.Latitude- nln1.Latitude)*npnt.Longitude+nln2.Latitude*nln1.Longitude-nln2.Longitude*nln1.Latitude)/
			double dist = klat*klon*(nln1.Latitude*nln2.Longitude-nln1.Longitude*nln2.Latitude)/
				Math.Sqrt(klat * klat * (nln2.Longitude-nln1.Longitude)* (nln2.Longitude - nln1.Longitude)+ klon * klon * (nln2.Latitude-nln1.Latitude)* (nln2.Latitude - nln1.Latitude));
			return Math.Abs(dist);
		}

		static double DistancePointToPoint(Coordinate p1, Coordinate p2)
		{
			//http://en.wikipedia.org/wiki/Geographical_distance
			Coordinate center = new Coordinate((p1.Latitude + p1.Latitude) / 2, (p1.Longitude + p2.Longitude) / 2);
			//Calc ellipsoid for converting coordinates to meters
			double xm = center.Latitude * Math.PI / 180.0;
			double klat = (111.13209 - 0.56605 * Math.Cos(2 * xm) + 0.00120 * Math.Cos(4 * xm));
			double klon = (111.41513 * Math.Cos(xm) - 0.09455 * Math.Cos(3 * xm) + 0.00012 * Math.Cos(5 * xm));
			//recalc all points coord to the center val
			Coordinate np1 = new Coordinate(p1.Latitude - center.Latitude, p2.Longitude - center.Longitude);
			Coordinate np2 = new Coordinate(p2.Latitude - center.Latitude, p1.Longitude - center.Longitude);
			double dist = Math.Sqrt(klat*klat*(np1.Latitude-np2.Latitude)* (np1.Latitude - np2.Latitude)+klon*klon*(np1.Longitude-np2.Longitude)* (np1.Longitude - np2.Longitude));
			return Math.Abs(dist);
		}
		static void Main(string[] args)
		{
			//Open WayPoint file - first argument
			gpx.gpxType tr;
			XmlSerializer xmls = new(typeof(gpx.gpxType));
			tr = (gpx.gpxType)xmls.Deserialize(File.OpenRead(args[0]));
			List<Point> wpts = new List<Point>();
			//Create array of aim points
			foreach(var i in tr.wpt)
			{
				if (int.TryParse(new String(i.name.Where(Char.IsDigit).ToArray()), out int id))
				{
					wpts.Add(new Point
					{
						Coordinate = new Coordinate() { Latitude = (double)i.lat, Longitude = (double)i.lon },
						Name = i.name,
						ID = id
					});
				}
			}
			//List of sorted points
			wpts = wpts.OrderBy(a=>a.ID).ToList();
		
			//Open track
			tr = (gpx.gpxType)xmls.Deserialize(File.OpenRead(args[1]));
			foreach (var i in tr.trk)
			{
				foreach(var j in i.trkseg)
				{
					double? dist = null;
					Coordinate? oldwp = null;
					foreach (var k in j.trkpt)
					{
						Coordinate wp = new Coordinate((double)k.lat, (double)k.lon);

						double ndist = DistancePointToPoint(wp, wpts[0].Coordinate);
						if (oldwp != null)
						{
							if (dist.HasValue && ndist >= dist)
							{
								double nDist = DistanceLineToPoint(wpts[0].Coordinate, oldwp.Value, wp);
							}
						}
						oldwp = wp;
						dist = ndist;
					}
				}
			}
		}
	}
}
