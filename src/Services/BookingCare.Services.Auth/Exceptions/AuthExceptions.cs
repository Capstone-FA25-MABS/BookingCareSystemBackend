using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Auth.Exceptions;

/// <summary>
/// Base exception for Auth service
/// </summary>
public class AuthException : BookingCareException
{
    public AuthException(
        string message, 
        string errorCode = "AUTH_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null) 
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

/// <summary>
/// Exception thrown when account is not found
/// </summary>
public class AccountNotFoundException : NotFoundException
{
    public AccountNotFoundException(string message) 
        : base(message, "ACCOUNT_NOT_FOUND")
    {
    }

    public AccountNotFoundException(Guid accountId)
        : base("Account", accountId, "ACCOUNT_NOT_FOUND")
    {
    }

    public AccountNotFoundException(string email, bool byEmail)
        : base($"Account with email '{email}' was not found.", "ACCOUNT_NOT_FOUND")
    {
        Details["Email"] = email;
    }
}

/// <summary>
/// Exception thrown when role is not found
/// </summary>
public class RoleNotFoundException : NotFoundException
{
    public RoleNotFoundException(string message) 
        : base(message, "ROLE_NOT_FOUND")
    {
    }

    public RoleNotFoundException(Guid roleId)
        : base("Role", roleId, "ROLE_NOT_FOUND")
    {
    }

    public RoleNotFoundException(string roleName, bool byName)
        : base($"Role with name '{roleName}' was not found.", "ROLE_NOT_FOUND")
    {
        Details["RoleName"] = roleName;
    }
}

/// <summary>
/// Exception thrown when permission is not found
/// </summary>
public class PermissionNotFoundException : NotFoundException
{
    public PermissionNotFoundException(string message) 
        : base(message, "PERMISSION_NOT_FOUND")
    {
    }

    public PermissionNotFoundException(Guid permissionId)
        : base("Permission", permissionId, "PERMISSION_NOT_FOUND")
    {
    }

    public PermissionNotFoundException(string permissionName, bool byName)
        : base($"Permission with name '{permissionName}' was not found.", "PERMISSION_NOT_FOUND")
    {
        Details["PermissionName"] = permissionName;
    }
}

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public class AuthenticationException : UnauthorizedException
{
    public AuthenticationException(string message) 
        : base(message, "AUTHENTICATION_FAILED")
    {
    }

    public AuthenticationException(string email, bool byEmail)
        : base($"Authentication failed for email '{email}'.", "AUTHENTICATION_FAILED")
    {
        Details["Email"] = email;
    }
}

/// <summary>
/// Exception thrown when authorization fails
/// </summary>
public class AuthorizationException : ForbiddenException
{
    public AuthorizationException(string message) 
        : base(message, "AUTHORIZATION_FAILED")
    {
    }

    public AuthorizationException(string email, string requiredRole)
        : base($"User '{email}' does not have required role '{requiredRole}'.", "AUTHORIZATION_FAILED")
    {
        Details["Email"] = email;
        Details["RequiredRole"] = requiredRole;
    }
}

/// <summary>
/// Exception thrown when account validation fails
/// </summary>
public class AccountValidationException : ValidationException
{
    public AccountValidationException(string message) 
        : base(message, null, "ACCOUNT_VALIDATION_ERROR")
    {
    }

    public AccountValidationException(List<ValidationError> validationErrors)
        : base("Account validation failed", validationErrors, "ACCOUNT_VALIDATION_ERROR")
    {
    }
}

/// <summary>
/// Exception thrown when account already exists
/// </summary>
public class AccountConflictException : ConflictException
{
    public AccountConflictException(string message) 
        : base(message, "ACCOUNT_CONFLICT")
    {
    }

    public AccountConflictException(string value, string fieldType)
        : base($"Account with {fieldType} '{value}' already exists.", "ACCOUNT_CONFLICT")
    {
        Details[fieldType] = value;
    }
}

/// <summary>
/// Exception thrown when role already exists
/// </summary>
public class RoleConflictException : ConflictException
{
    public RoleConflictException(string message) 
        : base(message, "ROLE_CONFLICT")
    {
    }

    public RoleConflictException(string roleName, bool byName)
        : base($"Role with name '{roleName}' already exists.", "ROLE_CONFLICT")
    {
        Details["RoleName"] = roleName;
    }
}

/// <summary>
/// Exception thrown when permission already exists
/// </summary>
public class PermissionConflictException : ConflictException
{
    public PermissionConflictException(string message) 
        : base(message, "PERMISSION_CONFLICT")
    {
    }

    public PermissionConflictException(string permissionName, bool byName)
        : base($"Permission with name '{permissionName}' already exists.", "PERMISSION_CONFLICT")
    {
        Details["PermissionName"] = permissionName;
    }
}

/// <summary>
/// Exception thrown when role validation fails
/// </summary>
public class RoleValidationException : ValidationException
{
    public RoleValidationException(string message) 
        : base(message, null, "ROLE_VALIDATION_ERROR")
    {
    }

    public RoleValidationException(List<ValidationError> validationErrors)
        : base("Role validation failed", validationErrors, "ROLE_VALIDATION_ERROR")
    {
    }
}

/// <summary>
/// Exception thrown when permission validation fails
/// </summary>
public class PermissionValidationException : ValidationException
{
    public PermissionValidationException(string message) 
        : base(message, null, "PERMISSION_VALIDATION_ERROR")
    {
    }

    public PermissionValidationException(List<ValidationError> validationErrors)
        : base("Permission validation failed", validationErrors, "PERMISSION_VALIDATION_ERROR")
    {
    }
}
