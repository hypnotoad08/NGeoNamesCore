using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;

namespace NGeoNames
{
	/// <summary>
	/// Provides methods to download files from geonames.org asynchronously.
	/// </summary>
	public class GeoFileDownloader
	{
		public static readonly Uri DEFAULTGEOFILEBASEURI = new Uri("http://download.geonames.org/export/dump/", UriKind.Absolute);
		public static readonly Uri DEFAULTPOSTALCODEBASEURI = new Uri("http://download.geonames.org/export/zip/", UriKind.Absolute);
		public static readonly string USERAGENT = $"{typeof(GeoFileDownloader).Assembly.GetName().Name} v{typeof(GeoFileDownloader).Assembly.GetName().Version}";

		public Uri BaseUri { get; set; }
		public RequestCachePolicy CachePolicy { get; set; }
		public ICredentials Credentials { get; set; }
		public IWebProxy Proxy { get; set; }
		public TimeSpan DefaultTTL { get; set; }

		public GeoFileDownloader(Uri baseUri) : this(baseUri, TimeSpan.FromHours(24))
		{
		}

		public GeoFileDownloader(Uri baseUri, TimeSpan ttl)
		{
			BaseUri = baseUri;
			DefaultTTL = ttl;
		}

		public static GeoFileDownloader CreateGeoFileDownloader()
		{
			return new GeoFileDownloader(DEFAULTGEOFILEBASEURI);
		}

		public static GeoFileDownloader CreateGeoFileDownloader(TimeSpan ttl)
		{
			return new GeoFileDownloader(DEFAULTGEOFILEBASEURI, ttl);
		}

		public static GeoFileDownloader CreatePostalcodeDownloader()
		{
			return new GeoFileDownloader(DEFAULTPOSTALCODEBASEURI);
		}

		public static GeoFileDownloader CreatePostalcodeDownloader(TimeSpan ttl)
		{
			return new GeoFileDownloader(DEFAULTPOSTALCODEBASEURI, ttl);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously.
		/// </summary>
		public async Task<string[]> DownloadFileAsync(string uri, string destinationpath)
		{
			return await DownloadFileAsync(new Uri(uri, UriKind.RelativeOrAbsolute), destinationpath);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		public async Task<string[]> DownloadFileAsync(Uri uri, string destinationpath)
		{
			return await DownloadFileWhenOlderThanAsync(uri, destinationpath, DefaultTTL);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		public async Task<string[]> DownloadFileWhenOlderThanAsync(string uri, string destinationpath, TimeSpan ttl)
		{
			return await DownloadFileWhenOlderThanAsync(new Uri(uri, UriKind.RelativeOrAbsolute), destinationpath, ttl);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		public async Task<string[]> DownloadFileWhenOlderThanAsync(Uri uri, string destinationpath, TimeSpan ttl)
		{
			var downloaduri = DetermineDownloadPath(uri);
			destinationpath = DetermineDestinationPath(downloaduri, destinationpath);

			if (await IsFileExpiredAsync(destinationpath, ttl))
			{
				using var httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
				httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(USERAGENT);
				var response = await httpClient.GetAsync(downloaduri);
				response.EnsureSuccessStatusCode();
				var fileBytes = await response.Content.ReadAsByteArrayAsync();
				await File.WriteAllBytesAsync(destinationpath, fileBytes);
			}

			if (Path.GetExtension(destinationpath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
				return await UnzipFilesAsync(destinationpath, ttl);
			return new[] { destinationpath };
		}

		private async Task<bool> IsFileExpiredAsync(string path, TimeSpan ttl)
		{
			var fileExists = File.Exists(path);
			return !fileExists || (DateTime.UtcNow - (await Task.Run(() => new FileInfo(path).LastWriteTimeUtc))) > ttl;
		}

		private async Task<string[]> UnzipFilesAsync(string path, TimeSpan ttl)
		{
			var files = new List<string>();

			using (var f = File.OpenRead(path))
			using (var z = new ZipArchive(f, ZipArchiveMode.Read))
			{
				foreach (var entry in z.Entries.Where(n => !n.Name.StartsWith("readme", StringComparison.OrdinalIgnoreCase)))
				{
					var dest = Path.Combine(Path.GetDirectoryName(path), entry.Name);
					if (await IsFileExpiredAsync(dest, ttl))
					{
						using var entryStream = entry.Open();
						using var e = File.Create(dest);
						await entryStream.CopyToAsync(e);
					}
					files.Add(dest);
				}
			}

			return files.ToArray();
		}

		private Uri DetermineDownloadPath(Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				return new Uri(BaseUri, uri.OriginalString);
			return uri;
		}

		private static string DetermineDestinationPath(Uri uri, string path)
		{
			if (Directory.Exists(path))
				path = Path.Combine(path, Path.GetFileName(uri.AbsolutePath));
			return path;
		}
	}
}