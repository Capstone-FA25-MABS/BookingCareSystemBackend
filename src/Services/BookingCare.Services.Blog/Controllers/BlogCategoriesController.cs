using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Blog.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/blog/categories")]
public class BlogCategoriesController : ControllerBase
{
    private readonly IBlogCategoryService _categoryService;

    public BlogCategoriesController(IBlogCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<BlogCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories([FromQuery] bool includeChildren = true, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetCategoriesAsync(includeChildren, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlogCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(Guid id, [FromQuery] bool includeChildren = true, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, includeChildren, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BlogCategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBlogCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" }, category);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BlogCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateBlogCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(category);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken = default)
    {
        await _categoryService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }
}

