using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Shared.Common.Helpers;

/// <summary>
/// Helper class for creating validation errors
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Creates a validation error for a required field
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>A validation error</returns>
    public static ValidationError RequiredField(string fieldName)
    {
        return new ValidationError(fieldName, $"{fieldName} is required");
    }

    /// <summary>
    /// Creates a validation error for an invalid email format
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="email">The invalid email value</param>
    /// <returns>A validation error</returns>
    public static ValidationError InvalidEmail(string fieldName, string email)
    {
        return new ValidationError(fieldName, "Invalid email format", email);
    }

    /// <summary>
    /// Creates a validation error for a string that's too short
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="minLength">The minimum required length</param>
    /// <param name="actualValue">The actual value</param>
    /// <returns>A validation error</returns>
    public static ValidationError TooShort(string fieldName, int minLength, string actualValue)
    {
        return new ValidationError(fieldName, $"{fieldName} must be at least {minLength} characters long", actualValue);
    }

    /// <summary>
    /// Creates a validation error for a string that's too long
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="maxLength">The maximum allowed length</param>
    /// <param name="actualValue">The actual value</param>
    /// <returns>A validation error</returns>
    public static ValidationError TooLong(string fieldName, int maxLength, string actualValue)
    {
        return new ValidationError(fieldName, $"{fieldName} must not exceed {maxLength} characters", actualValue);
    }

    /// <summary>
    /// Creates a validation error for an invalid date range
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="message">Custom message</param>
    /// <param name="actualValue">The actual value</param>
    /// <returns>A validation error</returns>
    public static ValidationError InvalidDateRange(string fieldName, string message, DateTime actualValue)
    {
        return new ValidationError(fieldName, message, actualValue);
    }

    /// <summary>
    /// Creates a validation error for a value that's out of range
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="minValue">The minimum value</param>
    /// <param name="maxValue">The maximum value</param>
    /// <param name="actualValue">The actual value</param>
    /// <returns>A validation error</returns>
    public static ValidationError OutOfRange(string fieldName, object minValue, object maxValue, object actualValue)
    {
        return new ValidationError(fieldName, $"{fieldName} must be between {minValue} and {maxValue}", actualValue);
    }

    /// <summary>
    /// Creates a validation error for an invalid format
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="expectedFormat">The expected format</param>
    /// <param name="actualValue">The actual value</param>
    /// <returns>A validation error</returns>
    public static ValidationError InvalidFormat(string fieldName, string expectedFormat, string actualValue)
    {
        return new ValidationError(fieldName, $"{fieldName} must be in format: {expectedFormat}", actualValue);
    }

    /// <summary>
    /// Creates a validation error for a duplicate value
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="actualValue">The duplicate value</param>
    /// <returns>A validation error</returns>
    public static ValidationError DuplicateValue(string fieldName, object actualValue)
    {
        return new ValidationError(fieldName, $"{fieldName} already exists", actualValue);
    }

    /// <summary>
    /// Validates an email address format
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a phone number format (basic validation)
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove common formatting characters
        var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Check if it contains only digits and has reasonable length
        return cleanNumber.All(char.IsDigit) && cleanNumber.Length >= 10 && cleanNumber.Length <= 15;
    }

    /// <summary>
    /// Validates that a date is in the future
    /// </summary>
    /// <param name="date">The date to validate</param>
    /// <returns>True if in the future, false otherwise</returns>
    public static bool IsInFuture(DateTime date)
    {
        return date > DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that a date is not in the past
    /// </summary>
    /// <param name="date">The date to validate</param>
    /// <returns>True if today or in the future, false otherwise</returns>
    public static bool IsNotInPast(DateTime date)
    {
        return date.Date >= DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Validates that a GUID is not empty
    /// </summary>
    /// <param name="guid">The GUID to validate</param>
    /// <returns>True if not empty, false otherwise</returns>
    public static bool IsValidGuid(Guid guid)
    {
        return guid != Guid.Empty;
    }
}

/// <summary>
/// Helper class for creating paginated results
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Creates a paginated result from a list of items
    /// </summary>
    /// <typeparam name="T">The type of items</typeparam>
    /// <param name="items">The items for the current page</param>
    /// <param name="totalCount">The total count of all items</param>
    /// <param name="pageNumber">The current page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A paginated result</returns>
    public static PagedResult<T> CreatePagedResult<T>(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <param name="maxPageSize">The maximum allowed page size</param>
    /// <returns>A list of validation errors</returns>
    public static List<ValidationError> ValidatePaginationParameters(int pageNumber, int pageSize, int maxPageSize = 100)
    {
        var errors = new List<ValidationError>();

        if (pageNumber < 1)
        {
            errors.Add(new ValidationError("PageNumber", "Page number must be at least 1", pageNumber));
        }

        if (pageSize < 1)
        {
            errors.Add(new ValidationError("PageSize", "Page size must be at least 1", pageSize));
        }

        if (pageSize > maxPageSize)
        {
            errors.Add(new ValidationError("PageSize", $"Page size must not exceed {maxPageSize}", pageSize));
        }

        return errors;
    }
}
