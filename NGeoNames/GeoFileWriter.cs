using NGeoNames.Composers;
using NGeoNames.Entities;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace NGeoNames
{
	/// <summary>
	/// Provides methods to write/compose geonames.org compatible files asynchronously.
	/// </summary>
	public class GeoFileWriter
	{
		/// <summary>
		/// The default separator used by the <see cref="GeoFileWriter"/> when writing records to a file/stream.
		/// </summary>
		public const string DEFAULTLINESEPARATOR = "\n";

		/// <summary>
		/// Writes records of type T asynchronously, using the specified composer to compose the file.
		/// </summary>
		/// <typeparam name="T">The type of objects to write/compose.</typeparam>
		/// <param name="path">The path of the file to read/write.</param>
		/// <param name="values">The values to write to the file.</param>
		/// <param name="composer">The <see cref="IComposer{T}"/> to use when writing the file.</param>
		/// <param name="lineseparator">The lineseparator to use (see <see cref="DEFAULTLINESEPARATOR"/>).</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task WriteRecordsAsync<T>(string path, IEnumerable<T> values, IComposer<T> composer, string lineseparator = DEFAULTLINESEPARATOR, CancellationToken cancellationToken = default)
		{
			await WriteRecordsAsync(path, values, composer, FileUtil.GetFileTypeFromExtension(path), lineseparator, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Writes records of type T asynchronously, using the specified composer to compose the file.
		/// </summary>
		/// <typeparam name="T">The type of objects to write/compose.</typeparam>
		/// <param name="path">The path of the file to read/write.</param>
		/// <param name="values">The values to write to the file.</param>
		/// <param name="filetype">The <see cref="FileType"/> of the file.</param>
		/// <param name="composer">The <see cref="IComposer{T}"/> to use when writing the file.</param>
		/// <param name="lineseparator">The lineseparator to use (see <see cref="DEFAULTLINESEPARATOR"/>).</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task WriteRecordsAsync<T>(string path, IEnumerable<T> values, IComposer<T> composer, FileType filetype, string lineseparator = DEFAULTLINESEPARATOR, CancellationToken cancellationToken = default)
		{
			using (var s = await GetStreamAsync(path, filetype, cancellationToken).ConfigureAwait(false))
			{
				await WriteRecordsAsync(s, values, composer, lineseparator, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Writes records of type T asynchronously, using the specified composer to compose the file.
		/// </summary>
		/// <typeparam name="T">The type of objects to write/compose.</typeparam>
		/// <param name="stream">The <see cref="Stream"/> to write to.</param>
		/// <param name="values">The values to write to the file.</param>
		/// <param name="composer">The <see cref="IComposer{T}"/> to use when writing the file.</param>
		/// <param name="lineseparator">The lineseparator to use (see <see cref="DEFAULTLINESEPARATOR"/>).</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task WriteRecordsAsync<T>(Stream stream, IEnumerable<T> values, IComposer<T> composer, string lineseparator = DEFAULTLINESEPARATOR, CancellationToken cancellationToken = default)
		{
			using (var w = new StreamWriter(stream, composer.Encoding))
			{
				foreach (var v in values)
				{
					cancellationToken.ThrowIfCancellationRequested();
					await w.WriteAsync(composer.Compose(v) + lineseparator).ConfigureAwait(false);
				}
			}
		}

		private static Task<Stream> GetStreamAsync(string path, FileType filetype, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var filestream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

			var writeAsType = filetype == FileType.AutoDetect ? FileUtil.GetFileTypeFromExtension(path) : filetype;
			return writeAsType switch
			{
				FileType.Plain => Task.FromResult<Stream>(filestream),
				FileType.GZip => Task.FromResult<Stream>(new GZipStream(filestream, CompressionLevel.Optimal)),
				_ => throw new System.NotSupportedException($"Filetype not supported: {writeAsType}")
			};
		}

		#region Convenience methods

		public static async Task WriteAdmin1CodesAsync(string filename, IEnumerable<Admin1Code> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new Admin1CodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAdmin1CodesAsync(Stream stream, IEnumerable<Admin1Code> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new Admin1CodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAdmin2CodesAsync(string filename, IEnumerable<Admin2Code> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new Admin2CodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAdmin2CodesAsync(Stream stream, IEnumerable<Admin2Code> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new Admin2CodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAlternateNamesAsync(string filename, IEnumerable<AlternateName> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new AlternateNameComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAlternateNamesAsync(Stream stream, IEnumerable<AlternateName> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new AlternateNameComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAlternateNamesV2Async(string filename, IEnumerable<AlternateNameV2> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new AlternateNameV2Composer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteAlternateNamesV2Async(Stream stream, IEnumerable<AlternateNameV2> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new AlternateNameV2Composer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteContinentsAsync(string filename, IEnumerable<Continent> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new ContinentComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteContinentsAsync(Stream stream, IEnumerable<Continent> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new ContinentComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteCountryInfoAsync(string filename, IEnumerable<CountryInfo> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new CountryInfoComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteCountryInfoAsync(Stream stream, IEnumerable<CountryInfo> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new CountryInfoComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteExtendedGeoNamesAsync(string filename, IEnumerable<ExtendedGeoName> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new ExtendedGeoNameComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteExtendedGeoNamesAsync(Stream stream, IEnumerable<ExtendedGeoName> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new ExtendedGeoNameComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteFeatureClassesAsync(string filename, IEnumerable<FeatureClass> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new FeatureClassComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteFeatureClassesAsync(Stream stream, IEnumerable<FeatureClass> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new FeatureClassComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteFeatureCodesAsync(string filename, IEnumerable<FeatureCode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new FeatureCodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteFeatureCodesAsync(Stream stream, IEnumerable<FeatureCode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new FeatureCodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteGeoNamesAsync(string filename, IEnumerable<GeoName> values, bool useExtendedFileFormat = false, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new GeoNameComposer(useExtendedFileFormat), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteGeoNamesAsync(Stream stream, IEnumerable<GeoName> values, bool useExtendedFileFormat = false, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new GeoNameComposer(useExtendedFileFormat), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteHierarchyAsync(string filename, IEnumerable<HierarchyNode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new HierarchyComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteHierarchyAsync(Stream stream, IEnumerable<HierarchyNode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new HierarchyComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteISOLanguageCodesAsync(string filename, IEnumerable<ISOLanguageCode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new ISOLanguageCodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteISOLanguageCodesAsync(Stream stream, IEnumerable<ISOLanguageCode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new ISOLanguageCodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteTimeZonesAsync(string filename, IEnumerable<TimeZone> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new TimeZoneComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteTimeZonesAsync(Stream stream, IEnumerable<TimeZone> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new TimeZoneComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteUserTagsAsync(string filename, IEnumerable<UserTag> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new UserTagComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WriteUserTagsAsync(Stream stream, IEnumerable<UserTag> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new UserTagComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WritePostalcodesAsync(string filename, IEnumerable<Postalcode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(filename, values, new PostalcodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		public static async Task WritePostalcodesAsync(Stream stream, IEnumerable<Postalcode> values, CancellationToken cancellationToken = default)
		{
			await new GeoFileWriter().WriteRecordsAsync(stream, values, new PostalcodeComposer(), DEFAULTLINESEPARATOR, cancellationToken).ConfigureAwait(false);
		}

		#endregion Convenience methods
	}
}