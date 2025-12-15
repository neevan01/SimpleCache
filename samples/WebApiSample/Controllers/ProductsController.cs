using SimpleCache.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleCache.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApiSample.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace WebApiSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ISimpleCache _cache;
        private readonly ILogger<ProductsController> _logger;

        // Simulated database
        private static readonly List<Product> _database = new()
        {
            new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 50, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 200, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 100, CreatedAt = DateTime.UtcNow },
            new Product { Id = 4, Name = "Monitor", Description = "4K monitor", Price = 399.99m, Stock = 30, CreatedAt = DateTime.UtcNow },
            new Product { Id = 5, Name = "Headphones", Description = "Noise-cancelling headphones", Price = 199.99m, Stock = 75, CreatedAt = DateTime.UtcNow }
        };

        public ProductsController(ISimpleCache cache, ILogger<ProductsController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get all products (cached for 5 minutes)
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Retrieve all products", Description = "Returns all products from cache or database.")]
        [SwaggerResponse(200, "List of products", typeof(IEnumerable<Product>))]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            _logger.LogInformation("GetAll products requested");

            var products = await _cache.GetOrSetAsync(
                "products:all",
                async () =>
                {
                    _logger.LogInformation("Cache miss - fetching from database");
                    await Task.Delay(500); // Simulate DB delay
                    return _database;
                },
                TimeSpan.FromMinutes(5)
            );

            return Ok(products);
        }

        /// <summary>
        /// Get product by ID (cached for 10 minutes)
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Retrieve a product by ID", Description = "Returns a single product, cached for 10 minutes.")]
        [SwaggerResponse(200, "Product found", typeof(Product))]
        [SwaggerResponse(404, "Product not found")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            _logger.LogInformation("GetById requested for product {ProductId}", id);

            var product = await _cache.GetOrSetAsync(
                $"product:{id}",
                async () =>
                {
                    _logger.LogInformation("Cache miss - fetching product {ProductId} from database", id);
                    await Task.Delay(200); // Simulate DB delay
                    return _database.FirstOrDefault(p => p.Id == id);
                },
                TimeSpan.FromMinutes(10)
            );

            if (product == null)
                return NotFound(new { message = $"Product {id} not found" });

            return Ok(product);
        }

        /// <summary>
        /// Search products by name (cached for 2 minutes)
        /// </summary>
        [HttpGet("search")]
        [SwaggerOperation(Summary = "Search products by name", Description = "Searches product name and description, cached for 2 minutes.")]
        [SwaggerResponse(200, "Search results", typeof(IEnumerable<Product>))]
        [SwaggerResponse(400, "Invalid query parameter")]
        public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query parameter is required" });

            _logger.LogInformation("Search requested for query: {Query}", query);

            var results = await _cache.GetOrSetAsync(
                $"products:search:{query.ToLower()}",
                async () =>
                {
                    _logger.LogInformation("Cache miss - searching database for: {Query}", query);
                    await Task.Delay(300); // Simulate DB delay
                    return _database.Where(p => 
                        p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                },
                TimeSpan.FromMinutes(2)
            );

            return Ok(results);
        }

        /// <summary>
        /// Create a new product (invalidates cache)
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Create a product", Description = "Adds a new product and invalidates cache.")]
        [SwaggerResponse(201, "Product created", typeof(Product))]
        [SwaggerResponse(400, "Invalid product data")]
        public async Task<ActionResult<Product>> Create([FromBody] Product product)
        {
            _logger.LogInformation("Create product requested");

            product.Id = _database.Max(p => p.Id) + 1;
            product.CreatedAt = DateTime.UtcNow;
            _database.Add(product);

            // Invalidate cache
            await _cache.RemoveAsync("products:all");
            _logger.LogInformation("Cache invalidated for products:all");

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>
        /// Update product (invalidates cache)
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update a product", Description = "Updates product info and invalidates cache.")]
        [SwaggerResponse(200, "Product updated", typeof(Product))]
        [SwaggerResponse(404, "Product not found")]
        public async Task<ActionResult<Product>> Update(int id, [FromBody] Product updatedProduct)
        {
            var product = _database.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = $"Product {id} not found" });

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;

            // Invalidate cache
            await _cache.RemoveAsync($"product:{id}");
            await _cache.RemoveAsync("products:all");
            _logger.LogInformation("Cache invalidated for product {ProductId}", id);

            return Ok(product);
        }

        /// <summary>
        /// Delete product (invalidates cache)
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a product", Description = "Deletes a product and invalidates cache.")]
        [SwaggerResponse(204, "Product deleted")]
        [SwaggerResponse(404, "Product not found")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = _database.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = $"Product {id} not found" });

            _database.Remove(product);

            // Invalidate cache
            await _cache.RemoveAsync($"product:{id}");
            await _cache.RemoveAsync("products:all");
            _logger.LogInformation("Cache invalidated for product {ProductId}", id);

            return NoContent();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("cache/stats")]
        [SwaggerOperation(Summary = "Get cache stats", Description = "Returns info about cached products.")]
        [SwaggerResponse(200, "Cache statistics")]
        public async Task<ActionResult> GetCacheStats()
        {
            var allExists = await _cache.ExistsAsync("products:all");
            
            var stats = new
            {
                allProductsCached = allExists,
                message = "Check logs for detailed cache hit/miss information"
            };

            return Ok(stats);
        }

        /// <summary>
        /// Clear all product cache
        /// </summary>
        [HttpDelete("cache/clear")]
        [SwaggerOperation(Summary = "Clear all product cache", Description = "Removes all cached products.")]
        [SwaggerResponse(200, "Cache cleared successfully")]
        public async Task<ActionResult> ClearCache()
        {
            await _cache.RemoveAsync("products:all");
            
            // Remove individual product caches
            foreach (var product in _database)
            {
                await _cache.RemoveAsync($"product:{product.Id}");
            }

            _logger.LogWarning("All product cache cleared");
            return Ok(new { message = "Cache cleared successfully" });
        }
    }
}
