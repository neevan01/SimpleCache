using SimpleCache;
using SimpleCache.Extensions;
using SimpleCache.Models;
using SimpleCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

Console.WriteLine("=== SimpleCache Demo ===\n");

// Setup
var memoryCache = new MemoryCache(new MemoryCacheOptions());
var options = new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(5),
    KeyPrefix = "demo",
    EnableSerialization = true
};

var provider = new MemoryCacheProvider(memoryCache, options.EnableSerialization);
var cache = new SimpleCacheService(provider, options);

// Example 1: GetOrSet Pattern (Most Common)
Console.WriteLine("=== Example 1: GetOrSet Pattern ===");
var user = await cache.GetOrSetAsync(
    "user:1",
    async () =>
    {
        Console.WriteLine("  → Fetching from 'database'...");
        await Task.Delay(100); // Simulate DB call
        return new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
    },
    TimeSpan.FromMinutes(5)
);
Console.WriteLine($"  ✓ User: {user.Name} ({user.Email})");

// Second call - should hit cache
Console.WriteLine("\n  Calling again (should hit cache)...");
var userCached = await cache.GetOrSetAsync(
    "user:1",
    async () =>
    {
        Console.WriteLine("  → This should NOT print!");
        return new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
    }
);
Console.WriteLine($"  ✓ User from cache: {userCached.Name}");

// Example 2: Manual Get/Set
Console.WriteLine("\n=== Example 2: Manual Get/Set ===");
var product = new Product { Id = 100, Name = "Laptop", Price = 999.99m };
var setSuccess = await cache.SetAsync("product:100", product, TimeSpan.FromMinutes(10));
Console.WriteLine($"  Set product: {setSuccess}");

var result = await cache.GetAsync<Product>("product:100");
if (result.Success && result.FromCache)
{
    Console.WriteLine($"  ✓ Got from cache: {result.Value!.Name} - ${result.Value.Price}");
}

// Example 3: Cache Stampede Prevention
Console.WriteLine("\n=== Example 3: Cache Stampede Prevention ===");
Console.WriteLine("  Simulating 5 concurrent requests for same key...");

var tasks = new List<Task<ExpensiveData>>();
for (int i = 0; i < 5; i++)
{
    tasks.Add(cache.GetOrSetAsync(
        "expensive-data",
        async () =>
        {
            Console.WriteLine($"    → Expensive operation executing (should only see this ONCE)");
            await Task.Delay(500); // Simulate expensive operation
            return new ExpensiveData { Value = "Computed Result", ComputedAt = DateTime.UtcNow };
        },
        TimeSpan.FromMinutes(1)
    ));
}

var results = await Task.WhenAll(tasks);
Console.WriteLine($"  ✓ All 5 requests completed");
Console.WriteLine($"  ✓ All got same result: {results[0].Value}");

// Example 4: Extension Methods
Console.WriteLine("\n=== Example 4: Extension Methods ===");

// GetOrDefault
var config = await cache.GetOrDefaultAsync("missing-key", new Config { Setting = "default" });
Console.WriteLine($"  GetOrDefault: {config.Setting}");

// TryGet
var (found, value) = await cache.TryGetAsync<Product>("product:100");
Console.WriteLine($"  TryGet: Found={found}, Value={value?.Name}");

// Example 5: Cache Management
Console.WriteLine("\n=== Example 5: Cache Management ===");

bool exists = await cache.ExistsAsync("user:1");
Console.WriteLine($"  Key 'user:1' exists: {exists}");

bool removed = await cache.RemoveAsync("user:1");
Console.WriteLine($"  Removed 'user:1': {removed}");

exists = await cache.ExistsAsync("user:1");
Console.WriteLine($"  Key 'user:1' exists after removal: {exists}");

// Example 6: Error Handling
Console.WriteLine("\n=== Example 6: Error Handling ===");
var errorResult = await cache.GetAsync<User>("");
Console.WriteLine($"  Empty key result - Success: {errorResult.Success}, Error: {errorResult.ErrorMessage}");

Console.WriteLine("\n=== Demo Complete ===");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

// Models
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ExpensiveData
{
    public string Value { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; }
}

public class Config
{
    public string Setting { get; set; } = string.Empty;
}
