using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGeoNames;
using NGeoNames.Parsers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGeoNamesTests
{
	[TestClass]
	public class ParserTests
	{
		[TestMethod]
		[ExpectedException(typeof(ParserException))]
		public async Task ParserThrowsOnInvalidData()
		{
			var target = await GeoFileReader.ReadAdmin1CodesAsync(@"testdata\invalid_admin1CodesASCII.txt").ToListAsync();
		}

		[TestMethod]
		[ExpectedException(typeof(FileNotFoundException))]
		public async Task ParserThrowsOnNonExistingFile()
		{
			var target = await GeoFileReader.ReadAdmin1CodesAsync(@"testdata\non_existing_file.txt").ToListAsync();
		}

		[TestMethod]
		public async Task Admin1CodesParser_ParsesFileCorrectly()
		{
			var target = (await GeoFileReader.ReadAdmin1CodesAsync(@"testdata\test_admin1CodesASCII.txt").ToListAsync()).ToArray();
			Assert.AreEqual(4, target.Length);

			Assert.AreEqual("CF.04", target[0].Code);
			Assert.AreEqual("Mambéré-Kadéï", target[0].Name);
			Assert.AreEqual("Mambere-Kadei", target[0].NameASCII);
			Assert.AreEqual(2386161, target[0].GeoNameId);

			Assert.AreEqual("This.is.a.VERY.LONG.ID", target[1].Code);
			Assert.AreEqual("Iğdır", target[1].Name);
			Assert.AreEqual("Igdir", target[1].NameASCII);
			Assert.AreEqual(0, target[1].GeoNameId);

			Assert.AreEqual("TR.80", target[2].Code);
			Assert.AreEqual("Şırnak", target[2].Name);
			Assert.AreEqual("Sirnak", target[2].NameASCII);
			Assert.AreEqual(443189, target[2].GeoNameId);

			Assert.AreEqual("UA.26", target[3].Code);
			Assert.AreEqual("Zaporiz’ka Oblast’", target[3].Name);
			Assert.AreEqual("Zaporiz'ka Oblast'", target[3].NameASCII);
			Assert.AreEqual(687699, target[3].GeoNameId);
		}

		[TestMethod]
		public async Task Admin2CodesParser_ParsesFileCorrectly()
		{
			var target = (await GeoFileReader.ReadAdmin2CodesAsync(@"testdata\test_admin2Codes.txt").ToListAsync()).ToArray();
			Assert.AreEqual(2, target.Length);

			Assert.AreEqual("AF.01.7052666", target[0].Code);
			Assert.AreEqual("Darwāz-e Bālā", target[0].Name);
			Assert.AreEqual("Darwaz-e Bala", target[0].NameASCII);
			Assert.AreEqual(7052666, target[0].GeoNameId);

			Assert.AreEqual("CA.10.11", target[1].Code);
			Assert.AreEqual("Gaspésie-Îles-de-la-Madeleine", target[1].Name);
			Assert.AreEqual("Gaspesie-Iles-de-la-Madeleine", target[1].NameASCII);
			Assert.AreEqual(0, target[1].GeoNameId);
		}

		[TestMethod]
		public async Task AlternateNamesParser_ParsesFileCorrectly()
		{
			var target = (await GeoFileReader.ReadAlternateNamesAsync(@"testdata\test_alternateNames.txt").ToListAsync()).ToArray();
			Assert.AreEqual(17, target.Length);

			Assert.AreEqual(2488123, target[0].Id);
			Assert.AreEqual(4, target[0].GeoNameId);
			Assert.AreEqual("رودخانه زاکلی", target[0].Name);
			Assert.AreEqual("fa", target[0].ISOLanguage);
			Assert.IsNull(target[0].Type);
			Assert.IsFalse(target[0].IsPreferredName);
			Assert.IsFalse(target[0].IsColloquial);
			Assert.IsFalse(target[0].IsShortName);
			Assert.IsFalse(target[0].IsHistoric);

			Assert.AreEqual("http://en.wikipedia.org/wiki/Takht-e_Qeysar", target[1].Name);
			Assert.IsNull(target[1].ISOLanguage);
			Assert.AreEqual("link", target[1].Type);
		}

		[TestMethod]
		public async Task AlternateNamesParserV2_ParsesFileCorrectly()
		{
			var target = (await GeoFileReader.ReadAlternateNamesV2Async(@"testdata\test_alternateNamesV2.txt").ToListAsync()).ToArray();
			Assert.AreEqual(5, target.Length);

			Assert.AreEqual(12453592, target[0].Id);
			Assert.AreEqual(7258581, target[0].GeoNameId);
			Assert.AreEqual("Rossmoor CDP", target[0].Name);
			Assert.AreEqual(string.Empty, target[0].ISOLanguage);
			Assert.IsNull(target[0].Type);
			Assert.IsFalse(target[0].IsPreferredName);
			Assert.IsFalse(target[0].IsColloquial);
			Assert.IsFalse(target[0].IsShortName);
			Assert.IsTrue(target[0].IsHistoric);

			Assert.AreEqual(string.Empty, target[0].From);
			Assert.AreEqual("19 February 2008", target[0].To);
		}

		[TestMethod]
		public async Task CountryInfoParser_ParsesFileCorrectly()
		{
			var target = (await GeoFileReader.ReadCountryInfoAsync(@"testdata\test_countryInfo.txt").ToListAsync()).ToArray();
			Assert.AreEqual(2, target.Length);

			Assert.AreEqual("BZ", target[0].ISO_Alpha2);
			Assert.AreEqual("BLZ", target[0].ISO_Alpha3);
			Assert.AreEqual("084", target[0].ISO_Numeric);
			Assert.AreEqual("Belize", target[0].Country);
			Assert.AreEqual("Belmopan", target[0].Capital);
		}

		[TestMethod]
		public async Task ExtendedGeoNameParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadExtendedGeoNamesAsync(@"testdata\test_extendedgeonames.txt").ToListAsync();
			Assert.AreEqual(7, target.Count);

			Assert.AreEqual(string.Empty, target[0].Admincodes[0]);
			Assert.AreEqual(string.Empty, target[0].Admincodes[1]);
			Assert.AreEqual(string.Empty, target[0].Admincodes[2]);
			Assert.AreEqual(string.Empty, target[0].Admincodes[3]);
			Assert.AreEqual(0, target[0].AlternateCountryCodes.Length);
			Assert.AreEqual(0, target[0].AlternateNames.Length);

			Assert.AreEqual("09", target[1].Admincodes[0]);
			Assert.AreEqual("900", target[1].Admincodes[1]);
			Assert.AreEqual("923", target[1].Admincodes[2]);
			Assert.AreEqual("2771781", target[1].Admincodes[3]);
			Assert.AreEqual(1, target[1].AlternateCountryCodes.Length);
			Assert.AreEqual(2, target[1].AlternateNames.Length);
		}

		[TestMethod]
		public async Task FeatureCodeParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadFeatureCodesAsync(@"testdata\test_featureCodes_en.txt").ToListAsync();
			Assert.AreEqual(3, target.Count);

			Assert.AreEqual("A", target[0].Class);
			Assert.AreEqual("ADM1", target[0].Code);

			Assert.AreEqual("first-order administrative division", target[0].Name);
			Assert.AreEqual("a primary administrative division of a country, such as a state in the United States", target[0].Description);

			Assert.AreEqual("XXX", target[1].Class);
			Assert.IsNull(target[1].Code);

			Assert.IsNull(target[2].Class);
			Assert.IsNull(target[2].Code);
		}

		[TestMethod]
		public async Task GeoNameParser_ParsesFileCorrectly()
		{
			using var stream = File.OpenRead(@"testdata\test_geonames.txt");
			var target = await GeoFileReader.ReadGeoNamesAsync(stream).ToListAsync();
			Assert.AreEqual(2, target.Count);

			Assert.AreEqual(1136469, target[0].Id);
			Assert.AreEqual("Khōst", target[0].Name);
			Assert.AreEqual(33.33951, target[0].Latitude);
			Assert.AreEqual(69.92041, target[0].Longitude);

			Assert.AreEqual(3865840, target[1].Id);
			Assert.AreEqual("Añatuya", target[1].Name);
			Assert.AreEqual(-28.46064, target[1].Latitude);
			Assert.AreEqual(-62.83472, target[1].Longitude);
		}

		[TestMethod]
		public async Task UserTagParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadUserTagsAsync(@"testdata\test_userTags.txt").ToListAsync();
			Assert.AreEqual(3, target.Count);

			Assert.AreEqual(2599253, target[0].GeoNameId);
			Assert.AreEqual("opengeodb", target[0].Tag);

			Assert.AreEqual(6255065, target[1].GeoNameId);
			Assert.AreEqual("http://de.wikipedia.org/wiki/Gotthardgebäude", target[1].Tag);

			Assert.AreEqual(6941058, target[2].GeoNameId);
			Assert.AreEqual("lyžařské", target[2].Tag);
		}

		[TestMethod]
		public async Task ContinentParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadContinentsAsync(@"testdata\test_continentCodes.txt").ToListAsync();
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual("EU", target[0].Code);
			Assert.AreEqual("Europe", target[0].Name);
			Assert.AreEqual(6255148, target[0].GeoNameId);
		}

		[TestMethod]
		public async Task FeatureClassesParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadFeatureClassesAsync(@"testdata\test_featureClasses_en.txt").ToListAsync();
			Assert.AreEqual(1, target.Count);

			Assert.AreEqual("X", target[0].Class);
			Assert.AreEqual("Test", target[0].Description);
		}

		[TestMethod]
		public async Task PostalCodeParser_ParsesFileCorrectly()
		{
			var target = await GeoFileReader.ReadPostalcodesAsync(@"testdata\test_postalcodes.txt").ToListAsync();
			Assert.AreEqual(4, target.Count);

			Assert.AreEqual("AU", target[0].CountryCode);
			Assert.AreEqual("0200", target[0].PostalCode);
			Assert.AreEqual("Australian National University", target[0].PlaceName);
			Assert.IsTrue(double.IsNaN(target[0].Latitude));
			Assert.IsTrue(double.IsNaN(target[0].Longitude));
			Assert.IsNull(target[0].Accuracy);

			Assert.AreEqual("CZ", target[1].CountryCode);
			Assert.AreEqual("561 13", target[1].PostalCode);
			Assert.AreEqual("Orlické Podhůří-Rozsocha", target[1].PlaceName);
			Assert.AreEqual(50.0333, target[1].Latitude);
			Assert.AreEqual(16.2833, target[1].Longitude);
			Assert.IsNull(target[1].Accuracy);
		}

		[TestMethod]
		public async Task CustomParser_IsUsedCorrectlyAsync()
		{
			var target = (await new GeoFileReader().ReadRecordsAsync(@"testdata\test_custom.txt", new CustomParser(19, 5, new[] { '☃' }, Encoding.UTF7, true)).ToListAsync()).ToArray();

			Assert.AreEqual(2, target.Length);
			CollectionAssert.AreEqual(target[0].Data, target[1].Data);
		}

		[TestMethod]
		public async Task FileReader_HandlesEmptyFilesCorrectly()
		{
			var target1 = await new GeoFileReader().ReadRecordsAsync(@"testdata\emptyfile.txt", new CustomParser(19, 5, new[] { '☃' }, Encoding.UTF8, true)).ToListAsync();
			Assert.AreEqual(0, target1.Count);

			var target2 = await GeoFileReader.ReadExtendedGeoNamesAsync(@"testdata\emptyfile.txt").ToListAsync();
			Assert.AreEqual(0, target2.Count);
		}

		[TestMethod]
		public async Task FileReader_HandlesGZippedFilesCorrectly()
		{
			var target = await GeoFileReader.ReadCountryInfoAsync(@"testdata\countryInfo_gzip_compat.txt.gz").ToListAsync();
			Assert.AreEqual(2, target.Count);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidDataException))]
		public async Task FileReader_ThrowsOnIncompatibleOrInvalidGZipFiles()
		{
			var target = await GeoFileReader.ReadCountryInfoAsync(@"testdata\countryInfo_not_gzip_compat.txt.gz").ToListAsync();
		}

		[TestMethod]
		public async Task FileReader_CanReadBuiltInContinentsCorrectly()
		{
			var target = await GeoFileReader.ReadBuiltInContinentsAsync().ToListAsync();
			Assert.AreEqual(7, target.Count);
			CollectionAssert.AreEqual(target.OrderBy(c => c.Code).Select(c => c.Code).ToArray(), new[] { "AF", "AN", "AS", "EU", "NA", "OC", "SA" });
		}

		[TestMethod]
		public async Task FileReader_CanReadBuiltInFeatureClassesCorrectly()
		{
			var target = await GeoFileReader.ReadBuiltInFeatureClassesAsync().ToListAsync();
			Assert.AreEqual(9, target.Count);
			CollectionAssert.AreEqual(target.OrderBy(c => c.Class).Select(c => c.Class).ToArray(), new[] { "A", "H", "L", "P", "R", "S", "T", "U", "V" });
		}

		[TestMethod]
		public async Task FileReader_Admin1Codes_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_admin1CodesASCII.txt"))
				await GeoFileReader.ReadAdmin1CodesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_Admin2Codes_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_admin2Codes.txt"))
				await GeoFileReader.ReadAdmin2CodesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_AlternateNames_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_alternateNames.txt"))
				await GeoFileReader.ReadAlternateNamesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_AlternateNamesV2_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_alternateNamesV2.txt"))
				await GeoFileReader.ReadAlternateNamesV2Async(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_Continent_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_continentCodes.txt"))
				await GeoFileReader.ReadContinentsAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_CountryInfo_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_CountryInfo.txt"))
				await GeoFileReader.ReadCountryInfoAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_ExtendedGeoNames_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_extendedgeonames.txt"))
				await GeoFileReader.ReadExtendedGeoNamesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_FeatureClasses_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_featureclasses_en.txt"))
				await GeoFileReader.ReadFeatureClassesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_FeatureCodes_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_featurecodes_en.txt"))
				await GeoFileReader.ReadFeatureCodesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_GeoNames_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_geonames.txt"))
				await GeoFileReader.ReadGeoNamesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_Hierarchy_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_hierarchy.txt"))
				await GeoFileReader.ReadHierarchyAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_ISOLanguageCode_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_iso-languagecodes.txt"))
				await GeoFileReader.ReadISOLanguageCodesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_TimeZone_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_timeZones.txt"))
				await GeoFileReader.ReadTimeZonesAsync(s).CountAsync();
		}

		[TestMethod]
		public async Task FileReader_UserTags_StreamOverload()
		{
			using (var s = File.OpenRead(@"testdata\test_usertags.txt"))
				await GeoFileReader.ReadUserTagsAsync(s).CountAsync();
		}
	}
}