using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for managing user's bank accounts
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
public class BankAccountsController : BaseApiController
{
    private const string InvalidBankAccountIdMessage = "Invalid bank account ID";

    private readonly IBankAccountService _bankAccountService;
    private readonly IValidator<CreateBankAccountRequest> _createValidator;
    private readonly IValidator<UpdateBankAccountRequest> _updateValidator;
    private readonly IValidator<GetBankAccountsRequest> _getValidator;
    private readonly ILogger<BankAccountsController> _logger;

    public BankAccountsController(
        IBankAccountService bankAccountService,
        IValidator<CreateBankAccountRequest> createValidator,
        IValidator<UpdateBankAccountRequest> updateValidator,
        IValidator<GetBankAccountsRequest> getValidator,
        ILogger<BankAccountsController> logger)
    {
        _bankAccountService = bankAccountService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _getValidator = getValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get bank account by ID
    /// </summary>
    /// <param name="id">Bank account ID</param>
    /// <returns>Bank account info</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBankAccount(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(InvalidBankAccountIdMessage);
            }

            var bankAccount = await _bankAccountService.GetByIdAsync(id);
            if (bankAccount == null)
            {
                return NotFound($"Bank account with ID {id} was not found");
            }

            return Success(bankAccount, "Get bank account successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank account with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the bank account" });
        }
    }

    /// <summary>
    /// Get all bank accounts of a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of bank accounts</returns>
    [HttpGet("user/{userId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBankAccountsByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var bankAccounts = await _bankAccountService.GetByUserIdAsync(userId);
            var accountsList = bankAccounts.ToList(); // Convert to list to get count
            var count = accountsList.Count;

            var responseData = new
            {
                accounts = accountsList,
                count = count
            };

            return Success(responseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank accounts for user: {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while retrieving the list of bank accounts" });
        }
    }

    /// <summary>
    /// Get user bank accounts with pagination
    /// </summary>
    /// <param name="request">Paging info</param>
    /// <returns>Paged list of bank accounts</returns>
    [HttpPost("search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPagedBankAccounts([FromBody] GetBankAccountsRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _getValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var pagedResult = await _bankAccountService.GetPagedByUserIdAsync(request);
            return Paginated(pagedResult, "Get paged bank accounts successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged bank accounts for user: {UserId}", request.UserId);
            return StatusCode(500, new { Message = "An error occurred while retrieving the list of bank accounts" });
        }
    }

    /// <summary>
    /// Get default bank account of a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Default bank account</returns>
    [HttpGet("user/{userId}/default")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDefaultBankAccount(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var defaultAccount = await _bankAccountService.GetDefaultByUserIdAsync(userId);
            if (defaultAccount == null)
            {
                return NotFound($"User {userId} does not have a default bank account");
            }

            return Success(defaultAccount, "Get default bank account successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default bank account for user: {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while retrieving the default bank account" });
        }
    }

    /// <summary>
    /// Create a new bank account
    /// </summary>
    /// <param name="request">Bank account info</param>
    /// <returns>Created bank account</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var bankAccount = await _bankAccountService.CreateAsync(request);
            return Created(bankAccount, "Create bank account successful");
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict when creating bank account");
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating bank account");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bank account");
            return StatusCode(500, new { Message = "An error occurred while creating the bank account" });
        }
    }

    /// <summary>
    /// Update bank account
    /// </summary>
    /// <param name="id">Bank account ID</param>
    /// <param name="request">Update info</param>
    /// <returns>Updated bank account</returns>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateBankAccount(Guid id, [FromBody] UpdateBankAccountRequest request)
    {
        try
        {
            request.Id = id; // Ensure ID matches route parameter

            // Validate request
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var bankAccount = await _bankAccountService.UpdateAsync(request);
            return Success(bankAccount, "Update bank account successful");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Bank account not found when updating");
            return NotFound(ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict when updating bank account - duplicate account number");
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating bank account");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank account with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating the bank account" });
        }
    }

    /// <summary>
    /// Smart delete or deactivate bank account
    /// If the bank account is linked to RefundHistories -> deactivate only
    /// If not linked -> delete permanently
    /// </summary>
    /// <param name="id">Bank account ID</param>
    /// <returns>Smart delete result</returns>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteBankAccount(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(InvalidBankAccountIdMessage);
            }

            var result = await _bankAccountService.SmartDeleteAsync(id);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return result.Action switch
            {
                BankAccountDeleteAction.Deleted =>
                    Success(result.Message),

                BankAccountDeleteAction.Deactivated =>
                    Success(new
                    {
                        message = result.Message,
                        action = "deactivated",
                        bankAccount = result.UpdatedBankAccount,
                        refundHistoriesCount = result.RefundHistoriesCount
                    }, result.Message),

                _ => BadRequest("Unknown action")
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting bank account");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank account with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the bank account" });
        }
    }

    /// <summary>
    /// Set bank account as default
    /// </summary>
    /// <param name="id">Bank account ID</param>
    /// <returns>Bank account set as default</returns>
    [HttpPut("{id}/set-default")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(InvalidBankAccountIdMessage);
            }

            var bankAccount = await _bankAccountService.SetAsDefaultAsync(id);
            return Success(bankAccount, "Set bank account as default successful");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Bank account not found when setting as default");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when setting bank account as default");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting bank account as default with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "An error occurred while setting the bank account as default" });
        }
    }

    /// <summary>
    /// Toggle bank account active status
    /// </summary>
    /// <param name="id">Bank account ID</param>
    /// <returns>Bank account with new status</returns>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleActiveStatus(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(InvalidBankAccountIdMessage);
            }

            var bankAccount = await _bankAccountService.ToggleActiveStatusAsync(id);
            return Success(bankAccount, "Toggle bank account status successful");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Bank account not found when toggling status");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when toggling bank account status");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling bank account status with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "An error occurred while changing the bank account status" });
        }
    }
}