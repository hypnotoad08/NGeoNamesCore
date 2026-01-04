using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NGeoNames
{
	/// <summary>
	/// Provides methods to download files from geonames.org asynchronously.
	/// </summary>
	public class GeoFileDownloader : IDisposable
	{
		public static readonly Uri DEFAULTGEOFILEBASEURI = new Uri("http://download.geonames.org/export/dump/", UriKind.Absolute);
		public static readonly Uri DEFAULTPOSTALCODEBASEURI = new Uri("http://download.geonames.org/export/zip/", UriKind.Absolute);
		public static readonly string USERAGENT = $"{typeof(GeoFileDownloader).Assembly.GetName().Name} v{typeof(GeoFileDownloader).Assembly.GetName().Version}";

		private static readonly Lazy<HttpClient> SharedHttpClient = new Lazy<HttpClient>(() =>
		{
			var client = new HttpClient(new SocketsHttpHandler
			{
				PooledConnectionLifetime = TimeSpan.FromMinutes(5),
				PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
			});
			client.DefaultRequestHeaders.UserAgent.ParseAdd(USERAGENT);
			client.Timeout = TimeSpan.FromMinutes(10);
			return client;
		});

		private readonly HttpClient _httpClient;
		private readonly bool _shouldDisposeHttpClient;

		public Uri BaseUri { get; set; }
		public RequestCachePolicy CachePolicy { get; set; }
		public ICredentials Credentials { get; set; }
		public IWebProxy Proxy { get; set; }
		public TimeSpan DefaultTTL { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeoFileDownloader"/> class with the specified base URI.
		/// Uses a shared, reusable HttpClient for better performance.
		/// </summary>
		/// <param name="baseUri">The base URI for downloading files.</param>
		public GeoFileDownloader(Uri baseUri) : this(baseUri, TimeSpan.FromHours(24))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeoFileDownloader"/> class with the specified base URI and TTL.
		/// Uses a shared, reusable HttpClient for better performance.
		/// </summary>
		/// <param name="baseUri">The base URI for downloading files.</param>
		/// <param name="ttl">The default time-to-live for cached files.</param>
		public GeoFileDownloader(Uri baseUri, TimeSpan ttl)
		{
			BaseUri = baseUri;
			DefaultTTL = ttl;
			_httpClient = SharedHttpClient.Value;
			_shouldDisposeHttpClient = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeoFileDownloader"/> class with a custom HttpClient.
		/// This constructor is useful for dependency injection scenarios or when you need custom HttpClient configuration.
		/// </summary>
		/// <param name="baseUri">The base URI for downloading files.</param>
		/// <param name="httpClient">A custom HttpClient instance to use for downloads.</param>
		/// <param name="ttl">The default time-to-live for cached files.</param>
		/// <remarks>
		/// When using this constructor, the caller is responsible for managing the HttpClient lifetime.
		/// The HttpClient will NOT be disposed when this instance is disposed.
		/// </remarks>
		public GeoFileDownloader(Uri baseUri, HttpClient httpClient, TimeSpan ttl = default)
		{
			BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			DefaultTTL = ttl == default ? TimeSpan.FromHours(24) : ttl;
			_shouldDisposeHttpClient = false;
		}

		public static GeoFileDownloader CreateGeoFileDownloader()
		{
			return new GeoFileDownloader(DEFAULTGEOFILEBASEURI);
		}

		public static GeoFileDownloader CreateGeoFileDownloader(TimeSpan ttl)
		{
			return new GeoFileDownloader(DEFAULTGEOFILEBASEURI, ttl);
		}

		/// <summary>
		/// Creates a new GeoFileDownloader with a custom HttpClient for dependency injection scenarios.
		/// </summary>
		/// <param name="httpClient">The HttpClient to use for downloads.</param>
		/// <param name="ttl">The default time-to-live for cached files.</param>
		/// <returns>A new GeoFileDownloader instance.</returns>
		public static GeoFileDownloader CreateGeoFileDownloader(HttpClient httpClient, TimeSpan ttl = default)
		{
			return new GeoFileDownloader(DEFAULTGEOFILEBASEURI, httpClient, ttl);
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
		/// Creates a new PostalcodeDownloader with a custom HttpClient for dependency injection scenarios.
		/// </summary>
		/// <param name="httpClient">The HttpClient to use for downloads.</param>
		/// <param name="ttl">The default time-to-live for cached files.</param>
		/// <returns>A new GeoFileDownloader instance.</returns>
		public static GeoFileDownloader CreatePostalcodeDownloader(HttpClient httpClient, TimeSpan ttl = default)
		{
			return new GeoFileDownloader(DEFAULTPOSTALCODEBASEURI, httpClient, ttl);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously.
		/// </summary>
		/// <param name="uri">The URI of the file to download.</param>
		/// <param name="destinationpath">The destination path where the file should be saved.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task<string[]> DownloadFileAsync(string uri, string destinationpath, CancellationToken cancellationToken = default)
		{
			return await DownloadFileAsync(new Uri(uri, UriKind.RelativeOrAbsolute), destinationpath, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		/// <param name="uri">The URI of the file to download.</param>
		/// <param name="destinationpath">The destination path where the file should be saved.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task<string[]> DownloadFileAsync(Uri uri, string destinationpath, CancellationToken cancellationToken = default)
		{
			return await DownloadFileWhenOlderThanAsync(uri, destinationpath, DefaultTTL, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		/// <param name="uri">The URI of the file to download.</param>
		/// <param name="destinationpath">The destination path where the file should be saved.</param>
		/// <param name="ttl">The time-to-live for the cached file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task<string[]> DownloadFileWhenOlderThanAsync(string uri, string destinationpath, TimeSpan ttl, CancellationToken cancellationToken = default)
		{
			return await DownloadFileWhenOlderThanAsync(new Uri(uri, UriKind.RelativeOrAbsolute), destinationpath, ttl, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Downloads the specified file to the destination path asynchronously using the specified TTL.
		/// </summary>
		/// <param name="uri">The URI of the file to download.</param>
		/// <param name="destinationpath">The destination path where the file should be saved.</param>
		/// <param name="ttl">The time-to-live for the cached file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		public async Task<string[]> DownloadFileWhenOlderThanAsync(Uri uri, string destinationpath, TimeSpan ttl, CancellationToken cancellationToken = default)
		{
			var downloaduri = DetermineDownloadPath(uri);
			destinationpath = DetermineDestinationPath(downloaduri, destinationpath);

			if (await IsFileExpiredAsync(destinationpath, ttl, cancellationToken).ConfigureAwait(false))
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, downloaduri);
				request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
				
				var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				
				var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
				await File.WriteAllBytesAsync(destinationpath, fileBytes, cancellationToken).ConfigureAwait(false);
			}

			if (Path.GetExtension(destinationpath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
				return await UnzipFilesAsync(destinationpath, ttl, cancellationToken).ConfigureAwait(false);
			return new[] { destinationpath };
		}

		private static async Task<bool> IsFileExpiredAsync(string path, TimeSpan ttl, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var fileExists = File.Exists(path);
			return !fileExists || (DateTime.UtcNow - (await Task.Run(() => new FileInfo(path).LastWriteTimeUtc, cancellationToken).ConfigureAwait(false))) > ttl;
		}

		private static async Task<string[]> UnzipFilesAsync(string path, TimeSpan ttl, CancellationToken cancellationToken = default)
		{
			var files = new List<string>();

			using (var f = File.OpenRead(path))
			using (var z = new ZipArchive(f, ZipArchiveMode.Read))
			{
				foreach (var entry in z.Entries.Where(n => !n.Name.StartsWith("readme", StringComparison.OrdinalIgnoreCase)))
				{
					cancellationToken.ThrowIfCancellationRequested();
					var dest = Path.Combine(Path.GetDirectoryName(path), entry.Name);
					if (await IsFileExpiredAsync(dest, ttl, cancellationToken).ConfigureAwait(false))
					{
						using var entryStream = entry.Open();
						using var e = File.Create(dest);
						await entryStream.CopyToAsync(e, cancellationToken).ConfigureAwait(false);
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

		/// <summary>
		/// Disposes resources used by the GeoFileDownloader.
		/// Note: The shared HttpClient is NOT disposed, only custom HttpClients provided via constructor.
		/// </summary>
		public void Dispose()
		{
			if (_shouldDisposeHttpClient)
			{
				_httpClient?.Dispose();
			}
		}
	}
}