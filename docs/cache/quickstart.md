# Quick Start Guide - Redis Caching in BookingCare

This guide will get you up and running with Redis caching in 10 minutes.

## Prerequisites

- .NET 8.0 SDK
- Docker (for Redis)
- Visual Studio Code or Visual Studio

## Step 1: Start Redis Server (2 minutes)

```bash
# Pull and run Redis container
docker run -d --name redis-bookingcare -p 6379:6379 redis:7-alpine

# Verify Redis is running
docker logs redis-bookingcare
```

Expected output: `Ready to accept connections`

## Step 2: Add Cache Library Reference (1 minute)

Add to your service's `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Shared\BookingCare.Shared.Cache\BookingCare.Shared.Cache.csproj" />
</ItemGroup>
```

## Step 3: Configure Cache in appsettings.json (1 minute)

```json
{
  "Cache": {
    "ConnectionString": "localhost:6379",
    "DefaultExpirationInMinutes": 60,
    "KeyPrefix": "BookingCare:YourService:",
    "Enabled": true
  }
}
```

## Step 4: Register Cache Service (1 minute)

In your `Program.cs`:

```csharp
using BookingCare.Shared.Cache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add your other services...
builder.Services.AddControllers();

// Add Redis cache - ONE LINE!
builder.Services.AddRedisCache(builder.Configuration);

var app = builder.Build();
// Continue with your app setup...
```

## Step 5: Use Cache in Your Service (5 minutes)

### Simple Example

```csharp
public class UserController : ControllerBase
{
    private readonly ICacheService _cache;
    
    public UserController(ICacheService cache)
    {
        _cache = cache;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        // Get or load user with caching
        var user = await _cache.GetOrSetAsync($"user:{id}", async () =>
        {
            // This only runs on cache miss
            return await LoadUserFromDatabase(id);
        }, TimeSpan.FromMinutes(30));
        
        return Ok(user);
    }
    
    private async Task<User> LoadUserFromDatabase(int id)
    {
        // Your database logic here
        await Task.Delay(100); // Simulate DB call
        return new User { Id = id, Name = $"User {id}" };
    }
}
```

### Test Your Cache

Add this test endpoint:

```csharp
[HttpGet("cache-test")]
public async Task<IActionResult> TestCache()
{
    // Set a value
    await _cache.SetAsync("test", new { Message = "Hello Cache!" }, TimeSpan.FromMinutes(5));
    
    // Get the value
    var cached = await _cache.GetAsync<object>("test");
    
    return Ok(new { 
        Success = cached != null,
        Data = cached,
        Timestamp = DateTime.UtcNow 
    });
}
```

## Step 6: Verify Everything Works

1. **Start your service**
   ```bash
   dotnet run
   ```

2. **Test the cache**
   - Call `GET /api/users/cache-test`
   - Should return: `{"success": true, "data": {"message": "Hello Cache!"}, ...}`

3. **Test user caching**
   - Call `GET /api/users/123` twice
   - First call: slower (cache miss)
   - Second call: faster (cache hit)

## Common Cache Patterns

### Pattern 1: Get or Set
```csharp
var data = await _cache.GetOrSetAsync("key", async () => 
{
    return await LoadExpensiveData();
}, TimeSpan.FromMinutes(30));
```

### Pattern 2: Manual Cache Management
```csharp
// Check cache first
var cached = await _cache.GetAsync<User>("user:123");
if (cached != null) return cached;

// Load from database
var user = await _database.GetUserAsync(123);

// Cache for next time
await _cache.SetAsync("user:123", user, TimeSpan.FromMinutes(30));
```

### Pattern 3: Cache Invalidation
```csharp
// When updating data
await _database.UpdateUserAsync(user);
await _cache.RemoveAsync($"user:{user.Id}"); // Invalidate cache
```

## Predefined Cache Keys

Use the provided cache key constants:

```csharp
using BookingCare.Shared.Cache.Constants;

// Instead of: "user:123"
var key = CacheKeys.Format(CacheKeys.UserById, 123);

// Available keys:
// CacheKeys.UserById, CacheKeys.UserByEmail
// CacheKeys.DoctorById, CacheKeys.DoctorSchedule
// CacheKeys.ClinicById, CacheKeys.AppointmentById
// And many more...
```

## Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionString` | `localhost:6379` | Redis server address |
| `DefaultExpirationInMinutes` | `60` | Default cache expiration |
| `KeyPrefix` | `BookingCare:` | Prefix for all cache keys |
| `Enabled` | `true` | Enable/disable caching |

## Next Steps

1. **Monitor your cache**: Check Redis with `redis-cli monitor`
2. **Optimize expiration times**: Short for frequent changes, long for static data
3. **Add cache invalidation**: Remove cache when data changes
4. **Use cache patterns**: Implement cache-aside, write-through, etc.

## Troubleshooting

**Cache not working?**
- Check Redis is running: `docker ps`
- Check connection: `redis-cli ping`
- Verify configuration in appsettings.json

**Performance issues?**
- Monitor cache hit/miss ratio
- Adjust expiration times
- Check Redis memory usage

## Full Working Example

Here's a complete controller with caching:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IUserRepository _repository;
    
    public UsersController(ICacheService cache, IUserRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _cache.GetOrSetAsync(
            CacheKeys.Format(CacheKeys.UserById, id),
            () => _repository.GetByIdAsync(id),
            TimeSpan.FromMinutes(30)
        );
        
        return user != null ? Ok(user) : NotFound();
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        await _repository.UpdateAsync(user);
        
        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Format(CacheKeys.UserById, id));
        
        return Ok();
    }
}
```

You're now ready to use Redis caching in your BookingCare services! 🚀
