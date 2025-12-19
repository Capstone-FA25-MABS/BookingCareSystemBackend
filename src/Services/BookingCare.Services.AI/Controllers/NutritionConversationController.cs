using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for nutrition conversation flow
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/nutrition-conversation")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class NutritionConversationController : BaseApiController
{
    private readonly INutritionConversationService _nutritionConversationService;
    private readonly ILogger<NutritionConversationController> _logger;

    public NutritionConversationController(
        INutritionConversationService nutritionConversationService,
        ILogger<NutritionConversationController> logger)
    {
        _nutritionConversationService = nutritionConversationService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new nutrition conversation
    /// </summary>
    [HttpPost("start")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> StartNutritionConversation(
        [FromBody] StartNutritionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var response = await _nutritionConversationService.StartConversationAsync(
                userId,
                request.SessionId,
                cancellationToken);

            return Success(response, "Nutrition conversation started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting nutrition conversation");
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi bắt đầu tạo kế hoạch dinh dưỡng"
            });
        }
    }

    /// <summary>
    /// Answer a nutrition question
    /// </summary>
    [HttpPost("answer")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> AnswerNutritionQuestion(
        [FromBody] AnswerNutritionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var response = await _nutritionConversationService.ProcessAnswerAsync(
                userId,
                request.SessionId,
                request.Answer,
                cancellationToken);

            return Success(response, "Answer processed successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in nutrition conversation");
            return StatusCode(400, new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing nutrition answer");
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi xử lý câu trả lời"
            });
        }
    }
}
