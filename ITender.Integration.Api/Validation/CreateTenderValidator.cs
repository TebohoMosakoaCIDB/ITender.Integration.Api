using FluentValidation;
using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.TranferObjects;
using Microsoft.Extensions.Options;

namespace ITender.Integration.Api.Validation;

public sealed class CreateTenderValidator : AbstractValidator<CreateTenderModel>
{
    // Replace with the real Dataverse option-set values for nv_clarificationmeeting.
    private const int Yes = 100000000;
    private const int No = 100000001;
    private static readonly int[] ValidTenderValueRangeIds =
    {
        100000008, 100000000, 100000001, 100000002,
        100000003, 100000004, 100000005, 100000006, 100000007
    };
    public CreateTenderValidator(
        ISastClock clock,
        IWorkingDayCalculator workingDays,
        IOptions<IntegrationOptions> options)
    {
        var opts = options.Value;
        var minWorkingDays = opts.MinimumAdvertisementWorkingDays;
        var minMeetingDays = opts.CompulsoryMeetingMinWorkingDaysBeforeClosing ?? 5;

        // --- Notice details ---
        RuleFor(x => x.EmployerTenderNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TendersInvitedFor).NotEmpty();
        // PreferencesOffered, EligibilityCriteria — optional per business decision

        // --- Location ---
        RuleFor(x => x.ProvinceId).NotEmpty();
        RuleFor(x => x.MetroDistrictId).NotEmpty();
        RuleFor(x => x.LocalMunicipalityId).NotEmpty()
            .WithMessage("Local municipality is required.");

        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue);
        RuleFor(x => x).Must(x =>
                (x.Latitude.HasValue && x.Longitude.HasValue) ||
                (!x.Latitude.HasValue && !x.Longitude.HasValue))
            .WithMessage("Latitude and Longitude must be provided together.");

        // --- Contract details ---
        RuleFor(x => x.TypeOfContractId).NotEmpty();
        RuleFor(x => x.ClassOfConstructionWorksId).NotEmpty();
        RuleFor(x => x.SubCategoryId).NotEmpty()
            .WithMessage("Sub-category is required.");
        RuleFor(x => x.TenderValueRangeId)
            .NotEmpty()
            .Must(id => id.HasValue && ValidTenderValueRangeIds.Contains(id.Value))
            .WithMessage("Tender value range must be one of the approved option-set values.");

        // BR-07: targeted development programme
        When(x => !string.IsNullOrWhiteSpace(x.NameOfTargetedDevelopmentProgramme), () =>
        {
            RuleFor(x => x.NationalContractorDevelopmentProgrammeId).NotEmpty()
                .WithMessage("National Contractor Development Programme is required when a targeted programme is named.");
        });

        // --- Employer ---
        RuleFor(x => x.EmployerId).NotEmpty()
            .WithMessage("Employer is required.");

        // --- Address 1 ---
        RuleFor(x => x.PrimaryAddress).NotNull();
        RuleFor(x => x.PrimaryAddress.Line1).NotEmpty().When(x => x.PrimaryAddress is not null);
        RuleFor(x => x.PrimaryAddress.City).NotEmpty().When(x => x.PrimaryAddress is not null);
        RuleFor(x => x.PrimaryAddress.Province).NotEmpty().When(x => x.PrimaryAddress is not null);
        RuleFor(x => x.PrimaryAddress.PostalCode).NotEmpty().When(x => x.PrimaryAddress is not null);

        // --- Address 2 (conditional on the flag) ---
        When(x => x.AdditionalCollectionAddress, () =>
        {
            RuleFor(x => x.AdditionalCollectionAddressDetails).NotNull()
                .WithMessage("Additional collection address details are required when the flag is set.");
            RuleFor(x => x.AdditionalCollectionAddressDetails!.Line1).NotEmpty();
            RuleFor(x => x.AdditionalCollectionAddressDetails!.City).NotEmpty();
            RuleFor(x => x.AdditionalCollectionAddressDetails!.Province).NotEmpty();
            RuleFor(x => x.AdditionalCollectionAddressDetails!.PostalCode).NotEmpty();
        });

        // --- Documents and payment ---
        RuleFor(x => x.DocumentsAvailableFrom).NotEmpty();

        // If any payment method is selected, deposit must be present
        When(x => x.MethodOfPaymentCash
               || x.MethodOfPaymentBankGuaranteedCheque
               || x.MethodOfPaymentProofOfDeposit, () =>
               {
                   RuleFor(x => x.DepositAmount)
                       .NotNull()
                       .GreaterThanOrEqualTo(0)
                       .WithMessage("Deposit amount is required when documents are sold.");
               });

        // --- Contacts ---
        RuleFor(x => x.ContactPerson)
            .NotNull()
            .Must(c => c.Count > 0)
            .WithMessage("At least one contact person is required.");

        RuleForEach(x => x.ContactPerson).ChildRules(contact =>
        {
            contact.RuleFor(c => c.PersonToQuery).NotEmpty();
            contact.RuleFor(c => c.TelephoneNumber).NotEmpty();
            contact.RuleFor(c => c.Email).NotEmpty().EmailAddress();
        });

        // --- Dates ---
        RuleFor(x => x.ClosingDateTime)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow)
                .WithMessage("Closing date must be in the future.")
            .Must((model, closing) =>
                model.DocumentsAvailableFrom.HasValue
                    ? closing <= model.DocumentsAvailableFrom.Value.AddDays(90)
                    : true)
                .WithMessage("Tender validity period cannot exceed 90 days from the documents-available date (BR-13).")
            .Must((_, closing) =>
                workingDays.CountWorkingDays(clock.Today, DateOnly.FromDateTime(closing)) >= minWorkingDays)
                .WithMessage($"Tender must be advertised for at least {minWorkingDays} working days (BR-09).");

        RuleFor(x => x.DocumentsAvailableFrom)
            .LessThan(x => x.ClosingDateTime)
            .When(x => x.DocumentsAvailableFrom.HasValue)
            .WithMessage("Documents available date must be before the closing date.");

        // --- Clarification meeting ---
        When(x => x.ClarificationMeetingRequired == Yes, () =>
        {
            RuleFor(x => x.ClarificationMeetingPlace).NotEmpty();
            RuleFor(x => x.ClarificationMeetingDateAndTime).NotEmpty();
        });

        // BR-12: compulsory meeting must be N working days before closing
        When(x => x.ClarificationMeetingRequired == Yes
               && x.ClarificationMeetingCompulsory == Yes, () =>
               {
                   RuleFor(x => x.ClarificationMeetingDateAndTime)
                       .Must((model, meeting) =>
                       {
                           if (meeting is null) return true;
                           var meetingDay = DateOnly.FromDateTime(meeting.Value);
                           var closingDay = DateOnly.FromDateTime(model.ClosingDateTime);
                           return workingDays.CountWorkingDays(meetingDay, closingDay) >= minMeetingDays;
                       })
                       .WithMessage($"Compulsory clarification meeting must be at least {minMeetingDays} working days before closing (BR-12).");
               });

        // --- Additional clarification meeting ---
        When(x => x.AdditionalClarificationMeeting == Yes, () =>
        {
            RuleFor(x => x.AddClarificationMeetingPlace).NotEmpty();
            RuleFor(x => x.AddClarificationMeetingDateAndTime).NotEmpty();
        });
    }
}