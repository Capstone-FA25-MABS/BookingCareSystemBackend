using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho CreateBankAccountRequest
/// </summary>
public class CreateBankAccountRequestValidator : AbstractValidator<CreateBankAccountRequest>
{
    public CreateBankAccountRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID không được để trống")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID không hợp lệ");

        RuleFor(x => x.BankCode)
            .NotEmpty()
            .WithMessage("Mã ngân hàng không được để trống")
            .MaximumLength(10)
            .WithMessage("Mã ngân hàng không được vượt quá 10 ký tự")
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Mã ngân hàng chỉ được chứa chữ cái hoa và số");

        RuleFor(x => x.BankName)
            .NotEmpty()
            .WithMessage("Tên ngân hàng không được để trống")
            .MaximumLength(255)
            .WithMessage("Tên ngân hàng không được vượt quá 255 ký tự");

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .WithMessage("Số tài khoản không được để trống")
            .MaximumLength(50)
            .WithMessage("Số tài khoản không được vượt quá 50 ký tự")
            .Matches("^[0-9]+$")
            .WithMessage("Số tài khoản chỉ được chứa các chữ số")
            .MinimumLength(6)
            .WithMessage("Số tài khoản phải có ít nhất 6 chữ số")
            .MaximumLength(20)
            .WithMessage("Số tài khoản không được vượt quá 20 chữ số");

        RuleFor(x => x.AccountName)
            .NotEmpty()
            .WithMessage("Tên chủ tài khoản không được để trống")
            .MaximumLength(255)
            .WithMessage("Tên chủ tài khoản không được vượt quá 255 ký tự")
            .Matches("^[a-zA-ZÀ-ỹĂ-ẵÂ-ẽÊ-ỷÔ-ỗÚ-ủĐđ\\s]+$")
            .WithMessage("Tên chủ tài khoản chỉ được chứa chữ cái và khoảng trắng");
    }
}

/// <summary>
/// Validator cho UpdateBankAccountRequest
/// </summary>
public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
{
    public UpdateBankAccountRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID không được để trống")
            .NotEqual(Guid.Empty)
            .WithMessage("ID không hợp lệ");

        RuleFor(x => x.BankCode)
            .MaximumLength(10)
            .WithMessage("Mã ngân hàng không được vượt quá 10 ký tự")
            .When(x => !string.IsNullOrEmpty(x.BankCode));

        RuleFor(x => x.BankCode)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Mã ngân hàng chỉ được chứa chữ cái hoa và số")
            .When(x => !string.IsNullOrEmpty(x.BankCode));

        RuleFor(x => x.BankName)
            .MaximumLength(255)
            .WithMessage("Tên ngân hàng không được vượt quá 255 ký tự")
            .When(x => !string.IsNullOrEmpty(x.BankName));

        RuleFor(x => x.AccountNumber)
            .MaximumLength(50)
            .WithMessage("Số tài khoản không được vượt quá 50 ký tự")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .Matches("^[0-9]+$")
            .WithMessage("Số tài khoản chỉ được chứa các chữ số")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .MinimumLength(6)
            .WithMessage("Số tài khoản phải có ít nhất 6 chữ số")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .MaximumLength(20)
            .WithMessage("Số tài khoản không được vượt quá 20 chữ số")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountName)
            .MaximumLength(255)
            .WithMessage("Tên chủ tài khoản không được vượt quá 255 ký tự")
            .When(x => !string.IsNullOrEmpty(x.AccountName));

        RuleFor(x => x.AccountName)
            .Matches("^[a-zA-ZÀ-ỹĂ-ẵÂ-ẽÊ-ỷÔ-ỗÚ-ủĐđ\\s]+$")
            .WithMessage("Tên chủ tài khoản chỉ được chứa chữ cái và khoảng trắng")
            .When(x => !string.IsNullOrEmpty(x.AccountName));

        // Ít nhất một trường phải được cập nhật
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.BankCode) ||
                      !string.IsNullOrEmpty(x.BankName) ||
                      !string.IsNullOrEmpty(x.AccountNumber) ||
                      !string.IsNullOrEmpty(x.AccountName) ||
                      x.IsDefault.HasValue ||
                      x.IsActive.HasValue)
            .WithMessage("Ít nhất một trường phải được cập nhật");
    }
}

/// <summary>
/// Validator cho GetBankAccountsRequest
/// </summary>
public class GetBankAccountsRequestValidator : AbstractValidator<GetBankAccountsRequest>
{
    public GetBankAccountsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID không được để trống")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID không hợp lệ");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Số trang phải lớn hơn 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Kích thước trang phải lớn hơn 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Kích thước trang không được vượt quá 100");
    }
}