#nullable enable

using NGeoNames.Entities;
using NGeoNames.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(string path, IParser<T> parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var record in ReadRecordsAsync(path, FileType.AutoDetect, parser, cancellationToken).ConfigureAwait(false))
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
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(string path, FileType fileType, IParser<T> parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var fileStream = await GetStreamAsync(path, fileType, cancellationToken).ConfigureAwait(false);
			await foreach (var record in ReadRecordsAsync(fileStream, parser, cancellationToken).ConfigureAwait(false))
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
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		/// <returns>Returns an IAsyncEnumerable of T representing the records read/parsed.</returns>
		public async IAsyncEnumerable<T> ReadRecordsAsync<T>(Stream stream, IParser<T> parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var reader = new StreamReader(stream, parser.Encoding);
			string? line;
			int lineCount = 0;

			while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
			{
				cancellationToken.ThrowIfCancellationRequested();
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
		private static async Task<Stream> GetStreamAsync(string path, FileType fileType, CancellationToken cancellationToken = default)
		{
			var fileStream = File.OpenRead(path);

			var detectedFileType = fileType == FileType.AutoDetect ? FileUtil.GetFileTypeFromExtension(path) : fileType;
			return detectedFileType switch
			{
				FileType.Plain => fileStream,
				FileType.GZip => new GZipStream(fileStream, CompressionMode.Decompress),
				FileType.Zip => await OpenZipStreamAsync(fileStream, cancellationToken).ConfigureAwait(false),
				_ => throw new NotSupportedException($"File type {detectedFileType} is not supported."),
			};
		}

		/// <summary>
		/// Opens the first entry from a ZIP file asynchronously.
		/// </summary>
		private static Task<Stream> OpenZipStreamAsync(Stream fileStream, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
			var entry = zipArchive.Entries.First();
			return Task.FromResult(entry.Open());
		}

		#region Convenience Methods

		/// <summary>
		/// Reads <see cref="ExtendedGeoName"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<ExtendedGeoName> ReadExtendedGeoNamesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ExtendedGeoNameParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="ExtendedGeoName"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<ExtendedGeoName> ReadExtendedGeoNamesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ExtendedGeoNameParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="GeoName"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="useExtendedFileFormat">Whether to use the extended file format.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<GeoName> ReadGeoNamesAsync(string filename, bool useExtendedFileFormat = true, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new GeoNameParser(useExtendedFileFormat), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="GeoName"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="useExtendedFileFormat">Whether to use the extended file format.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<GeoName> ReadGeoNamesAsync(Stream stream, bool useExtendedFileFormat = true, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new GeoNameParser(useExtendedFileFormat), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Admin1Code"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Admin1Code> ReadAdmin1CodesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new Admin1CodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Admin1Code"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Admin1Code> ReadAdmin1CodesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new Admin1CodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Admin2Code"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Admin2Code> ReadAdmin2CodesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new Admin2CodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Admin2Code"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Admin2Code> ReadAdmin2CodesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new Admin2CodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="AlternateName"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<AlternateName> ReadAlternateNamesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new AlternateNameParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="AlternateName"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<AlternateName> ReadAlternateNamesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new AlternateNameParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="AlternateNameV2"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<AlternateNameV2> ReadAlternateNamesV2Async(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new AlternateNameParserV2(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="AlternateNameV2"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<AlternateNameV2> ReadAlternateNamesV2Async(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new AlternateNameParserV2(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the built-in data.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Continent> ReadBuiltInContinentsAsync(CancellationToken cancellationToken = default)
		{
			return ReadBuiltInResourceAsync("continentCodes", new ContinentParser(), cancellationToken);
		}

		private static async IAsyncEnumerable<T> ReadBuiltInResourceAsync<T>(string name, IParser<T> parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var data = Properties.Resources.ResourceManager.GetString(name) ?? throw new ArgumentNullException(nameof(name), $"Resource with name {name} not found.");
			await using var memoryStream = new MemoryStream(parser.Encoding.GetBytes(data));
			await foreach (var record in new GeoFileReader().ReadRecordsAsync(memoryStream, parser, cancellationToken).ConfigureAwait(false))
			{
				yield return record;
			}
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Continent> ReadContinentsAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ContinentParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Continent"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Continent> ReadContinentsAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ContinentParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="CountryInfo"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<CountryInfo> ReadCountryInfoAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new CountryInfoParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="CountryInfo"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<CountryInfo> ReadCountryInfoAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new CountryInfoParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the built-in data.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<FeatureClass> ReadBuiltInFeatureClassesAsync(CancellationToken cancellationToken = default)
		{
			return ReadBuiltInResourceAsync("featureClasses_en", new FeatureClassParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<FeatureClass> ReadFeatureClassesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new FeatureClassParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="FeatureClass"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<FeatureClass> ReadFeatureClassesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new FeatureClassParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="FeatureCode"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<FeatureCode> ReadFeatureCodesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new FeatureCodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="FeatureCode"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<FeatureCode> ReadFeatureCodesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new FeatureCodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="HierarchyNode"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<HierarchyNode> ReadHierarchyAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new HierarchyParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="HierarchyNode"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<HierarchyNode> ReadHierarchyAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new HierarchyParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="ISOLanguageCode"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<ISOLanguageCode> ReadISOLanguageCodesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new ISOLanguageCodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="ISOLanguageCode"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<ISOLanguageCode> ReadISOLanguageCodesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new ISOLanguageCodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="TimeZone"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<TimeZone> ReadTimeZonesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new TimeZoneParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="TimeZone"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<TimeZone> ReadTimeZonesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new TimeZoneParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="UserTag"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<UserTag> ReadUserTagsAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new UserTagParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="UserTag"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<UserTag> ReadUserTagsAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new UserTagParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Postalcode"/> records asynchronously from the specified file.
		/// </summary>
		/// <param name="filename">The filename to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Postalcode> ReadPostalcodesAsync(string filename, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(filename, new PostalcodeParser(), cancellationToken);
		}

		/// <summary>
		/// Reads <see cref="Postalcode"/> records asynchronously from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public static IAsyncEnumerable<Postalcode> ReadPostalcodesAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			return new GeoFileReader().ReadRecordsAsync(stream, new PostalcodeParser(), cancellationToken);
		}

		#endregion Convenience Methods
	}
}