using FluentValidation;

namespace BookingCare.Services.Review.Extensions;

/// <summary>
/// Custom validation extensions for common business rules
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates that a GUID is not empty
    /// </summary>
    public static IRuleBuilderOptions<T, Guid> NotEmptyGuid<T>(this IRuleBuilder<T, Guid> ruleBuilder)
    {
        return ruleBuilder
            .NotEqual(Guid.Empty)
            .WithMessage("'{PropertyName}' must not be empty");
    }

    /// <summary>
    /// Validates that a nullable GUID is not empty when it has value
    /// </summary>
    public static IRuleBuilderOptions<T, Guid?> NotEmptyGuidWhenHasValue<T>(this IRuleBuilder<T, Guid?> ruleBuilder)
    {
        return ruleBuilder
            .Must(guid => !guid.HasValue || guid.Value != Guid.Empty)
            .WithMessage("'{PropertyName}' must not be empty when provided");
    }

    /// <summary>
    /// Validates that a nullable GUID is not empty and is required
    /// </summary>
    public static IRuleBuilderOptions<T, Guid?> NotEmptyGuidRequired<T>(this IRuleBuilder<T, Guid?> ruleBuilder)
    {
        return ruleBuilder
            .NotNull()
            .WithMessage("'{PropertyName}' is required")
            .Must(guid => guid.HasValue && guid.Value != Guid.Empty)
            .WithMessage("'{PropertyName}' must not be empty");
    }

    /// <summary>
    /// Validates MongoDB ObjectId format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidObjectId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(BeValidObjectId)
            .WithMessage("'{PropertyName}' must be a valid MongoDB ObjectId (24 character hex string)");
    }

    /// <summary>
    /// Validates that string is not null, empty, or whitespace with custom message
    /// </summary>
    public static IRuleBuilderOptions<T, string> NotEmptyWithMessage<T>(this IRuleBuilder<T, string> ruleBuilder, string customMessage)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage(customMessage);
    }

    /// <summary>
    /// Validates rating is within acceptable range with descriptive message
    /// </summary>
    public static IRuleBuilderOptions<T, int> ValidRating<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 (Poor) and 5 (Excellent) stars");
    }

    /// <summary>
    /// Validates comment length with user-friendly message
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidComment<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Comment is required - please share your experience")
            .MinimumLength(5)
            .WithMessage("Comment must be at least 5 characters long")
            .MaximumLength(1000)
            .WithMessage("Comment cannot exceed 1000 characters")
            .Must(NotContainOnlyWhitespace)
            .WithMessage("Comment cannot contain only whitespace characters");
    }

    /// <summary>
    /// Validates reply content with appropriate constraints
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidReplyContent<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Reply content is required")
            .MinimumLength(3)
            .WithMessage("Reply content must be at least 3 characters long")
            .MaximumLength(500)
            .WithMessage("Reply content cannot exceed 500 characters")
            .Must(NotContainOnlyWhitespace)
            .WithMessage("Reply content cannot contain only whitespace characters");
    }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Page number cannot exceed 10,000");
    }

    /// <summary>
    /// Validates page size parameters
    /// </summary>
    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100 items");
    }

    /// <summary>
    /// Validates batch operation size limits
    /// </summary>
    public static IRuleBuilderOptions<T, int> ValidBatchSize<T>(this IRuleBuilder<T, int> ruleBuilder, int maxSize = 100)
    {
        return ruleBuilder
            .InclusiveBetween(1, maxSize)
            .WithMessage($"Batch size must be between 1 and {maxSize} items for optimal performance");
    }

    /// <summary>
    /// Validates that rating range is logical
    /// </summary>
    public static IRuleBuilderOptions<T, int?> ValidRatingRange<T>(this IRuleBuilder<T, int?> ruleBuilder,
        Func<T, int?> minRatingSelector)
    {
        return ruleBuilder
            .Must((model, maxRating) =>
            {
                var minRating = minRatingSelector(model);
                if (!minRating.HasValue || !maxRating.HasValue) return true;
                return maxRating >= minRating;
            })
            .WithMessage("Maximum rating must be greater than or equal to minimum rating");
    }

    #region Private Helper Methods

    private static bool BeValidObjectId(string? objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return false;
        if (objectId.Length != 24) return false;

        return objectId.All(c =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F'));
    }

    private static bool NotContainOnlyWhitespace(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    #endregion
}