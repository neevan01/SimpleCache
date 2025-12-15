# SimpleCache Web API Sample

This sample demonstrates how to use SimpleCache in an ASP.NET Core Web API application.

## Features Demonstrated

- ✅ Dependency injection setup
- ✅ GetOrSet pattern for automatic caching
- ✅ Cache invalidation on updates
- ✅ Different expiration times per endpoint
- ✅ Logging integration
- ✅ Cache management endpoints

## Running the Sample

```bash
cd samples/WebApiSample
dotnet run
```

Then open: https://localhost:5001/swagger

## API Endpoints

### Products

- `GET /api/products` - Get all products (cached 5 min)
- `GET /api/products/{id}` - Get product by ID (cached 10 min)
- `GET /api/products/search?query=laptop` - Search products (cached 2 min)
- `POST /api/products` - Create product (invalidates cache)
- `PUT /api/products/{id}` - Update product (invalidates cache)
- `DELETE /api/products/{id}` - Delete product (invalidates cache)

### Cache Management

- `GET /api/products/cache/stats` - Get cache statistics
- `DELETE /api/products/cache/clear` - Clear all product cache

## Testing Cache Behavior

### 1. Test Cache Hit/Miss

```bash
# First call - cache miss (slow)
curl https://localhost:5001/api/products

# Second call - cache hit (fast)
curl https://localhost:5001/api/products
```

Check the console logs to see:
```
Cache miss - fetching from database
Cache hit for key: webapi:products:all
```

### 2. Test Cache Invalidation

```bash
# Get product (cached)
curl https://localhost:5001/api/products/1

# Update product (invalidates cache)
curl -X PUT https://localhost:5001/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Laptop","description":"New description","price":899.99,"stock":45}'

# Get product again (cache miss, fresh data)
curl https://localhost:5001/api/products/1
```

### 3. Test Search Caching

```bash
# Search for "laptop" (cache miss)
curl "https://localhost:5001/api/products/search?query=laptop"

# Same search (cache hit)
curl "https://localhost:5001/api/products/search?query=laptop"

# Different search (cache miss)
curl "https://localhost:5001/api/products/search?query=mouse"
```

## Configuration

In `Program.cs`:

```csharp
builder.Services.AddSimpleCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.KeyPrefix = "webapi";
    options.EnableSerialization = true;
});
```

## Logging

Set log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "SimpleCache": "Debug"
    }
  }
}
```

You'll see logs like:
- `Cache hit for key: webapi:products:all`
- `Cache miss for key: webapi:product:1`
- `Cached value for key: webapi:products:all, expiration: 00:05:00`

## Performance Comparison

Without cache:
- GET /api/products: ~500ms (simulated DB delay)
- GET /api/products/1: ~200ms

With cache (hit):
- GET /api/products: ~1ms
- GET /api/products/1: ~1ms

**Result: 200-500x faster!**
