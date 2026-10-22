using FluentValidation;
using Microsoft.Extensions.Options;

namespace TaskTracker.Api;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IOptions<TaskOptions> options)
    {
        var maxTitleLength = options.Value.MaxTitleLength;

        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => title.Trim().Length <= maxTitleLength)
            .WithMessage($"Title must be at most {maxTitleLength} characters.");
    }
}
