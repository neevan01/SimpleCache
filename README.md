# SimpleCache

A lightweight, fluent wrapper for IMemoryCache with automatic serialization, cache stampede prevention, error handling, and optional logging. Simplifies in-memory caching with a clean API.
**Note.** v1.0.0 provides in-memory caching only. Redis and distributed cache providers are planned for v2.0.0.

## Features

- ✅ .NET Standard 2.0 compatible (works with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- ✅ Fluent API for common cache operations
- ✅ Automatic serialization/deserialization
- ✅ Cache stampede prevention (locking)
- ✅ Optional logging support (Microsoft.Extensions.Logging)
- ✅ Structured result objects
- ✅ Error handling (never throws)
- ✅ Async/await throughout
- ✅ Key prefixing for namespacing
- ✅ Configurable default expiration

## Installation

```bash
dotnet add package SimpleCache
```

## Quick Start

### 1. Configure in Startup (ASP.NET Core)

```csharp
services.AddSimpleCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.KeyPrefix = "myapp";
    options.EnableSerialization = true;
});
```

### 2. Inject and Use

```csharp
public class ProductService
{
    private readonly ISimpleCache _cache;

    public ProductService(ISimpleCache cache)
    {
        _cache = cache;
    }

    public async Task<Product> GetProductAsync(int id)
    {
        // GetOrSet pattern - the most common use case
        return await _cache.GetOrSetAsync(
            $"product:{id}",
            async () => await _database.GetProductAsync(id),
            TimeSpan.FromMinutes(5)
        );
    }
}
```

## Usage Examples

### GetOrSet Pattern (Recommended)

```csharp
// Async factory
var user = await cache.GetOrSetAsync(
    "user:123",
    async () => await GetUserFromDatabase(123),
    TimeSpan.FromMinutes(5)
);

// Sync factory
var config = await cache.GetOrSetAsync(
    "config",
    () => LoadConfiguration(),
    TimeSpan.FromHours(1)
);
```

### Manual Get/Set

```csharp
// Set value
await cache.SetAsync("key", myObject, TimeSpan.FromMinutes(10));

// Get value
var result = await cache.GetAsync<MyObject>("key");
if (result.Success && result.FromCache)
{
    Console.WriteLine($"Got from cache: {result.Value}");
}

// Get or default
var value = await cache.GetOrDefaultAsync("key", defaultValue: new MyObject());

// Try get
var (success, value) = await cache.TryGetAsync<MyObject>("key");
if (success)
{
    Console.WriteLine($"Found: {value}");
}
```

### Cache Management

```csharp
// Check if exists
bool exists = await cache.ExistsAsync("key");

// Remove
await cache.RemoveAsync("key");

// Clear all (use with caution)
await cache.ClearAsync();
```

### Without Dependency Injection

```csharp
var memoryCache = new MemoryCache(new MemoryCacheOptions());
var provider = new MemoryCacheProvider(memoryCache);
var cache = new SimpleCacheService(provider);

var data = await cache.GetOrSetAsync("key", async () => await GetData());
```

## Configuration Options

```csharp
public class CacheOptions
{
    // Default expiration time (default: 5 minutes)
    public TimeSpan DefaultExpiration { get; set; }

    // Enable automatic serialization (default: true)
    public bool EnableSerialization { get; set; }

    // Cache key prefix for namespacing (default: empty)
    public string? KeyPrefix { get; set; }

    // Enable cache statistics tracking (default: false)
    public bool EnableStatistics { get; set; }
}
```

### Advanced Configuration

```csharp
services.AddSimpleCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
    options.KeyPrefix = "myapp";
    options.EnableSerialization = true;
    options.EnableStatistics = true;
});
```

## Logging

The library integrates seamlessly with `Microsoft.Extensions.Logging`:

```csharp
// In Startup.cs or Program.cs
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

services.AddSimpleCache();
```

Log output includes:

- Cache hits/misses
- Set/remove operations
- Errors and warnings
- Key information

If logging is not configured, the library works silently without errors.

## Error Handling

SimpleCache never throws exceptions. All operations return success/failure indicators:

```csharp
// GetOrSetAsync returns the value (executes factory on error)
var data = await cache.GetOrSetAsync("key", async () => await GetData());

// GetAsync returns structured result
var result = await cache.GetAsync<MyData>("key");
if (!result.Success)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}

// SetAsync/RemoveAsync return bool
bool success = await cache.SetAsync("key", value);
```

## Cache Stampede Prevention

SimpleCache automatically prevents cache stampede (thundering herd) using semaphore locking:

```csharp
// Multiple concurrent requests for the same key
// Only ONE will execute the factory, others will wait and get the cached result
await Task.WhenAll(
    cache.GetOrSetAsync("expensive", async () => await ExpensiveOperation()),
    cache.GetOrSetAsync("expensive", async () => await ExpensiveOperation()),
    cache.GetOrSetAsync("expensive", async () => await ExpensiveOperation())
);
// ExpensiveOperation() is called only ONCE
```

## Key Prefixing

Use key prefixes to namespace your cache entries:

```csharp
services.AddSimpleCache(options =>
{
    options.KeyPrefix = "myapp";
});

// Key "user:123" becomes "myapp:user:123" in cache
await cache.SetAsync("user:123", user);
```

## Best Practices

### 1. Use Descriptive Keys

```csharp
// Good
await cache.GetOrSetAsync($"product:{productId}", ...);
await cache.GetOrSetAsync($"user:{userId}:profile", ...);

// Bad
await cache.GetOrSetAsync("p1", ...);
await cache.GetOrSetAsync("data", ...);
```

### 2. Set Appropriate Expiration Times

```csharp
// Frequently changing data - short expiration
await cache.GetOrSetAsync("stock-price", factory, TimeSpan.FromSeconds(30));

// Rarely changing data - long expiration
await cache.GetOrSetAsync("country-list", factory, TimeSpan.FromHours(24));

// Static data - very long expiration
await cache.GetOrSetAsync("app-config", factory, TimeSpan.FromDays(1));
```

### 3. Use GetOrSet Pattern

```csharp
// Preferred - simple and safe
var data = await cache.GetOrSetAsync("key", async () => await GetData());

// Avoid - more code, same result
var result = await cache.GetAsync<Data>("key");
if (!result.Success || !result.FromCache)
{
    var data = await GetData();
    await cache.SetAsync("key", data);
    return data;
}
return result.Value;
```

## Performance

- **Cache Hit:** ~0.1ms (memory lookup + deserialization)
- **Cache Miss:** Factory execution time + ~0.2ms (serialization + storage)
- **Serialization:** Uses Newtonsoft.Json (can be disabled)
- **Locking:** Minimal overhead, only on cache misses

## Comparison with IMemoryCache

### Before (IMemoryCache)

```csharp
public async Task<User> GetUser(int id)
{
    var key = $"user:{id}";

    if (!_cache.TryGetValue(key, out User user))
    {
        user = await _database.GetUserAsync(id);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _cache.Set(key, user, options);
    }

    return user;
}
```

### After (SimpleCache)

```csharp
public async Task<User> GetUser(int id)
{
    return await _cache.GetOrSetAsync(
        $"user:{id}",
        async () => await _database.GetUserAsync(id),
        TimeSpan.FromMinutes(5)
    );
}
```

**Result:** 70% less code, more readable, safer.

## Development

### Running Tests

```bash
cd tests/SimpleCache.Tests
dotnet test
```

### Building the Package

```bash
cd src/SimpleCache
dotnet pack -c Release
```

## Roadmap

### v1.1.0 (Free)

- [ ] Cache statistics (hit rate, miss rate)
- [ ] Batch operations (GetMany, SetMany)
- [ ] Cache warming

### v2.0.0 (Premium Providers)

- [ ] Redis provider (IDistributedCache)
- [ ] SQL Server provider
- [ ] Distributed cache abstraction

## License

MIT

## Support

- GitHub Issues: Report bugs and request features
- Documentation: Full API documentation available
- Examples: Check the samples folder

---

**Made with ❤️ for the .NET community - Nabin Ghimire**
