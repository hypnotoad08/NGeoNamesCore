
# ![Logo](https://raw.githubusercontent.com/Hypnotoad08/NGeoNamesCore/master/icon.png) NGeoNamesCore

This project is a fork of the original [NGeoNames](https://github.com/RobThree/NGeoNames) by RobThree, which has been modified to be compatible with .NET Core and .NET 6+, and now fully supports **.Net 8**

### Changes in this fork:
- Migrated from .NET Framework 4.5 to .NET Core / .NET 6+.
- Now compatible with .Net 8
- Renamed the project to **NGeoNamesCore** to reflect .NET Core compatibility.
- Updated dependencies and APIs to support .NET Core and the latest .NET standards.
- All methods have been updated to follow async patterns for better scalability.

Inspired by [OfflineReverseGeocode](https://github.com/AReallyGoodName/OfflineReverseGeocode) found in [this Reddit post](http://www.reddit.com/r/programming/comments/281msj/). You may also be interested in [GeoSharp](https://github.com/Necrolis/GeoSharp). Uses [KdTree](https://github.com/codeandcats/KdTree).

This library provides classes for asynchronously downloading, reading, parsing, writing, and composing [files from GeoNames.org](http://download.geonames.org/export/dump/) and offers (reverse) geocoding methods such as `NearestNeighbourSearchAsync()` and `RadialSearchAsync()` on the downloaded dataset(s).

The library is now available as a [NuGet package](https://www.nuget.org/packages/NGeoNamesCore/) with .NET Core and .NET 8 compatibility.


## Basic usage / example / "quick start"

```c#
var datadir = @"D:\test\geo\";

// Download file (optional; you can point a GeoFileReader to existing files of course)
var downloader = GeoFileDownloader.CreateGeoFileDownloader();
await downloader.DownloadFileAsync("NL.zip", datadir);    // Zipfile will be automatically extracted

// Read NL.txt file to memory (NL = ISO3166-2:The Netherlands)
var nldata = await GeoFileReader.ReadExtendedGeoNamesAsync(Path.Combine(datadir, "NL.txt")).ToArrayAsync();
// Note: we "Materialize" the file to memory by calling ToArrayAsync()

// We're going to use Amsterdam as "search-center"
var amsterdam = nldata.Where(n =>
        n.Name.Equals("Amsterdam", StringComparison.OrdinalIgnoreCase)
        && n.FeatureCode.Equals("PPLC")
    ).First();

// Initialize a reversegeocoder with our geo-items from The Netherlands
var reversegeocoder = new ReverseGeoCode<ExtendedGeoName>(nldata);
// Locate 250 geo-items near the center of Amsterdam using async method
var results = await reversegeocoder.RadialSearchAsync(amsterdam, 250);  
// Print the results
foreach (var r in results) {
    Console.WriteLine(
        string.Format(
            CultureInfo.InvariantCulture, "{0}, {1} {2} ({3:F4}Km)",
            r.Latitude, r.Longitude, r.Name, r.DistanceTo(amsterdam)
        )
    );
}
```

## Overview

The library provides for the following main operations:

1. [Downloading / retrieving data from geonames.org](#downloading) (Optional)
2. [Reading / parsing geonames.org data](#parsing)
3. [Utilizing geonames.org data](#utilizing)
4. [Writing / composing geonames.org data](#composing)

The library consists mainly of parsers, composers, and entities (in their respective namespaces) and a `GeoFileReader` and `GeoFileWriter` to read/parse and write/compose geonames.org compatible files, a `GeoFileDownloader` to retrieve files from geonames.org, and a `ReverseGeoCode<T>` class to do the heavy lifting of the reverse geocoding itself.

Because some "geoname files" can be very large (like `allcountries.txt`), we have a `GeoName` entity, which is a simplified version (and base class) of an `ExtendedGeoName`. The `GeoName` class contains a unique id, which can be used to resolve the `ExtendedGeoName` easily for more information when required. It is, however, recommended to use `<countrycode>.txt` (e.g., `GB.txt`) `cities15000.txt` or `cities1000.txt`, for example, to reduce the dataset to a smaller size. You can also compose your own custom datasets using the `GeoFileWriter` and composers.

Also worth noting is that the readers return an `IAsyncEnumerable<SomeEntity>`; make sure that you materialize these async enumerables to a list, array, or other datastructure (using `.ToListAsync()`, `.ToArrayAsync()`, `.ToDictionaryAsync()`, etc.) if you access it more than once to avoid file I/O to the underlying file each time you access the data.

### <a name="downloading"></a>Downloading / retrieving data from geonames.org (Optional)

To download files from geonames.org, you can use the `GeoFileDownloader` class, which is, in essence, a wrapper for a basic [`WebClient`](http://msdn.microsoft.com/en-us/library/system.net.webclient.aspx). The simplest form is:

```c#
// Downloads (and extracts) geoname data in NL.zip from geonames.org
await GeoFileDownloader.CreateGeoFileDownloader()
    .DownloadFileAsync("NL.zip", @"D:\my\geodata\geo");
    
// Downloads (and extracts) postal code data in NL.zip from geonames.org
await GeoFileDownloader.CreatePostalcodeDownloader()
    .DownloadFileAsync("NL.zip", @"D:\my\geodata\postalcode");
```

You can specify the BaseUri in the `GeoFileDownloader` constructor or pass an absolute URL to the `DownloadFileAsync()` method if you want to use another location than the default `http://download.geonames.org/export/dump/`. The static 'factory methods'  `CreateGeoFileDownloader()` and `CreatePostalcodeDownloader()` are the easiest way to create a `GeoFileDownloader`; these use the built-in values for the BaseUri. The `GeoFileDownloader` has properties to set a (HTTP) `CachePolicy`, `Proxy`, and `Credentials` to use when downloading the file. The filedownloader, by default, downloads a file only if the destination file doesn't exist *or* when the destination file has "expired" (by default 24 hours). It uses the file's CreationDate to determine when the file was downloaded and if a newer version should be downloaded. The "TTL", how long a file will be 'valid', can be set using the `DefaultTTL` property of the `GeoFileDownloader` class. You can also use the `DownloadFileWhenOlderThan()` method which allows you to explicitly set a TTL. When a filename is specified (e.g. `d:\folder\foo.txt`) the file will be named accordingly.

ZIP files are automatically extracted in the destination folder; the original zip file is preserved because the `GeoFileDownloader` needs to know which files are supposed to be in the zip file and thus in the destination directory in their extracted form.

### <a name="parsing"></a>Reading / parsing geonames.org data

Once files are downloaded using the `GeoFileDownloader`, *or* by using your own custom/specific implementation, the files can be accessed using the `GeoFileReader` class. This class contains a number of static "convenience methods" like `ReadGeoNamesAsync()` and its "sibling" `ReadExtendedGeoNamesAsync()`. but also `ReadCountryInfoAsync()`, `ReadAlternateNamesAsync()`, etc. There is a "convenience method" for each entity.

```c#
// Open file "cities1000.txt" and retrieve only cities in the US
var cities_in_us = await GeoFileReader.ReadExtendedGeoNamesAsync(@"D:\my\geodata\cities1000.txt")
        .Where(p => p.CountryCode.Equals("US", StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Name).ToListAsync();
```

Again, **please note** that `Read<Something>Async` methods return an `IAsyncEnumerable<T>`. Whenever you want to access the data more than once, you will probably want to call `.ToArrayAsync()` or similar to materialize the data into memory. The `GeoFileReader` class has two static methods (`ReadBuiltInContinentsAsync()` and `ReadBuiltInFeatureClassesAsync()`) that can be used to use built-in values for continents and [feature codes](http://www.geonames.org/export/codes.html), which are not provided by g...

You can also add your own entities and, as long as you provide a parser for it, use the `GeoFileReader` class to read/parse files for these entities as well:

```c#
var data = new GeoFileReader().ReadRecordsAsync<MyEntity>("d:\foo\bar.txt", new MyEntityParser());
```

As long as your parser implements `IParser<MyEntity>`, you're good to go. A parser can skip a fixed number of lines in a file (for example, a 'header' record), skip comments (for example, lines starting with `#`), and you can even specify the encoding to use etc. Examples and more information can be found in the unit tests.

Another thing to note is that the `GeoFileReader` will try to "autodetect" if the file is a plain text file (`.txt` extension) or a GZipped file (`.gz` extension). Support for GZip was added to keep the footprint of the files lower when desired. This will, however, trade off I/O speed and CPU load for space. The `ReadRecordsAsync<T>()` method has an overload where you can explicitly specify the type of the file (should you want to use your own file extensions like `.dat` for example).

The `GeoFileReader` also supports the use of [`Stream`](http://msdn.microsoft.com/en-us/library/system.io.stream.aspx)s so you can provide data from a MemoryStream for example or any other source that can be wrapped in a stream.

As you'll probably realize by now, the `GeoFileReader` class *combined* with [LINQ](http://msdn.microsoft.com/en-us/library/bb397926.aspx) allows for very powerful querying, filtering and sorting of the data. Combine it with the `GeoFileWriter` to persist custom datasets (custom "materialized views") and the sky is the limit.

### <a name="utilizing"></a>Utilizing geonames.org data

The 'heart' of the library is the `ReverseGeoCode<T>` class. When you supply it with either `IEnumerable<GeoNames>` or `IEnumerable<ExtendedGeoNames>`, it can be used to do a `RadialSearchAsync()` or `NearestNeighbourSearchAsync()`. Supplying the class with data can be done by either passing it to the class constructor or by using the `AddAsync()` or `AddRangeAsync()` methods. You may want to call the `BalanceAsync()` method to balance the internal KD-tree. This is done automatically when the data is supplied via the constructor.

```c#
// Create our ReverseGeoCode class and supply it with data
var r = new ReverseGeoCode<ExtendedGeoName>(
        await GeoFileReader.ReadExtendedGeoNamesAsync(@"D:oo\cities1000.txt").ToListAsync()
    );

// Create a point from a lat/long pair from which we want to conduct our search(es) (center)
var new_york = r.CreateFromLatLong(40.7056308, -73.9780035);

// Find 10 nearest
await r.NearestNeighbourSearchAsync(new_york, 10);
```

Ofcourse there's no need to dabble with lat/long at all:

```c#
// Read data into memory
var data = (await GeoFileReader.ReadExtendedGeoNamesAsync(@"D:oo\cities1000.txt"))
        .ToDictionary(p => p.Id);

// Find New York by it's geoname ID (O(1) lookup)
var new_york = data[5128581];

// Find 10 nearest
var r = new ReverseGeoCode<ExtendedGeoName>(data.Values);
await r.NearestNeighbourSearchAsync(new_york, 10);
```

Or simply find by name:


```c#
// Read data into memory
var data = (await GeoFileReader.ReadExtendedGeoNamesAsync(@"D:oo\cities1000.txt"))
        .ToArray();

// Find New York by it's name (linear search, O(n))
var new_york = data.Where(p => p.Name.Equals("New York City")).First();

// Find 10 nearest
var r = new ReverseGeoCode<ExtendedGeoName>(data);
await r.NearestNeighbourSearchAsync(new_york, 10);
```
Depending on how you want to search/use the underlying data, you may want to use other, more optimal, data structures than demonstrated above. It's up to you!

Note that the library is based on the [**International System of Units (SI)**](http://en.wikipedia.org/wiki/International_System_of_Units); units of distance are specified in **meters**. If you want to use the imperial system (e.g. miles, nautical miles, yards) you need to convert to/from meters. The `GeoUtil` class provides helper-methods for converting miles/yards to meters and vice versa.

The `GeoName` class (and, by extension, the `ExtendedGeoName` class) has a `DistanceTo()` method which can be used to determine the exact distance between two points.

Both the `NearestNeighbourSearch()` and `RadialSearch()` methods have some overloads that accept lat/long pairs as *doubles* as well.
### <a name="composing"></a>Writing / composing geonames.org data

The `NGeoNames.Composers` namespace holds composers (the opposite of parsers) to enable you to write geoname.org data files. For this, you can use the `GeoNameFileWriter` class which, like the `GeoNameFileReader` class, has generic methods for writing records (`WriteRecordsAsync<T>`) and static "convenience methods" to write specific entities to a file.

Below is an example of what this would look like (with an extra filter added to filter out records with `population < 1000`):

```c#
// Filter 'allcountries.txt' to only BE, NL, LU entries with a population of >= 1000
await GeoFileWriter.WriteExtendedGeoNamesAsync(@"d:\foo\benelux1000.txt",
   (await GeoFileReader.ReadExtendedGeoNamesAsync(@"d:\foo\allcountries.txt"))
      .Where(e => new[] { "BE", "NL", "LU" }.Contains(e.CountryCode) && e.Population >= 1000)
      .OrderBy(e => e.CountryCode).ThenBy(e => e.Name)
);


// ...or...

// Join BE, NL en LU datasets, filter records with a population of >= 1000
await GeoFileWriter.WriteExtendedGeoNamesAsync(@"d:\foo\benelux1000.txt",
   (await GeoFileReader.ReadExtendedGeoNamesAsync(@"d:\foo\BE.txt"))
      .Union(await GeoFileReader.ReadExtendedGeoNamesAsync(@"d:\foo\NL.txt"))
      .Union(await GeoFileReader.ReadExtendedGeoNamesAsync(@"d:\foo\LU.txt"))
        .Where(e => e.Population >= 1000)
        .OrderBy(e => e.CountryCode).ThenBy(e => e.Name)
);


```
### A word about "extended format"

The `GeoNamesReader` and `GeoNamesWriter`, as well as the (Extended)GeoName parsers/composers, always assume the `ExtendedGeoName` format unless explicitly specified. The parameter **extendedfileformat** may pop up on some method overloads. Whenever this parameter is passed `false`, the class will assume a 'simple' (or non-extended) format with only 4 fields of data: Id, Name, Latitude, and Longitude. This format is more compact.

## Help

This fork comes with updated documentation for **NGeoNamesCore** to reflect changes for .NET Core and .NET 8 compatibility.

You can explore the code directly as it is richly commented. Updated help files and documentation may be included in future releases.

For more information on usage, refer to the examples in this `README.md` or the original [NGeoNames repository](https://github.com/RobThree/NGeoNames).

## Project status

This is a fork of the original [NGeoNames](https://github.com/RobThree/NGeoNames) and has been renamed to **NGeoNamesCore** to reflect compatibility with .NET Core and .NET 8.
The project will be updated as needed to ensure compatibility with newer versions of .NET, and I welcome contributions. If you're interested in contributing to this library, feel free to submit a pull request.

If you encounter any issues, please [open an issue](https://github.com/Hypnotoad08/NGeoNamesCore/issues).

<a href="https://www.nuget.org/packages/NGeoNamesCore/"><img src="https://img.shields.io/nuget/v/NGeoNamesCore" alt="NuGet version" height="18"></a>


## License

This project is licensed under the MIT license. See [LICENSE](LICENSE) for details.

The original project is licensed under the same MIT license, and all credits to the original author remain intact.

[Logo / icon](http://www.iconninja.com/earth-search-internet-icon-44388) sourced from iconninja.com ([Archived page](http://riii.me/rftqo))

### NuGet Package

The original NuGet package for **NGeoNames** can be found [here](https://www.nuget.org/packages/NGeoNames/).

This forked version is now available as **NGeoNamesCore** on [NuGet.org](https://www.nuget.org/packages/NGeoNamesCore/), with the package reflecting updates to .NET Core, .NET 8, and fully async methods.
