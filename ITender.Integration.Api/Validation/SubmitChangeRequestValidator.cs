using FluentValidation;
using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Validation;

public sealed class SubmitChangeRequestValidator : AbstractValidator<SubmitChangeRequest>
{
    public SubmitChangeRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason for the change is required.");

        // Ensure at least one field is being changed
        RuleFor(x => x)
            .Must(x => x.NewClosingDateTime.HasValue || !string.IsNullOrWhiteSpace(x.NewTitle) || !string.IsNullOrWhiteSpace(x.NewDescription))
            .WithMessage("At least one field must be changed.");
    }
}