# ![Logo](https://raw.githubusercontent.com/RobThree/NGeoNames/master/icon.png) NGeoNames

Inspired by [OfflineReverseGeocode](https://github.com/AReallyGoodName/OfflineReverseGeocode) found at [this Reddit post](http://www.reddit.com/r/programming/comments/281msj/). Uses [KdTree](https://github.com/codeandcats/KdTree).

This library provides classes for downloading and parsing [files from GeoNames.org](download.geonames.org/export/dump/) and provides (reverse) geocoding methods like `NearestNeighbourSearch()` and `RadialSearch()` on the downloaded dataset(s).

This library is available as [NuGet package](https://www.nuget.org/packages/NGeoNames/).

## Basic usage / example / "quick start"

```c#
var datadirectory = @"D:\test\geo\";

// Download file (optional; you can point a GeoFileReader to existing files ofcourse)
var downloader = new GeoFileDownloader();
downloader.DownloadFile("NL.txt", datadirectory);    // Download NL.txt to D:\test\geo\

// Read NL.txt file to memory
var cities = GeoFileReader.ReadExtendedGeoNames(Path.Combine(datadirectory, "NL.txt")).ToArray();   // "Materialize" file to memory by calling ToArray()

// We're going to use Amsterdam as "search-center"
var amsterdam = cities.Where(n => n.Name.Equals("Amsterdam", StringComparison.OrdinalIgnoreCase) && n.FeatureCode.Equals("PPLC")).First();

// Find first 50 items of interest closest to our center
var reversegeocoder = new ReverseGeoCode<ExtendedGeoName>(cities);
var results = reversegeocoder.RadialSearch(amsterdam, 250);  // Locate 250 geo-items near the center of Amsterdam
//Print the results
foreach (var r in results)
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}, {1} {2} ({3:F4}Km)", r.Latitude, r.Longitude, r.Name, r.DistanceTo(amsterdam)));
```

## Overview

The library provides for the following main operations:

1. [Downloading / retrieving data from geonames.org](#downloading)
2. [Reading / parsing geonames.org data](#parsing)
3. [Utilizing geonames.org data](#utilizing)

### <a name="downloading"></a>Downloading / retrieving data from geonames.org

{documentation and code samples to follow}

### <a name="parsing"></a>Reading / parsing geonames.org data

{documentation and code samples to follow}

### <a name="utilizing"></a>Utilizing geonames.org data

{documentation and code samples to follow}

## Project status

Currently I'm adding (better) unittests. These will also serve a second purpose of demonstrating the usage of the library. As soon as I'm happy with the unittests I will add more documentation to this readme.

[![Build status](https://ci.appveyor.com/api/projects/status/mkmbxvm1w0mxaifv)](https://ci.appveyor.com/project/RobIII/ngeonames)
