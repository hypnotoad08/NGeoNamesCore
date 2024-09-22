using NGeoNames;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DumpTester
{
	internal class Program
	{
		private static readonly string Dump_DownloadDirectory = ConfigurationManager.AppSettings["dump_downloaddirectory"];
		private static readonly string Postal_DownloadDirectory = ConfigurationManager.AppSettings["postal_downloaddirectory"];

		private static async Task Main(string[] args)
		{
			// Test GeoName dumps
			var dumpdownloader = GeoFileDownloader.CreateGeoFileDownloader();
			var dumpfiles = GetDumps(dumpdownloader);

			await Task.WhenAll(dumpfiles.Select(async g =>
			{
				Console.WriteLine($"Download: {g.Filename}");
				await dumpdownloader.DownloadFileAsync(g.Filename, Dump_DownloadDirectory);
				Console.WriteLine($"Testing {g.Filename}: {await g.Test(Path.Combine(Dump_DownloadDirectory, g.Filename))}");
			}));

			// Test Postalcode dumps
			var postalcodedownloader = GeoFileDownloader.CreatePostalcodeDownloader();
			var postalcodefiles = await GetCountryPostalcodesAsync(postalcodedownloader);

			await Task.WhenAll(postalcodefiles.Select(async g =>
			{
				Console.WriteLine($"Download: {g.Filename}");
				await postalcodedownloader.DownloadFileAsync(g.Filename, Postal_DownloadDirectory);
				Console.WriteLine($"Testing {g.Filename}: {await g.Test(Path.Combine(Postal_DownloadDirectory, g.Filename))}");
			}));

			Console.WriteLine("Testing ASCII fields");
			await DumpASCIILiesAsync(Dump_DownloadDirectory);

			Console.WriteLine("All done!");
		}

		private static async Task<GeoFile[]> GetCountryPostalcodesAsync(GeoFileDownloader downloader)
		{
			using var client = new HttpClient();
			var document = await client.GetStringAsync(downloader.BaseUri);

			var countries = new Regex("href=\"([A-Z]{2}.zip)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile
				{
					Filename = m.Groups[1].Value,
					Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadPostalcodesAsync(fn).ToListAsync()).Count())
				});

			return new[] {
				new GeoFile { Filename = "allCountries.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadPostalcodesAsync(fn).ToListAsync()).Count()) }
			}.Union(countries.OrderBy(m => m.Filename)).ToArray();
		}

		private static GeoFile[] GetDumps(GeoFileDownloader downloader)
		{
			return new[] {
				new GeoFile { Filename = "admin1CodesASCII.txt", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadAdmin1CodesAsync(File.OpenRead(fn)).ToListAsync()).Count()) },
				new GeoFile { Filename = "admin2Codes.txt", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadAdmin2CodesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "allCountries.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "alternateNames.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadAlternateNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "alternateNamesV2.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadAlternateNamesV2Async(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "cities1000.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "cities15000.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "cities5000.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "countryInfo.txt", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadCountryInfoAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "hierarchy.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadHierarchyAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "iso-languagecodes.txt", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadISOLanguageCodesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "no-country.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "timeZones.txt", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadTimeZonesAsync(fn).ToListAsync()).Count()) },
				new GeoFile { Filename = "userTags.zip", Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadUserTagsAsync(fn).ToListAsync()).Count()) }
			}.Union(GetCountryDumps(downloader).Result).ToArray();
		}

		private static async Task<GeoFile[]> GetCountryDumps(GeoFileDownloader downloader)
		{
			using var client = new HttpClient();
			var document = await client.GetStringAsync(downloader.BaseUri);

			var countries = new Regex("href=\"([A-Z]{2}.zip)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile { Filename = m.Groups[1].Value, Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadExtendedGeoNamesAsync(fn).ToListAsync()).Count()) });

			var featurecodes = new Regex("href=\"(featureCodes_[A-Z]{2}.txt)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile { Filename = m.Groups[1].Value, Test = async (f) => await ExecuteTestAsync(f, async (fn) => (await GeoFileReader.ReadFeatureCodesAsync(fn).ToListAsync()).Count()) });

			return countries.Union(featurecodes).OrderBy(m => m.Filename).ToArray();
		}

		private static async Task<string> ExecuteTestAsync(string filename, Func<string, Task<int>> test)
		{
			try
			{
				// Hack: Replace .zip with .txt as file content is extracted
				var file = filename.Replace(".zip", ".txt");
				return $"{await test(file)} records OK";
			}
			catch (Exception ex)
			{
				return $"FAILED: {ex.Message}";
			}
		}

		private static async Task DumpASCIILiesAsync(string logpath)
		{
			await Task.Run(() =>
			{
				var nonasciifilter = new Regex("[^\x20-\x7F]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				var geofilefilter = new Regex("^[A-Z]{2}.txt$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

				using var lw = new StreamWriter(Path.Combine(logpath, "_asciilies.log"));
				lw.WriteLine("The following files contain entries that claim to contain ASCII only but contain non-ASCII data anyways:");

				var extgeofiles = new[] { "allCountries", "cities1000", "cities5000", "cities15000", "no-country" }
					.Select(f => Path.Combine(Dump_DownloadDirectory, f + ".txt"))
					.Union(Directory.GetFiles(Dump_DownloadDirectory, "*.txt")
					.Where(f => geofilefilter.IsMatch(Path.GetFileName(f))));

				var lies = extgeofiles.AsParallel()
					.SelectMany(f => GeoFileReader.ReadExtendedGeoNamesAsync(f).ToListAsync().Result
						.Where(e => nonasciifilter.IsMatch(e.NameASCII))
						.Select(i => new NonASCIIEntry { FileName = f, Id = i.Id, Value = i.NameASCII })
					).Union(
					GeoFileReader.ReadAdmin1CodesAsync(File.OpenRead(Path.Combine(Dump_DownloadDirectory, "admin1CodesASCII.txt"))).ToListAsync().Result.AsParallel()
							.Where(c => nonasciifilter.IsMatch(c.NameASCII))
							.Select(i => new NonASCIIEntry { FileName = "admin1CodesASCII.txt", Id = i.GeoNameId, Value = i.NameASCII })
					).Union(
						GeoFileReader.ReadAdmin2CodesAsync(Path.Combine(Dump_DownloadDirectory, "admin2Codes.txt")).ToListAsync().Result.AsParallel()
							.Where(c => nonasciifilter.IsMatch(c.NameASCII))
							.Select(i => new NonASCIIEntry { FileName = "admin2Codes.txt", Id = i.GeoNameId, Value = i.NameASCII })
					);

				foreach (var l in lies.OrderBy(l => l.FileName).ThenBy(l => l.Value))
				{
					lw.WriteLine($"{Path.GetFileName(l.FileName)}\t{l.Id}\t{l.Value}");
				}
			});
		}
	}

	internal class GeoFile
	{
		public string Filename { get; set; }
		public Func<string, Task<string>> Test { get; set; }

		public GeoFile()
		{
			this.Test = (f) => Task.FromResult("Not Implemented");
		}
	}

	internal class NonASCIIEntry
	{
		public string FileName { get; set; }
		public string Value { get; set; }
		public int Id { get; set; }
	}
}