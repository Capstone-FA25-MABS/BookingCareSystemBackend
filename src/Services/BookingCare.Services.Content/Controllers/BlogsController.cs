using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Services;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Content.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/blogs")]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly ILogger<BlogsController> _logger;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const int MaxImageSizeMb = 5;
    private const string ThumbnailFolder = "blogs/thumbnails";
    private const string HeroFolder = "blogs/hero";
    private const string ExpectedFolderRoot = "blogs";

    public BlogsController(
        IBlogService blogService,
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<BlogsController> logger)
    {
        _blogService = blogService;
        _uploadOrchestrator = uploadOrchestrator;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<BlogSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlogs(
        [FromQuery] BlogFilterParameters filter,
        CancellationToken cancellationToken = default)
    {
        var blogs = await _blogService.GetBlogsAsync(filter, cancellationToken);
        return Ok(blogs);
    }

    /// <summary>
    /// Get blogs created by the authenticated account.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<BlogSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBlogs(
        [FromQuery] BlogFilterParameters filter,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaims(HttpContext);
        if (!accountId.HasValue)
        {
            return Forbid();
        }

        filter.CreatedByAccountId = accountId;
        var blogs = await _blogService.GetBlogsAsync(filter, cancellationToken);
        return Ok(blogs);
    }

    [HttpGet("all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<BlogDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBlogs(
        CancellationToken cancellationToken = default)
    {
        var blogs = await _blogService.GetAllBlogsAsync(cancellationToken);
        return Ok(blogs);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlog(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var blog = await _blogService.GetBlogByIdAsync(id, cancellationToken);
        return blog is null ? NotFound() : Ok(blog);
    }

    [HttpPost]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBlog(
        [FromBody] CreateBlogRequest request,
        CancellationToken cancellationToken = default)
    {
        var createdBy = JwtHelper.GetAccountIdFromClaims(HttpContext);
        return await CreateBlogInternalAsync(request, createdBy, cancellationToken);
    }

    /// <summary>
    /// Create a new blog with image upload support.
    /// </summary>
    [HttpPost("upload-images")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBlogWithImages(
        [FromForm] CreateBlogRequest request,
        [FromForm] IFormFile? thumbnailFile,
        [FromForm] IFormFile? heroImageFile,
        CancellationToken cancellationToken = default)
    {
        var createdBy = JwtHelper.GetAccountIdFromClaims(HttpContext);
        var uploaderId = createdBy ?? Guid.Empty;

        var (thumbnailUrl, thumbnailError) = await TryUploadBlogImageAsync(
            thumbnailFile,
            ThumbnailFolder,
            "blog-thumbnail",
            uploaderId,
            cancellationToken);

        if (thumbnailError is not null)
        {
            return thumbnailError;
        }

        if (!string.IsNullOrEmpty(thumbnailUrl))
        {
            request.ThumbnailUrl = thumbnailUrl;
        }

        var (heroUrl, heroError) = await TryUploadBlogImageAsync(
            heroImageFile,
            HeroFolder,
            "blog-hero-image",
            uploaderId,
            cancellationToken);

        if (heroError is not null)
        {
            return heroError;
        }

        if (!string.IsNullOrEmpty(heroUrl))
        {
            request.HeroImageUrl = heroUrl;
        }

        return await CreateBlogInternalAsync(request, createdBy, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBlog(
        Guid id,
        [FromBody] UpdateBlogRequest request,
        CancellationToken cancellationToken = default)
    {
        var blog = await _blogService.UpdateBlogAsync(id, request, cancellationToken);
        return Ok(blog);
    }

    /// <summary>
    /// Update blog with optional thumbnail/hero image upload.
    /// </summary>
    [HttpPut("{id:guid}/upload-images")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBlogWithImages(
        Guid id,
        [FromForm] UpdateBlogRequest request,
        [FromForm] IFormFile? thumbnailFile,
        [FromForm] IFormFile? heroImageFile,
        CancellationToken cancellationToken = default)
    {
        var existing = await _blogService.GetBlogByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (thumbnailFile != null)
        {
            await DeleteBlogImageAsync(existing.ThumbnailUrl, id, cancellationToken);

            var (thumbnailUrl, error) = await TryUploadBlogImageAsync(
                thumbnailFile,
                ThumbnailFolder,
                "blog-thumbnail",
                id,
                cancellationToken);

            if (error is not null)
            {
                return error;
            }

            request.ThumbnailUrl = thumbnailUrl;
        }

        if (heroImageFile != null)
        {
            await DeleteBlogImageAsync(existing.HeroImageUrl, id, cancellationToken);

            var (heroUrl, error) = await TryUploadBlogImageAsync(
                heroImageFile,
                HeroFolder,
                "blog-hero-image",
                id,
                cancellationToken);

            if (error is not null)
            {
                return error;
            }

            request.HeroImageUrl = heroUrl;
        }

        var blog = await _blogService.UpdateBlogAsync(id, request, cancellationToken);
        return Ok(blog);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBlog(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _blogService.DeleteBlogAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Approve a blog (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveBlog(
        Guid id,
        [FromBody] ApproveBlogRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var blog = await _blogService.ApproveBlogAsync(id, request?.Featured, cancellationToken);
        return Ok(blog);
    }

    /// <summary>
    /// Reject a blog (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Role:Admin,Staff,Doctor")]
    [ProducesResponseType(typeof(BlogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectBlog(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var blog = await _blogService.RejectBlogAsync(id, cancellationToken);
        return Ok(blog);
    }

    private async Task<IActionResult> CreateBlogInternalAsync(
        CreateBlogRequest request,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var blog = await _blogService.CreateBlogAsync(request, createdBy, cancellationToken);
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return CreatedAtAction(nameof(GetBlog), new { id = blog.Id, version = apiVersion }, blog);
    }

    private async Task<(string? Url, IActionResult? ErrorResult)> TryUploadBlogImageAsync(
        IFormFile? imageFile,
        string folder,
        string entityType,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (imageFile is null)
        {
            return (null, null);
        }

        var config = new FileUploadConfig
        {
            AllowedExtensions = AllowedImageExtensions,
            MaxSizeInMB = MaxImageSizeMb,
            Folder = folder,
            SuccessMessage = $"{entityType} uploaded successfully",
            EntityType = entityType
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(
            imageFile,
            config,
            accountId,
            _logger,
            cancellationToken);

        if (!uploadResult.Success || uploadResult.UploadResult is null)
        {
            _logger.LogWarning("Failed to upload {EntityType} for blog {BlogId}: {Error}", entityType, accountId, uploadResult.ErrorMessage);
            return (null, BadRequest(new { error = uploadResult.ErrorMessage ?? "Upload failed" }));
        }

        var url = uploadResult.UploadResult.CloudFrontUrl ?? uploadResult.UploadResult.FileUrl;
        return (url, null);
    }

    private async Task DeleteBlogImageAsync(
        string? imageUrl,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var deleteConfig = new FileDeletionConfig
        {
            FileUrl = imageUrl,
            ExpectedFolder = ExpectedFolderRoot,
            SuccessMessage = "Blog image deleted successfully",
            EntityType = "blog-image"
        };

        var deleteResult = await _uploadOrchestrator.DeleteFileAsync(
            deleteConfig,
            accountId,
            _logger,
            cancellationToken);

        if (!deleteResult.Success)
        {
            _logger.LogWarning("Failed to delete blog image for blog {BlogId}: {Error}", accountId, deleteResult.ErrorMessage);
        }
    }
}


