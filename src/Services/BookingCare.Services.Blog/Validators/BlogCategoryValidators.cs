using BookingCare.Services.Blog.Models.DTOs;
using FluentValidation;

namespace BookingCare.Services.Blog.Validators;

public class CreateBlogCategoryRequestValidator : AbstractValidator<CreateBlogCategoryRequest>
{
    public CreateBlogCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
    }
}

public class UpdateBlogCategoryRequestValidator : AbstractValidator<UpdateBlogCategoryRequest>
{
    public UpdateBlogCategoryRequestValidator()
    {
        Include(new CreateBlogCategoryRequestValidator());
    }
}

