﻿using NGeoNames;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DumpTester
{
	/// <summary>
	/// Quick'n'Dirty program to download all dumpfiles and iterate over all of them to ensure parsing works for
	/// all files. Use at own risk!
	/// </summary>
	/// <remarks>
	/// This code is full of hacks / shortcuts / sloppy work; it's NOT intended to be used in production. This project
	/// is a simple "brute force" approach to test all of the files (by forcing the parsers to parse each record) made
	/// available by geonames.org.
	/// </remarks>
	internal class Program
	{
		private static string Dump_DownloadDirectory = ConfigurationManager.AppSettings["dump_downloaddirectory"];
		private static string Postal_DownloadDirectory = ConfigurationManager.AppSettings["postal_downloaddirectory"];

		private static void Main(string[] args)
		{
			//Test GeoName dumps
			var dumpdownloader = GeoFileDownloader.CreateGeoFileDownloader();
			var dumpfiles = GetDumps(dumpdownloader);

			dumpfiles.AsParallel().ForAll(g =>
			{
				Console.WriteLine("Download: {0}", g.Filename);
				dumpdownloader.DownloadFile(g.Filename, Dump_DownloadDirectory);
				Console.WriteLine("Testing {0}: {1}", g.Filename, g.Test(Path.Combine(Dump_DownloadDirectory, g.Filename)));
			});

			//Test Postalcode dumps
			var postalcodedownloader = GeoFileDownloader.CreatePostalcodeDownloader();
			var postalcodefiles = GetCountryPostalcodes(postalcodedownloader).Result;

			postalcodefiles.AsParallel().ForAll(g =>
			{
				Console.WriteLine("Download: {0}", g.Filename);
				postalcodedownloader.DownloadFile(g.Filename, Postal_DownloadDirectory);
				Console.WriteLine("Testing {0}: {1}", g.Filename, g.Test(Path.Combine(Postal_DownloadDirectory, g.Filename)));
			});

			Console.WriteLine("Testing ASCII fields");
			DumpASCIILies(Dump_DownloadDirectory);

			Console.WriteLine("All done!");
		}

		private static async Task<GeoFile[]> GetCountryPostalcodes(GeoFileDownloader downloader)
		{
			using var client = new HttpClient();
			var document = await client.GetStringAsync(downloader.BaseUri);

			var countries = new Regex("href=\"([A-Z]{2}.zip)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile { Filename = m.Groups[1].Value, Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadPostalcodes(fn).Count(); }) });

			return new[] {
				new GeoFile { Filename = "allCountries.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadPostalcodes(fn).Count(); }) }
			}.Union(countries.OrderBy(m => m.Filename)).ToArray();
		}

		private static GeoFile[] GetDumps(GeoFileDownloader downloader)
		{
			return new[] {
				new GeoFile { Filename = "admin1CodesASCII.txt", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadAdmin1Codes(fn).Count(); }) },
				new GeoFile { Filename = "admin2Codes.txt", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadAdmin2Codes(fn).Count(); }) },
				new GeoFile { Filename = "allCountries.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) },
				new GeoFile { Filename = "alternateNames.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadAlternateNames(fn).Count(); }) },
				new GeoFile { Filename = "alternateNamesV2.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadAlternateNamesV2(fn).Count(); }) },
				new GeoFile { Filename = "cities1000.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) },
				new GeoFile { Filename = "cities15000.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) },
				new GeoFile { Filename = "cities5000.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) },
				new GeoFile { Filename = "countryInfo.txt", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadCountryInfo(fn).Count(); }) },
                //Featurecodes are downloaded by GetCountryDumps()
                new GeoFile { Filename = "hierarchy.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadHierarchy(fn).Count(); }) },
				new GeoFile { Filename = "iso-languagecodes.txt", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadISOLanguageCodes(fn).Count(); }) },
				new GeoFile { Filename = "no-country.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) },
				new GeoFile { Filename = "timeZones.txt", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadTimeZones(fn).Count(); }) },
				new GeoFile { Filename = "userTags.zip", Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadUserTags(fn).Count(); }) },
					}.Union(GetCountryDumps(downloader).Result).ToArray();
		}

		private static async Task<GeoFile[]> GetCountryDumps(GeoFileDownloader downloader)
		{
			using var client = new HttpClient();
			var document = await client.GetStringAsync(downloader.BaseUri);

			var countries = new Regex("href=\"([A-Z]{2}.zip)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile { Filename = m.Groups[1].Value, Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadExtendedGeoNames(fn).Count(); }) });

			var featurecodes = new Regex("href=\"(featureCodes_[A-Z]{2}.txt)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
				.Matches(document)
				.Cast<Match>()
				.Select(m => new GeoFile { Filename = m.Groups[1].Value, Test = (f) => ExecuteTest(f, (fn) => { return GeoFileReader.ReadFeatureCodes(fn).Count(); }) });

			return [.. countries.Union(featurecodes).OrderBy(m => m.Filename)];
		}

		private static string ExecuteTest(string filename, Func<string, int> test)
		{
			try
			{
				//Haaaack
				var file = filename.Replace(".zip", ".txt");

				//Haaaaaaaaaack
				//if (file.EndsWith("no-country.txt"))
				//    file = file.Replace("no-country.txt", "null.txt");

				return string.Format("{0} records OK", test(file));
			}
			catch (Exception ex)
			{
				return string.Format("FAILED: {0}", ex.Message);
			}
		}

		private static void DumpASCIILies(string logpath)
		{
			using (var lw = File.CreateText(Path.Combine(logpath, "_asciilies.log")))
			{
				//Test for fields that claim to contain ASCII only but contain non-ASCII data anyways
				var nonasciifilter = new Regex("[^\x20-\x7F]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				var geofilefilter = new Regex("^[A-Z]{2}.txt$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

				lw.WriteLine("The following files contain entries that claim to contain ASCII only but contain non-ASCII data anyways:");

				var extgeofiles = new[] { "allCountries", "cities1000", "cities5000", "cities15000", "no-country" }
					.Select(f => Path.Combine(Dump_DownloadDirectory, f + ".txt"))
					.Union(Directory.GetFiles(Dump_DownloadDirectory, "*.txt")
						.Where(f => geofilefilter.IsMatch(Path.GetFileName(f)))
					);

				var lies = extgeofiles.AsParallel()
					.SelectMany(f => GeoFileReader.ReadExtendedGeoNames(f)
						.Where(e => nonasciifilter.IsMatch(e.NameASCII))
						.Select(i => new NonASCIIEntry { FileName = f, Id = i.Id, Value = i.NameASCII })
					).Union(
						GeoFileReader.ReadAdmin1Codes(Path.Combine(Dump_DownloadDirectory, "admin1CodesASCII.txt")).AsParallel()
							.Where(c => nonasciifilter.IsMatch(c.NameASCII))
							.Select(i => new NonASCIIEntry { FileName = "admin1CodesASCII.txt", Id = i.GeoNameId, Value = i.NameASCII })
					).Union(
						GeoFileReader.ReadAdmin2Codes(Path.Combine(Dump_DownloadDirectory, "admin2Codes.txt")).AsParallel()
							.Where(c => nonasciifilter.IsMatch(c.NameASCII))
							.Select(i => new NonASCIIEntry { FileName = "admin2Codes.txt", Id = i.GeoNameId, Value = i.NameASCII })
					);

				foreach (var l in lies.OrderBy(l => l.FileName).ThenBy(l => l.Value))
				{
					lw.WriteLine(string.Join("\t", Path.GetFileName(l.FileName), l.Id, l.Value));
				};
			}
		}
	}

	internal class GeoFile
	{
		public string Filename { get; set; }
		public Func<string, string> Test { get; set; }

		public GeoFile()
		{
			this.Test = (f) => { throw new NotImplementedException(); };
		}
	}

	internal class NonASCIIEntry
	{
		public string FileName { get; set; }
		public string Value { get; set; }
		public int Id { get; set; }
	}
}