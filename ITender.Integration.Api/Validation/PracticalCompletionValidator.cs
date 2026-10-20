using FluentValidation;
using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Validation;

public sealed class PracticalCompletionValidator : AbstractValidator<PracticalCompletionRequest>
{
    public PracticalCompletionValidator()
    {
        RuleFor(x => x.CompletionDate).NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("Practical completion date cannot be in the future.");

        RuleFor(x => x.EstimatedFinalValueInclVat).NotEmpty().GreaterThan(0);
    }
}