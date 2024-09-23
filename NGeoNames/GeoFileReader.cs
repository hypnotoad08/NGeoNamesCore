#nullable enable

using NGeoNames.Entities;
using NGeoNames.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZone = NGeoNames.Entities.TimeZone;

namespace NGeoNames
{
	/// <summary>
	/// Provides methods to read/parse files from geonames.org asynchronously.
	/// </summary>
	public class GeoFileReader
	{
		/// <summary>
		/// Reads records of type T asynchronously, using the specified parser to parse the values.
		/// </summary>
		/// <typeparam name="T">The type of objects to read/parse.</typeparam>
		/// <param name="path">The path of the file to read/parse.</param>
		/// <param name="parser">The <see cref="IParser{T}"/> to use when reading the file.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(string path, IParser<T> parser)
		{
			await foreach (var record in ReadRecordsAsync(path, FileType.AutoDetect, parser))
			{
				yield return record;
			}
		}

		/// <summary>
		/// Reads records of type T asynchronously, using the specified parser to parse the values.
		/// </summary>
		/// <typeparam name="T">The type of objects to read/parse.</typeparam>
		/// <param name="path">The path of the file to read/parse.</param>
		/// <param name="fileType">The <see cref="FileType"/> of the file.</param>
		/// <param name="parser">The <see cref="IParser{T}"/> to use when reading the file.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(string path, FileType fileType, IParser<T> parser)
		{
			await using var fileStream = await GetStreamAsync(path, fileType);
			await foreach (var record in ReadRecordsAsync(fileStream, parser))
			{
				yield return record;
			}
		}

		/// <summary>
		/// Reads records of type T asynchronously from a stream, using the specified parser to parse the values.
		/// </summary>
		/// <typeparam name="T">The type of objects to read/parse.</typeparam>
		/// <param name="stream">The <see cref="Stream"/> to read/parse.</param>
		/// <param name="parser">The <see cref="IParser{T}"/> to use when reading the file.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(Stream stream, IParser<T> parser)
		{
			using var reader = new StreamReader(stream, parser.Encoding);
			string? line;
			int lineCount = 0;

			while ((line = await reader.ReadLineAsync()) is not null)
			{
				lineCount++;

				if (lineCount > parser.SkipLines && line.Length > 0 && (!parser.HasComments || !line.StartsWith('#')))
				{
					var data = line.Split(parser.FieldSeparators);
					if (data.Length != parser.ExpectedNumberOfFields)
					{
						throw new ParserException($"Expected {parser.ExpectedNumberOfFields} fields, but got {data.Length} on line {lineCount}.");
					}

					yield return parser.Parse(data);
				}
			}
		}

		/// <summary>
		/// Opens a file stream based on the file type asynchronously.
		/// </summary>
		private static async Task<Stream> GetStreamAsync(string path, FileType fileType)
		{
			var fileStream = File.OpenRead(path);

			var detectedFileType = fileType == FileType.AutoDetect ? FileUtil.GetFileTypeFromExtension(path) : fileType;
			return detectedFileType switch
			{
				FileType.Plain => fileStream,
				FileType.GZip => new GZipStream(fileStream, CompressionMode.Decompress),
				FileType.Zip => await OpenZipStreamAsync(fileStream),
				_ => throw new NotSupportedException($"File type {detectedFileType} is not supported."),
			};
		}

		/// <summary>
		/// Opens the first entry from a ZIP file asynchronously.
		/// </summary>
		private static Task<Stream> OpenZipStreamAsync(Stream fileStream)
		{
			var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
			var entry = zipArchive.Entries.First();
			return Task.FromResult(entry.Open()); // Assumes single file inside the zip
		}

		#region Convenience Methods

		/// <summary>
		/// Reads <see cref="ExtendedGeoName"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<ExtendedGeoName> ReadExtendedGeoNamesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ExtendedGeoNameParser());
		}

		/// <summary>
		/// Reads <see cref="ExtendedGeoName"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<ExtendedGeoName> ReadExtendedGeoNamesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ExtendedGeoNameParser());
		}

		/// <summary>
		/// Reads <see cref="GeoName"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<GeoName> ReadGeoNamesAsync(string filename, bool useExtendedFileFormat = true)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new GeoNameParser(useExtendedFileFormat));
		}

		/// <summary>
		/// Reads <see cref="GeoName"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<GeoName> ReadGeoNamesAsync(Stream stream, bool useExtendedFileFormat = true)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new GeoNameParser(useExtendedFileFormat));
		}

		/// <summary>
		/// Reads <see cref="Admin1Code"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<Admin1Code> ReadAdmin1CodesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new Admin1CodeParser());
		}

		/// <summary>
		/// Reads <see cref="Admin1Code"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<Admin1Code> ReadAdmin1CodesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new Admin1CodeParser());
		}

		/// <summary>
		/// Reads <see cref="Admin2Code"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<Admin2Code> ReadAdmin2CodesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new Admin2CodeParser());
		}

		/// <summary>
		/// Reads <see cref="Admin2Code"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<Admin2Code> ReadAdmin2CodesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new Admin2CodeParser());
		}

		/// <summary>
		/// Reads <see cref="AlternateName"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<AlternateName> ReadAlternateNamesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new AlternateNameParser());
		}

		/// <summary>
		/// Reads <see cref="AlternateName"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<AlternateName> ReadAlternateNamesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new AlternateNameParser());
		}

		/// <summary>
		/// Reads <see cref="AlternateNameV2"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<AlternateNameV2> ReadAlternateNamesV2Async(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new AlternateNameParserV2());
		}

		/// <summary>
		/// Reads <see cref="AlternateNameV2"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<AlternateNameV2> ReadAlternateNamesV2Async(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new AlternateNameParserV2());
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the built-in data.
		/// </summary>
		public static IAsyncEnumerable<Continent> ReadBuiltInContinentsAsync()
		{
			return ReadBuiltInResourceAsync("continentCodes", new ContinentParser());
		}

		private static async IAsyncEnumerable<T> ReadBuiltInResourceAsync<T>(string name, IParser<T> parser)
		{
			var data = Properties.Resources.ResourceManager.GetString(name) ?? throw new ArgumentNullException(nameof(name), $"Resource with name {name} not found.");
			await using var memoryStream = new MemoryStream(parser.Encoding.GetBytes(data));
			await foreach (var record in new GeoFileReader().ReadRecordsAsync(memoryStream, parser))
			{
				yield return record;
			}
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<Continent> ReadContinentsAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ContinentParser());
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<Continent> ReadContinentsAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ContinentParser());
		}

		/// <summary>
		/// Reads <see cref="CountryInfo"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<CountryInfo> ReadCountryInfoAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new CountryInfoParser());
		}

		/// <summary>
		/// Reads <see cref="CountryInfo"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<CountryInfo> ReadCountryInfoAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new CountryInfoParser());
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the built-in data.
		/// </summary>
		public static IAsyncEnumerable<FeatureClass> ReadBuiltInFeatureClassesAsync()
		{
			return ReadBuiltInResourceAsync("featureClasses_en", new FeatureClassParser());
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<FeatureClass> ReadFeatureClassesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new FeatureClassParser());
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<FeatureClass> ReadFeatureClassesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new FeatureClassParser());
		}

		/// <summary>
		/// Reads <see cref="FeatureCode"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<FeatureCode> ReadFeatureCodesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new FeatureCodeParser());
		}

		/// <summary>
		/// Reads <see cref="FeatureCode"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<FeatureCode> ReadFeatureCodesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new FeatureCodeParser());
		}

		/// <summary>
		/// Reads <see cref="HierarchyNode"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<HierarchyNode> ReadHierarchyAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new HierarchyParser());
		}

		/// <summary>
		/// Reads <see cref="HierarchyNode"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<HierarchyNode> ReadHierarchyAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new HierarchyParser());
		}

		/// <summary>
		/// Reads <see cref="ISOLanguageCode"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<ISOLanguageCode> ReadISOLanguageCodesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ISOLanguageCodeParser());
		}

		/// <summary>
		/// Reads <see cref="ISOLanguageCode"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<ISOLanguageCode> ReadISOLanguageCodesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ISOLanguageCodeParser());
		}

		/// <summary>
		/// Reads <see cref="TimeZone"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<TimeZone> ReadTimeZonesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new TimeZoneParser());
		}

		/// <summary>
		/// Reads <see cref="TimeZone"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<TimeZone> ReadTimeZonesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new TimeZoneParser());
		}

		/// <summary>
		/// Reads <see cref="UserTag"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<UserTag> ReadUserTagsAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new UserTagParser());
		}

		/// <summary>
		/// Reads <see cref="UserTag"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<UserTag> ReadUserTagsAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new UserTagParser());
		}

		/// <summary>
		/// Reads <see cref="Postalcode"/> records asynchronously from the specified file.
		/// </summary>
		public static IAsyncEnumerable<Postalcode> ReadPostalcodesAsync(string filename)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new PostalcodeParser());
		}

		/// <summary>
		/// Reads <see cref="Postalcode"/> records asynchronously from the specified stream.
		/// </summary>
		public static IAsyncEnumerable<Postalcode> ReadPostalcodesAsync(Stream stream)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new PostalcodeParser());
		}

		#endregion Convenience Methods
	}
}