using FluentValidation;
using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Validation;

public sealed class TerminateContractValidator : AbstractValidator<TerminateContractRequest>
{
    private const int OtherReasonId = 100000004; // From your screenshot

    public TerminateContractValidator()
    {
        RuleFor(x => x.TerminationDate)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("Termination date cannot be in the future.");

        RuleFor(x => x.ReasonId)
            .NotEmpty()
            .InclusiveBetween(100000000, 100000004)
            .WithMessage("Invalid termination reason selected.");

        // If "Other" is selected, require the free text
        When(x => x.ReasonId == OtherReasonId, () =>
        {
            RuleFor(x => x.OtherReason)
                .NotEmpty()
                .WithMessage("You must provide details when 'Other' is selected.");
        });
    }
}