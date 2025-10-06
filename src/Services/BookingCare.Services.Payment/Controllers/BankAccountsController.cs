using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller quản lý tài khoản ngân hàng của user
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
public class BankAccountsController : BaseApiController
{
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
    /// Lấy bank account theo ID
    /// </summary>
    /// <param name="id">ID của bank account</param>
    /// <returns>Thông tin bank account</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBankAccount(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID bank account không hợp lệ");
            }

            var bankAccount = await _bankAccountService.GetByIdAsync(id);
            if (bankAccount == null)
            {
                return NotFound($"Bank account với ID {id} không tìm thấy");
            }

            return Success(bankAccount, "Lấy bank account thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank account with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy bank account" });
        }
    }

    /// <summary>
    /// Lấy tất cả bank accounts của user
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <returns>Danh sách bank accounts</returns>
    [HttpGet("user/{userId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBankAccountsByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("User ID không hợp lệ");
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
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy danh sách bank accounts" });
        }
    }

    /// <summary>
    /// Lấy bank accounts của user với phân trang
    /// </summary>
    /// <param name="request">Thông tin phân trang</param>
    /// <returns>Danh sách bank accounts có phân trang</returns>
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
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var pagedResult = await _bankAccountService.GetPagedByUserIdAsync(request);
            return Paginated(pagedResult, "Lấy danh sách bank accounts có phân trang thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged bank accounts for user: {UserId}", request.UserId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy danh sách bank accounts" });
        }
    }

    /// <summary>
    /// Lấy bank account mặc định của user
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <returns>Bank account mặc định</returns>
    [HttpGet("user/{userId}/default")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDefaultBankAccount(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("User ID không hợp lệ");
            }

            var defaultAccount = await _bankAccountService.GetDefaultByUserIdAsync(userId);
            if (defaultAccount == null)
            {
                return NotFound($"User {userId} chưa có bank account mặc định");
            }

            return Success(defaultAccount, "Lấy bank account mặc định thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default bank account for user: {UserId}", userId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy bank account mặc định" });
        }
    }

    /// <summary>
    /// Tạo bank account mới
    /// </summary>
    /// <param name="request">Thông tin bank account</param>
    /// <returns>Bank account được tạo</returns>
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
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var bankAccount = await _bankAccountService.CreateAsync(request);
            return Created(bankAccount, "Tạo bank account thành công");
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
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi tạo bank account" });
        }
    }

    /// <summary>
    /// Cập nhật bank account
    /// </summary>
    /// <param name="id">ID của bank account</param>
    /// <param name="request">Thông tin cập nhật</param>
    /// <returns>Bank account đã cập nhật</returns>
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
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var bankAccount = await _bankAccountService.UpdateAsync(request);
            return Success(bankAccount, "Cập nhật bank account thành công");
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
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi cập nhật bank account" });
        }
    }

    /// <summary>
    /// Xóa bank account
    /// </summary>
    /// <param name="id">ID của bank account</param>
    /// <returns>Kết quả xóa</returns>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteBankAccount(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID bank account không hợp lệ");
            }

            var result = await _bankAccountService.DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Bank account với ID {id} không tìm thấy");
            }

            return Success("Xóa bank account thành công");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting bank account");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank account with ID: {BankAccountId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi xóa bank account" });
        }
    }

    /// <summary>
    /// Đặt bank account làm mặc định
    /// </summary>
    /// <param name="id">ID của bank account</param>
    /// <returns>Bank account đã được đặt làm mặc định</returns>
    [HttpPut("{id}/set-default")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID bank account không hợp lệ");
            }

            var bankAccount = await _bankAccountService.SetAsDefaultAsync(id);
            return Success(bankAccount, "Đặt bank account làm mặc định thành công");
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
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi đặt bank account làm mặc định" });
        }
    }

    /// <summary>
    /// Kích hoạt/vô hiệu hóa bank account
    /// </summary>
    /// <param name="id">ID của bank account</param>
    /// <returns>Bank account với trạng thái mới</returns>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleActiveStatus(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID bank account không hợp lệ");
            }

            var bankAccount = await _bankAccountService.ToggleActiveStatusAsync(id);
            return Success(bankAccount, "Thay đổi trạng thái bank account thành công");
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
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi thay đổi trạng thái bank account" });
        }
    }
}