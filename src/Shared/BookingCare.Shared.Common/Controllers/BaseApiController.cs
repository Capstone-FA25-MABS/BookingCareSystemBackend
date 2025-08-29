using BookingCare.Shared.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Shared.Common.Controllers;

/// <summary>
/// Base controller class with common functionality for all API controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Returns a successful response with data
    /// </summary>
    /// <typeparam name="T">The type of data to return</typeparam>
    /// <param name="data">The data to return</param>
    /// <param name="message">Optional success message</param>
    /// <returns>API response with data</returns>
    protected IActionResult Success<T>(T data, string message = "Success")
    {
        var response = ApiResponse<T>.SuccessResult(data, message);
        return Ok(response);
    }

    /// <summary>
    /// Returns a successful response without data
    /// </summary>
    /// <param name="message">Success message</param>
    /// <returns>API response without data</returns>
    protected IActionResult Success(string message = "Success")
    {
        var response = ApiResponse<object?>.SuccessResult(null, message);
        return Ok(response);
    }

    /// <summary>
    /// Returns a created response with data
    /// </summary>
    /// <typeparam name="T">The type of data to return</typeparam>
    /// <param name="data">The data to return</param>
    /// <param name="message">Optional success message</param>
    /// <returns>API response with data and 201 status</returns>
    protected IActionResult Created<T>(T data, string message = "Created successfully")
    {
        var response = ApiResponse<T>.SuccessResult(data, message);
        return StatusCode(201, response);
    }

    /// <summary>
    /// Returns a bad request response with error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Optional list of specific errors</param>
    /// <returns>API response with error and 400 status</returns>
    protected IActionResult BadRequest(string message, List<string>? errors = null)
    {
        var response = ApiResponse<object>.ErrorResult(message, errors);
        return BadRequest(response);
    }

    /// <summary>
    /// Returns a not found response with error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>API response with error and 404 status</returns>
    protected IActionResult NotFound(string message)
    {
        var response = ApiResponse<object>.ErrorResult(message);
        return NotFound(response);
    }

    /// <summary>
    /// Returns an unauthorized response with error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>API response with error and 401 status</returns>
    protected IActionResult Unauthorized(string message = "Unauthorized")
    {
        var response = ApiResponse<object>.ErrorResult(message);
        return Unauthorized(response);
    }

    /// <summary>
    /// Returns a forbidden response with error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>API response with error and 403 status</returns>
    protected IActionResult Forbidden(string message = "Forbidden")
    {
        var response = ApiResponse<object>.ErrorResult(message);
        return StatusCode(403, response);
    }

    /// <summary>
    /// Returns a conflict response with error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>API response with error and 409 status</returns>
    protected IActionResult Conflict(string message)
    {
        var response = ApiResponse<object>.ErrorResult(message);
        return Conflict(response);
    }

    /// <summary>
    /// Returns a paginated response
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated result</typeparam>
    /// <param name="pagedResult">The paginated result</param>
    /// <param name="message">Optional success message</param>
    /// <returns>API response with paginated data</returns>
    protected IActionResult Paginated<T>(PagedResult<T> pagedResult, string message = "Success")
    {
        var response = ApiResponse<PagedResult<T>>.SuccessResult(pagedResult, message);
        return Ok(response);
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context
    /// </summary>
    /// <returns>The current user ID or null if not authenticated</returns>
    protected string? GetCurrentUserId()
    {
        return HttpContext.User?.Identity?.Name;
    }

    /// <summary>
    /// Gets the correlation ID from the HTTP context
    /// </summary>
    /// <returns>The correlation ID</returns>
    protected string GetCorrelationId()
    {
        return HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Checks if the current user is in the specified role
    /// </summary>
    /// <param name="role">The role to check</param>
    /// <returns>True if the user is in the role, false otherwise</returns>
    protected bool IsInRole(string role)
    {
        return HttpContext.User?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Gets a claim value from the current user
    /// </summary>
    /// <param name="claimType">The claim type</param>
    /// <returns>The claim value or null if not found</returns>
    protected string? GetClaimValue(string claimType)
    {
        return HttpContext.User?.FindFirst(claimType)?.Value;
    }
}
