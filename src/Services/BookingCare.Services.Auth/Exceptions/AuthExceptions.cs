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
    public AccountNotFoundException(Guid accountId)
        : base("Account", accountId, "ACCOUNT_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when role is not found
/// </summary>
public class RoleNotFoundException : NotFoundException
{
    public RoleNotFoundException(Guid roleId)
        : base("Role", roleId, "ROLE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when permission is not found
/// </summary>
public class PermissionNotFoundException : NotFoundException
{
    public PermissionNotFoundException(Guid permissionId)
        : base("Permission", permissionId, "PERMISSION_NOT_FOUND")
    {
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
}

/// <summary>
/// Exception thrown when account already exists
/// </summary>
public class AccountConflictException : ConflictException
{
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
}

/// <summary>
/// Exception thrown when trying to assign a role that account already has
/// </summary>
public class RoleAlreadyAssignedException : ConflictException
{
    public RoleAlreadyAssignedException(Guid accountId, string roleName)
        : base($"Account already has role '{roleName}'.", "ROLE_ALREADY_ASSIGNED")
    {
        Details["AccountId"] = accountId;
        Details["RoleName"] = roleName;
    }
}

/// <summary>
/// Exception thrown when trying to assign a permission that role already has
/// </summary>
public class PermissionAlreadyAssignedException : ConflictException
{
    public PermissionAlreadyAssignedException(Guid roleId, Guid permissionId)
        : base($"Role already has this permission.", "PERMISSION_ALREADY_ASSIGNED")
    {
        Details["RoleId"] = roleId;
        Details["PermissionId"] = permissionId;
    }
}

/// <summary>
/// Exception thrown when trying to remove a role that account doesn't have
/// </summary>
public class RoleNotAssignedException : NotFoundException
{
    public RoleNotAssignedException(Guid accountId, string roleName)
        : base($"Account does not have role '{roleName}'.", "ROLE_NOT_ASSIGNED")
    {
        Details["AccountId"] = accountId;
        Details["RoleName"] = roleName;
    }
}

/// <summary>
/// Exception thrown when trying to remove a permission that role doesn't have
/// </summary>
public class PermissionNotAssignedException : NotFoundException
{
    public PermissionNotAssignedException(Guid roleId, Guid permissionId)
        : base($"Role does not have this permission.", "PERMISSION_NOT_ASSIGNED")
    {
        Details["RoleId"] = roleId;
        Details["PermissionId"] = permissionId;
    }
}