using FluentValidation;
using ITender.Integration.Api.TranferObjects;

namespace ITender.Integration.Api.Validation;

public sealed class AwardTenderValidator : AbstractValidator<AwardTenderRequest>
{
    public AwardTenderValidator()
    {
        RuleFor(x => x.ContractTitle).NotEmpty();
        RuleFor(x => x.AwardDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.TotalContractValueInclVat).NotEmpty().GreaterThan(0);

        // BR-19: 80/20 Rule Assumption
        RuleFor(x => x.FinancialPoints)
            .LessThanOrEqualTo(80).WithMessage("Financial points cannot exceed 80 (BR-19).");
        RuleFor(x => x.PreferencePoints)
            .LessThanOrEqualTo(20).WithMessage("Preference points cannot exceed 20 (BR-19).");

        // Contractors
        RuleFor(x => x.Contractors).NotEmpty().WithMessage("At least one contractor is required.");

        RuleForEach(x => x.Contractors).ChildRules(contractor =>
        {
            contractor.RuleFor(c => c.ContractorId).NotEmpty();
            contractor.RuleFor(c => c.JvSharePercentage).InclusiveBetween(0, 100);
        });

        // BR-21: JV Share Percentages should total 100%
        RuleFor(x => x.Contractors)
            .Must(c => c.Sum(x => x.JvSharePercentage) == 100)
            .When(x => x.Contractors.Count > 1)
            .WithMessage("The sum of all JV share percentages must equal exactly 100% (BR-21).");
    }
}