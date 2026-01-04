# Changelog

All notable changes to NGeoNamesCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2026-01-04

### Added
- CancellationToken support for all async operations (GeoFileReader, GeoFileWriter, GeoFileDownloader)
- `GeoFileDownloader` constructor overload accepting `HttpClient` for dependency injection scenarios
- Memory<T> and Span<T> optimizations in parsing operations
- ConfigureAwait(false) on all await statements
- Shared HttpClient instance with optimized SocketsHttpHandler configuration

### Changed
- All async methods now accept optional `CancellationToken` parameter (backward compatible)
- Improved string parsing using zero-allocation operations where possible
- HttpClient lifecycle management to prevent socket exhaustion

### Removed
- `ReverseGeoCode<T>.AddAsync()` (use `Add()` instead)
- `ReverseGeoCode<T>.AddRangeAsync()` (use `AddRange()` instead)
- `ReverseGeoCode<T>.BalanceAsync()` (use `Balance()` instead)
- `ReverseGeoCode<T>.RadialSearchAsync()` (use `RadialSearch()` instead)
- `ReverseGeoCode<T>.NearestNeighbourSearchAsync()` (use `NearestNeighbourSearch()` instead)

**Note:** The removed methods were wrappers around CPU-bound synchronous operations. Use the synchronous methods directly, or wrap them in `Task.Run()` if needed for background processing.


## [1.6.2] - Previous Release
- .NET 8 compatibility
- Bug fixes and improvements

