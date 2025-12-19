using BookingCare.Services.Content.Models.DTOs;
using FluentValidation;

namespace BookingCare.Services.Content.Validators;

public class CreateBlogRequestValidator : AbstractValidator<CreateBlogRequest>
{
    public CreateBlogRequestValidator()
    {
        RuleFor(x => x.TitleVi)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentVi)
            .NotEmpty();

        RuleFor(x => x.TitleEn)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.TitleEn));

        RuleFor(x => x.Tag)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Tag));

        RuleFor(x => x.Source)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.BlogCategoryId)
            .NotEqual(Guid.Empty)
            .When(x => x.BlogCategoryId.HasValue);
    }
}

public class UpdateBlogRequestValidator : AbstractValidator<UpdateBlogRequest>
{
    public UpdateBlogRequestValidator()
    {
        Include(new CreateBlogRequestValidator());
    }
}


