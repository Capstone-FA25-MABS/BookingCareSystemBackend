using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreateBankAccountRequest
/// </summary>
public class CreateBankAccountRequestValidator : AbstractValidator<CreateBankAccountRequest>
{
    public CreateBankAccountRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is not valid");

        RuleFor(x => x.BankCode)
            .NotEmpty()
            .WithMessage("Bank code must not be empty")
            .MaximumLength(10)
            .WithMessage("Bank code must not exceed 10 characters")
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Bank code must contain only uppercase letters and digits");

        RuleFor(x => x.BankName)
            .NotEmpty()
            .WithMessage("Bank name must not be empty")
            .MaximumLength(255)
            .WithMessage("Bank name must not exceed 255 characters");

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .WithMessage("Account number must not be empty")
            .MaximumLength(50)
            .WithMessage("Account number must not exceed 50 characters")
            .Matches("^[0-9]+$")
            .WithMessage("Account number must contain digits only")
            .MinimumLength(6)
            .WithMessage("Account number must be at least 6 digits")
            .MaximumLength(20)
            .WithMessage("Account number must not exceed 20 digits");

        RuleFor(x => x.AccountName)
            .NotEmpty()
            .WithMessage("Account holder name must not be empty")
            .MaximumLength(255)
            .WithMessage("Account holder name must not exceed 255 characters")
            .Matches("^[a-zA-ZÀ-ỹĂ-ẵÂ-ẽÊ-ỷÔ-ỗÚ-ủĐđ\\s]+$")
            .WithMessage("Account holder name must contain only letters and whitespace");
    }
}

/// <summary>
/// Validator for UpdateBankAccountRequest
/// </summary>
public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
{
    public UpdateBankAccountRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Id is not valid");

        RuleFor(x => x.BankCode)
            .MaximumLength(10)
            .WithMessage("Bank code must not exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.BankCode));

        RuleFor(x => x.BankCode)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Bank code must contain only uppercase letters and digits")
            .When(x => !string.IsNullOrEmpty(x.BankCode));

        RuleFor(x => x.BankName)
            .MaximumLength(255)
            .WithMessage("Bank name must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.BankName));

        RuleFor(x => x.AccountNumber)
            .MaximumLength(50)
            .WithMessage("Account number must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .Matches("^[0-9]+$")
            .WithMessage("Account number must contain digits only")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .MinimumLength(6)
            .WithMessage("Account number must be at least 6 digits")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountNumber)
            .MaximumLength(20)
            .WithMessage("Account number must not exceed 20 digits")
            .When(x => !string.IsNullOrEmpty(x.AccountNumber));

        RuleFor(x => x.AccountName)
            .MaximumLength(255)
            .WithMessage("Account holder name must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.AccountName));

        RuleFor(x => x.AccountName)
            .Matches("^[a-zA-ZÀ-ỹĂ-ẵÂ-ẽÊ-ỷÔ-ỗÚ-ủĐđ\\s]+$")
            .WithMessage("Account holder name must contain only letters and whitespace")
            .When(x => !string.IsNullOrEmpty(x.AccountName));

        // At least one field must be updated
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.BankCode) ||
                      !string.IsNullOrEmpty(x.BankName) ||
                      !string.IsNullOrEmpty(x.AccountNumber) ||
                      !string.IsNullOrEmpty(x.AccountName) ||
                      x.IsDefault.HasValue ||
                      x.IsActive.HasValue)
            .WithMessage("At least one field must be updated");
    }
}

/// <summary>
/// Validator for GetBankAccountsRequest
/// </summary>
public class GetBankAccountsRequestValidator : AbstractValidator<GetBankAccountsRequest>
{
    public GetBankAccountsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is not valid");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must not exceed 100");
    }
}