using BookingCare.Services.User.Models;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Cache.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.User.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CachedUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        CachedUserService userService,
        ICacheService cacheService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "User", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { Message = $"User with email {email} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("{id}/profile")]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        try
        {
            var profile = await _userService.GetUserProfileAsync(id);
            if (profile == null)
            {
                return NotFound(new { Message = $"User profile with ID {id} not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile {UserId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserModel userRequest)
    {
        try
        {
            // Simulate user creation and return created user
            var createdUser = new UserModel
            {
                Id = new Random().Next(1000, 9999),
                Email = userRequest.Email,
                FirstName = userRequest.FirstName,
                LastName = userRequest.LastName,
                PhoneNumber = userRequest.PhoneNumber,
                DateOfBirth = userRequest.DateOfBirth,
                Address = userRequest.Address,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Cache the newly created user
            await _userService.UpdateUserAsync(createdUser);

            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserModel userRequest)
    {
        try
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found" });
            }

            // Update user properties
            existingUser.FirstName = userRequest.FirstName;
            existingUser.LastName = userRequest.LastName;
            existingUser.PhoneNumber = userRequest.PhoneNumber;
            existingUser.DateOfBirth = userRequest.DateOfBirth;
            existingUser.Address = userRequest.Address;

            await _userService.UpdateUserAsync(existingUser);

            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found" });
            }

            await _userService.DeleteUserAsync(id, user.Email);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    // Cache management endpoints for demonstration

    [HttpPost("{id}/cache/invalidate")]
    public async Task<IActionResult> InvalidateUserCache(int id)
    {
        try
        {
            await _userService.InvalidateAllUserDataAsync(id);
            return Ok(new { Message = $"Cache invalidated for user {id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for user {UserId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("cache/test")]
    public async Task<IActionResult> TestCache()
    {
        try
        {
            const string testKey = "test:cache:key";
            const string testValue = "Hello from cache!";

            // Set a test value
            await _cacheService.SetAsync(testKey, new { Message = testValue }, TimeSpan.FromMinutes(5));

            // Get the test value
            var cachedValue = await _cacheService.GetAsync<object>(testKey);

            // Check if key exists
            var exists = await _cacheService.ExistsAsync(testKey);

            return Ok(new
            {
                TestKey = testKey,
                SetValue = testValue,
                CachedValue = cachedValue,
                KeyExists = exists,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing cache");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}