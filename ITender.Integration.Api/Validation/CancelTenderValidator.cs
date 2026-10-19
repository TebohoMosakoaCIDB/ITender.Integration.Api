using FluentValidation;
using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Validation;

public sealed class CancelTenderValidator : AbstractValidator<CancelTenderRequest>
{
    // The specific GUID from the CSV for "Other"
    private static readonly Guid OtherReasonId = Guid.Parse("cfde742c-80c6-e111-a22d-00155d022301");

    public CancelTenderValidator()
    {
        RuleFor(x => x.ReasonId).NotEmpty()
            .WithMessage("Cancellation reason is required.");

        RuleFor(x => x.DecisionDate)
            .NotEmpty().WithMessage("Decision date is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
                .WithMessage("Decision date cannot be in the future.")
            .When(x => x.DecisionDate.HasValue);

        // If "Other" is selected, ReasonOther must be provided
        When(x => x.ReasonId == OtherReasonId, () =>
        {
            RuleFor(x => x.ReasonOther)
                .NotEmpty()
                .WithMessage("When selecting 'Other', you must provide the reason in the 'ReasonOther' field.");
        });
    }
}