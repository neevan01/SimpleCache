# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025/12/15

### Added

- Initial release
- GetOrSetAsync pattern for automatic cache management
- Manual Get/Set/Remove operations
- Cache stampede prevention using semaphore locking
- Automatic JSON serialization/deserialization
- Optional logging support via Microsoft.Extensions.Logging
- Structured CacheResult objects
- Error handling (never throws exceptions)
- Key prefixing for namespacing
- Configurable default expiration
- Extension methods for fluent API
- .NET Standard 2.0 support
- Comprehensive documentation and samples

### Features

- IMemoryCache provider (in-memory caching)
- Async/await throughout
- Dependency injection support
- Zero-config defaults
- Thread-safe operations
