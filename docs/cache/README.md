# Redis Caching Implementation Guide for BookingCare System

## Overview

This guide provides step-by-step instructions for implementing and using Redis caching in the BookingCare microservices system. The caching solution is designed to improve performance, reduce database load, and provide a consistent caching interface across all services.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Installation and Setup](#installation-and-setup)
4. [Configuration](#configuration)
5. [Basic Usage](#basic-usage)
6. [Advanced Patterns](#advanced-patterns)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)
9. [Examples](#examples)

## Prerequisites

- .NET 8.0 or later
- Redis server (local or remote)
- Docker (optional, for running Redis locally)

## Architecture Overview

The caching system consists of:

- **BookingCare.Shared.Cache**: Core caching library with Redis implementation
- **ICacheService**: Main interface for all caching operations
- **RedisCacheService**: Redis-specific implementation
- **CacheOptions**: Configuration options
- **CacheKeys**: Predefined cache key constants

### Key Components

```
BookingCare.Shared.Cache/
├── Abstractions/
│   └── ICacheService.cs           # Main caching interface
├── Services/
│   ├── RedisCacheService.cs       # Redis implementation
│   └── NoCacheService.cs          # No-op implementation (when disabled)
├── Options/
│   └── CacheOptions.cs            # Configuration options
├── Constants/
│   └── CacheKeys.cs              # Predefined cache keys
└── Extensions/
    └── ServiceCollectionExtensions.cs # DI registration
```

## Installation and Setup

### Step 1: Install Redis

#### Option A: Docker (Recommended for Development)

```bash
# Run Redis in Docker
docker run -d --name redis-bookingcare -p 6379:6379 redis:7-alpine

# Or using docker-compose (add to your docker-compose.yml)
version: '3.8'
services:
  redis:
    image: redis:7-alpine
    container_name: redis-bookingcare
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

#### Option B: Install Locally

**Windows:**
- Download Redis for Windows from GitHub releases
- Install and start the Redis service

**macOS:**
```bash
brew install redis
brew services start redis
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install redis-server
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

### Step 2: Add Cache Library Reference

Add the cache library reference to your service project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Shared\BookingCare.Shared.Cache\BookingCare.Shared.Cache.csproj" />
</ItemGroup>
```

### Step 3: Configure Services

Update your `Program.cs`:

```csharp
using BookingCare.Shared.Cache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add other services...
builder.Services.AddControllers();

// Add Redis cache
builder.Services.AddRedisCache(builder.Configuration);

var app = builder.Build();
// Continue with app configuration...
```

## Configuration

### appsettings.json Configuration

Add the cache configuration section to your `appsettings.json`:

```json
{
  "Cache": {
    "ConnectionString": "localhost:6379",
    "DefaultExpirationInMinutes": 60,
    "KeyPrefix": "BookingCare:ServiceName:",
    "Database": 0,
    "Enabled": true,
    "RetryCount": 3,
    "ConnectTimeout": 5,
    "CommandTimeout": 5
  }
}
```

### Configuration Options Explained

| Option | Description | Default | Example |
|--------|-------------|---------|---------|
| `ConnectionString` | Redis connection string | `localhost:6379` | `redis-server:6379` |
| `DefaultExpirationInMinutes` | Default cache expiration time | `60` | `30` |
| `KeyPrefix` | Prefix for all cache keys | `BookingCare:` | `BookingCare:User:` |
| `Database` | Redis database number (0-15) | `0` | `1` |
| `Enabled` | Enable/disable caching | `true` | `false` |
| `RetryCount` | Retry attempts for failed operations | `3` | `5` |
| `ConnectTimeout` | Connection timeout in seconds | `5` | `10` |
| `CommandTimeout` | Command timeout in seconds | `5` | `10` |

### Environment-Specific Configuration

**Development (appsettings.Development.json):**
```json
{
  "Cache": {
    "ConnectionString": "localhost:6379",
    "Enabled": true,
    "DefaultExpirationInMinutes": 30
  }
}
```

**Production (appsettings.Production.json):**
```json
{
  "Cache": {
    "ConnectionString": "redis-cluster.production.com:6379",
    "Enabled": true,
    "DefaultExpirationInMinutes": 120,
    "RetryCount": 5
  }
}
```

## Basic Usage

### Step 1: Inject ICacheService

```csharp
public class UserService
{
    private readonly ICacheService _cacheService;
    
    public UserService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
}
```

### Step 2: Basic Operations

#### Get from Cache
```csharp
var user = await _cacheService.GetAsync<User>("user:123");
```

#### Set Cache
```csharp
await _cacheService.SetAsync("user:123", user, TimeSpan.FromMinutes(30));
```

#### Remove from Cache
```csharp
await _cacheService.RemoveAsync("user:123");
```

#### Check if Key Exists
```csharp
bool exists = await _cacheService.ExistsAsync("user:123");
```

#### Get or Set Pattern
```csharp
var user = await _cacheService.GetOrSetAsync("user:123", async () =>
{
    // This function runs only if cache miss
    return await _userRepository.GetByIdAsync(123);
}, TimeSpan.FromMinutes(30));
```

### Step 3: Using Predefined Cache Keys

```csharp
using BookingCare.Shared.Cache.Constants;

// Using predefined keys
var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);
var user = await _cacheService.GetAsync<User>(cacheKey);

// Or directly
var user = await _cacheService.GetAsync<User>(
    CacheKeys.Format(CacheKeys.UserById, 123)
);
```

## Advanced Patterns

### Pattern 1: Cache-Aside (Lazy Loading)

```csharp
public async Task<User?> GetUserAsync(int userId)
{
    var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);
    
    // Try to get from cache first
    var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
    if (cachedUser != null)
    {
        return cachedUser;
    }
    
    // Cache miss - load from database
    var user = await _userRepository.GetByIdAsync(userId);
    if (user != null)
    {
        // Cache for future requests
        await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
    }
    
    return user;
}
```

### Pattern 2: Write-Through Cache

```csharp
public async Task UpdateUserAsync(User user)
{
    // Update in database first
    await _userRepository.UpdateAsync(user);
    
    // Update cache
    var cacheKey = CacheKeys.Format(CacheKeys.UserById, user.Id);
    await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
}
```

### Pattern 3: Write-Behind (Cache Invalidation)

```csharp
public async Task UpdateUserAsync(User user)
{
    // Update in database
    await _userRepository.UpdateAsync(user);
    
    // Invalidate cache - let next read load fresh data
    var cacheKey = CacheKeys.Format(CacheKeys.UserById, user.Id);
    await _cacheService.RemoveAsync(cacheKey);
}
```

### Pattern 4: Bulk Cache Operations

```csharp
public async Task InvalidateUserDataAsync(int userId)
{
    // Remove multiple related cache entries
    var tasks = new List<Task>
    {
        _cacheService.RemoveAsync(CacheKeys.Format(CacheKeys.UserById, userId)),
        _cacheService.RemoveAsync(CacheKeys.Format(CacheKeys.UserProfile, userId)),
        _cacheService.RemoveAsync(CacheKeys.Format(CacheKeys.UserAppointments, userId))
    };
    
    await Task.WhenAll(tasks);
}
```

### Pattern 5: Cache with Refresh-Ahead

```csharp
public async Task<User?> GetUserWithRefreshAsync(int userId)
{
    var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);
    
    return await _cacheService.GetOrSetAsync(cacheKey, async () =>
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        // Schedule refresh before expiration (fire and forget)
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(25)); // Refresh 5 minutes before expiration
            var freshUser = await _userRepository.GetByIdAsync(userId);
            if (freshUser != null)
            {
                await _cacheService.SetAsync(cacheKey, freshUser, TimeSpan.FromMinutes(30));
            }
        });
        
        return user;
    }, TimeSpan.FromMinutes(30));
}
```

## Best Practices

### 1. Cache Key Naming Convention

- Use consistent naming patterns
- Include service prefix to avoid collisions
- Use delimiters (`:`) for hierarchy

```csharp
// Good examples:
"BookingCare:User:123"
"BookingCare:Doctor:456:Schedule:2024-09-14"
"BookingCare:Clinic:789:Services"

// Bad examples:
"user123"
"doctorschedule456"
"clinic_services_789"
```

### 2. Expiration Strategies

```csharp
// Frequently accessed, rarely changed data
await _cacheService.SetAsync(key, data, TimeSpan.FromHours(6));

// User sessions
await _cacheService.SetAsync(key, session, TimeSpan.FromMinutes(30));

// Configuration data
await _cacheService.SetAsync(key, config, TimeSpan.FromDays(1));

// Real-time data (very short lived)
await _cacheService.SetAsync(key, realtimeData, TimeSpan.FromSeconds(30));
```

### 3. Error Handling

```csharp
public async Task<User?> GetUserSafelyAsync(int userId)
{
    try
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);
        var user = await _cacheService.GetAsync<User>(cacheKey);
        
        if (user == null)
        {
            user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                // Cache with shorter expiration on errors
                await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(5));
            }
        }
        
        return user;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Cache error for user {UserId}. Falling back to database", userId);
        // Fallback to database on cache errors
        return await _userRepository.GetByIdAsync(userId);
    }
}
```

### 4. Performance Considerations

```csharp
// Use batch operations when possible
var tasks = userIds.Select(id => 
    _cacheService.GetAsync<User>(CacheKeys.Format(CacheKeys.UserById, id))
).ToArray();

var users = await Task.WhenAll(tasks);

// Consider serialization size
// Large objects should have shorter expiration times
public class LargeDataCache
{
    // Larger objects -> shorter expiration
    public async Task CacheLargeDataAsync(string key, LargeObject data)
    {
        var expiration = data.EstimatedSize > 1024 * 100 // 100KB
            ? TimeSpan.FromMinutes(5)  // Short expiration for large objects
            : TimeSpan.FromMinutes(30); // Normal expiration
            
        await _cacheService.SetAsync(key, data, expiration);
    }
}
```

### 5. Monitoring and Logging

```csharp
public class MonitoredCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<MonitoredCacheService> _logger;
    
    public async Task<T?> GetWithMetricsAsync<T>(string key) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _cacheService.GetAsync<T>(key);
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Cache {Operation} for key {Key}: {Result} (took {Duration}ms)",
            "GET",
            key,
            result != null ? "HIT" : "MISS",
            stopwatch.ElapsedMilliseconds
        );
        
        return result;
    }
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Connection Issues

**Problem**: Cannot connect to Redis server

**Solutions**:
```bash
# Check Redis is running
redis-cli ping
# Should return "PONG"

# Check connection string in appsettings.json
# Verify Redis server is accessible
telnet localhost 6379
```

#### 2. Serialization Issues

**Problem**: Objects not serializing/deserializing properly

**Solutions**:
```csharp
// Ensure your models are serializable
public class User
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("email")]
    public string Email { get; set; }
    
    // Avoid circular references
    [JsonIgnore]
    public List<Order> Orders { get; set; }
}
```

#### 3. Memory Issues

**Problem**: Redis consuming too much memory

**Solutions**:
```bash
# Check Redis memory usage
redis-cli info memory

# Set memory policies in Redis configuration
redis-cli config set maxmemory 1gb
redis-cli config set maxmemory-policy allkeys-lru
```

#### 4. Performance Issues

**Problem**: Cache operations are slow

**Solutions**:
- Use connection pooling (handled automatically by StackExchange.Redis)
- Implement pipelining for bulk operations
- Monitor network latency between application and Redis

### Debugging Cache Issues

```csharp
public class CacheDebugService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheDebugService> _logger;
    
    public async Task DebugCacheOperationsAsync()
    {
        var testKey = "debug:test";
        var testValue = new { Message = "Test", Timestamp = DateTime.UtcNow };
        
        try
        {
            // Test set operation
            _logger.LogInformation("Testing cache SET operation...");
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            _logger.LogInformation("Cache SET successful");
            
            // Test get operation
            _logger.LogInformation("Testing cache GET operation...");
            var retrieved = await _cacheService.GetAsync<object>(testKey);
            _logger.LogInformation("Cache GET result: {@Result}", retrieved);
            
            // Test exists operation
            _logger.LogInformation("Testing cache EXISTS operation...");
            var exists = await _cacheService.ExistsAsync(testKey);
            _logger.LogInformation("Cache EXISTS result: {Exists}", exists);
            
            // Test remove operation
            _logger.LogInformation("Testing cache REMOVE operation...");
            await _cacheService.RemoveAsync(testKey);
            _logger.LogInformation("Cache REMOVE successful");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache debug operation failed");
        }
    }
}
```

## Examples

See the [examples](./examples.md) document for complete working examples including:

- User service with caching
- Doctor appointment caching
- Multi-level caching strategies
- Cache warming techniques
- Distributed cache invalidation

## Performance Metrics

Expected performance improvements with caching:

- **Database load reduction**: 60-80% for frequently accessed data
- **Response time improvement**: 10-50x faster for cached data
- **Scalability**: Better handling of traffic spikes

## Next Steps

1. Implement caching in your service following this guide
2. Monitor cache hit/miss ratios
3. Adjust expiration times based on usage patterns
4. Consider implementing cache warming for critical data
5. Set up monitoring and alerting for cache health

## Support

For issues and questions:
- Check the troubleshooting section
- Review logs for error details
- Contact the development team for assistance