using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGeoNames;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace NGeoNamesTests
{
	[TestClass]
	public class ComposerTests
	{
		[TestMethod]
		public async Task Admin1CodesComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_admin1CodesASCII.txt";
			var dst = @"testdata\test_admin1CodesASCII.out.txt";

			await GeoFileWriter.WriteAdmin1CodesAsync(dst, await GeoFileReader.ReadAdmin1CodesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 4, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task Admin2CodesComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_admin2Codes.txt";
			var dst = @"testdata\test_admin2Codes.out.txt";

			await GeoFileWriter.WriteAdmin2CodesAsync(dst, await GeoFileReader.ReadAdmin2CodesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 4, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task AlternateNamesComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_alternateNames.txt";
			var dst = @"testdata\test_alternateNames.out.txt";

			await GeoFileWriter.WriteAlternateNamesAsync(dst, await GeoFileReader.ReadAlternateNamesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 8, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task AlternateNamesComposerV2_ComposesFileCorrectly()
		{
			var src = @"testdata\test_alternateNamesV2.txt";
			var dst = @"testdata\test_alternateNamesV2.out.txt";

			await GeoFileWriter.WriteAlternateNamesV2Async(dst, await GeoFileReader.ReadAlternateNamesV2Async(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 10, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task ContinentComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_continentCodes.txt";
			var dst = @"testdata\test_continentCodes.out.txt";

			await GeoFileWriter.WriteContinentsAsync(dst, await GeoFileReader.ReadContinentsAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 3, 0, new[] { '\t' }, Encoding.UTF8, true);
		}

		[TestMethod]
		public async Task CountryInfoComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_countryInfo2.txt";
			var dst = @"testdata\test_countryInfo2.out.txt";

			await GeoFileWriter.WriteCountryInfoAsync(dst, await GeoFileReader.ReadCountryInfoAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 19, 0, new[] { '\t' }, Encoding.UTF8, true);
		}

		[TestMethod]
		public async Task ExtendedGeoNamesComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_extendedgeonames.txt";
			var dst = @"testdata\test_extendedgeonames.out.txt";

			await GeoFileWriter.WriteExtendedGeoNamesAsync(dst, await GeoFileReader.ReadExtendedGeoNamesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 19, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task FeatureClassComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_featureClasses_en.txt";
			var dst = @"testdata\test_featureClasses_en.out.txt";

			await GeoFileWriter.WriteFeatureClassesAsync(dst, await GeoFileReader.ReadFeatureClassesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 2, 0, new[] { '\t' }, Encoding.UTF8, true);
		}

		[TestMethod]
		public async Task FeatureCodeComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_featureCodes_en.txt";
			var dst = @"testdata\test_featureCodes_en.out.txt";

			await GeoFileWriter.WriteFeatureCodesAsync(dst, await GeoFileReader.ReadFeatureCodesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 3, 0, new[] { '\t' }, Encoding.UTF8, true);
		}

		[TestMethod]
		public async Task GeoNamesComposerSimple_ComposesFileCorrectly()
		{
			// In this test we test the "compact file format" (e.g. GeoName, not ExtendedGeoName)
			var src = @"testdata\test_geonames_simple.txt";
			var dst = @"testdata\test_geonames_simple.out.txt";

			using (var srcStream = new FileStream(src, FileMode.Open, FileAccess.Read))
			{
				var geoNames = await GeoFileReader.ReadGeoNamesAsync(srcStream, false).ToListAsync();
				using (var dstStream = new FileStream(dst, FileMode.Create, FileAccess.Write))
				{
					await GeoFileWriter.WriteGeoNamesAsync(dstStream, geoNames, false);
				}
			}

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 4, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task GeoNamesComposerSimple_ComposesFileCorrectlyAsync()
		{
			// In this test, we test the "compact file format" (e.g. GeoName, not ExtendedGeoName)
			var src = @"testdata\test_geonames_simple.txt";
			var dst = @"testdata\test_geonames_simple.out.txt";

			// Reading and writing the file asynchronously
			var geoNames = await GeoFileReader.ReadGeoNamesAsync(src, false).ToListAsync();

			using (var stream = new FileStream(dst, FileMode.Create, FileAccess.Write))
			{
				await GeoFileWriter.WriteGeoNamesAsync(stream, geoNames, false);
			}

			// Ensure both files (src and dst) are functionally equal
			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 4, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task HierarchyComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_hierarchy.txt";
			var dst = @"testdata\test_hierarchy.out.txt";

			await GeoFileWriter.WriteHierarchyAsync(dst, await GeoFileReader.ReadHierarchyAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 3, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task ISOLanguageCodeComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_iso-languagecodes.txt";
			var dst = @"testdata\test_iso-languagecodes.out.txt";

			await GeoFileWriter.WriteISOLanguageCodesAsync(dst, await GeoFileReader.ReadISOLanguageCodesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 4, 1, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task TimeZoneComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_timeZones.txt";
			var dst = @"testdata\test_timeZones.out.txt";

			await GeoFileWriter.WriteTimeZonesAsync(dst, await GeoFileReader.ReadTimeZonesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 5, 1, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task UserTagComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_userTags.txt";
			var dst = @"testdata\test_userTags.out.txt";

			await GeoFileWriter.WriteUserTagsAsync(dst, await GeoFileReader.ReadUserTagsAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 2, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task PostalcodeComposer_ComposesFileCorrectly()
		{
			var src = @"testdata\test_postalCodes.txt";
			var dst = @"testdata\test_postalCodes.out.txt";

			await GeoFileWriter.WritePostalcodesAsync(dst, await GeoFileReader.ReadPostalcodesAsync(src).ToListAsync());

			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 12, 0, new[] { '\t' }, Encoding.UTF8, false);
		}

		[TestMethod]
		public async Task CustomComposer_ComposesFileCorrectlyAsync()
		{
			var src = @"testdata\test_custom.txt";
			var dst = @"testdata\test_custom.out.txt";

			// Read records from the source file asynchronously
			var records = await new GeoFileReader().ReadRecordsAsync(src, new CustomParser(19, 5, new[] { '☃' }, Encoding.UTF7, true)).ToListAsync();

			// Write the records to the destination file asynchronously
			using (var stream = new FileStream(dst, FileMode.Create, FileAccess.Write))
			{
				await new GeoFileWriter().WriteRecordsAsync(stream, records, new CustomComposer(Encoding.UTF7, '☃'));
			}

			// Ensure both files (src and dst) are functionally equal
			FileUtil.EnsureFilesAreFunctionallyEqual(src, dst, 19, 5, new[] { '☃' }, Encoding.UTF7, true);
		}

		[TestMethod]
		public async Task Composer_HandlesGZippedFilesCorrectly()
		{
			var src = @"testdata\test_extendedgeonames.txt";
			var dst = @"testdata\test_extendedgeonames.out.gz";

			await GeoFileWriter.WriteExtendedGeoNamesAsync(dst, await GeoFileReader.ReadExtendedGeoNamesAsync(src).ToListAsync());

			Assert.AreEqual(7, (await GeoFileReader.ReadExtendedGeoNamesAsync(dst).ToListAsync()).Count());
		}
	}
}