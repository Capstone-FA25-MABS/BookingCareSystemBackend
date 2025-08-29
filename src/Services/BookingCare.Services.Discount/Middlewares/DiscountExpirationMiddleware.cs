using BookingCare.Services.Discount.Services;

namespace BookingCare.Services.Discount.Middlewares;

public class DiscountExpirationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DiscountExpirationMiddleware> _logger;

    public DiscountExpirationMiddleware(RequestDelegate next, ILogger<DiscountExpirationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDiscountService discountService)
    {
        // Check and update expired discounts periodically
        // This is a simple implementation - in production, you might want to use a background service
        if (ShouldCheckExpiredDiscounts(context))
        {
            try
            {
                var expiredCount = await discountService.UpdateExpiredDiscountsAsync();
                if (expiredCount > 0)
                {
                    _logger.LogInformation("Updated {Count} expired discounts", expiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expired discounts");
                // Don't throw - this shouldn't block the request
            }
        }

        await _next(context);
    }

    private static bool ShouldCheckExpiredDiscounts(HttpContext context)
    {
        // Only check on specific endpoints to avoid checking on every request
        var path = context.Request.Path.Value?.ToLower();
        return path != null && (
            path.Contains("/discounts/validate") ||
            path.Contains("/discounts/use") ||
            path.Contains("/discounts/applicable")
        );
    }
}
