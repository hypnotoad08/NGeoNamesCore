using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGeoNames;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGeoNamesTests
{
	[TestClass]
	public class GeoFileReaderTests
	{
		[TestMethod]
		public async Task GeoFileReader_ParsesFileCorrectly1()
		{
			var gf = new GeoFileReader();
			var target = (await gf.ReadRecordsAsync(@"testdata\test_geofilereadercustom1.txt", new CustomParser(9, 1, new[] { ',' }, Encoding.UTF8, true)).ToListAsync()).ToArray();
			Assert.AreEqual(2, target.Length);
		}

		[TestMethod]
		public async Task GeoFileReader_ParsesFileCorrectly2()
		{
			var gf = new GeoFileReader();
			var target = (await gf.ReadRecordsAsync(@"testdata\test_geofilereadercustom2.txt", new CustomParser(4, 0, new[] { '!' }, Encoding.UTF8, false)).ToListAsync()).ToArray();
			Assert.AreEqual(3, target.Length);
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public async Task GeoFileReader_ThrowsOnFailureWhenAutodetectingFileType()
		{
			//When filetype == autodetect and an unknown extension is used an exception should be thrown
			var gf = new GeoFileReader();
			var target = (await gf.ReadRecordsAsync(@"testdata\invalid.ext", new CustomParser(5, 0, new[] { '\t' }, Encoding.UTF8, false)).ToListAsync()).ToArray();
		}

		[TestMethod]
		public async Task GeoFileReader_DoesNotThrowOnInvalidExtensionButSpecifiedFileType()
		{
			//When filetype is specified and an unknown extension is used it should be read fine
			var gf = new GeoFileReader();
			var target = (await gf.ReadRecordsAsync(@"testdata\invalid.ext", FileType.Plain, new CustomParser(5, 0, new[] { '\t' }, Encoding.UTF8, false)).ToListAsync()).ToArray();
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public async Task GeoFileReader_ThrowsOnUnknownSpecifiedFileType()
		{
			//When and unknown filetype is specified an exception should be thrown
			var gf = new GeoFileReader();
			var target = (await gf.ReadRecordsAsync(@"testdata\invalid.ext", (FileType)999, new CustomParser(5, 0, new[] { '\t' }, Encoding.UTF8, false)).ToListAsync()).ToArray();
		}
	}
}