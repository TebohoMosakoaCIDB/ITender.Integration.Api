using ITender.Integration.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ITender.Integration.Api.Validation;

public interface ISastClock
{
    DateOnly Today { get; }
}

public sealed class SastClock : ISastClock
{
    private static readonly TimeZoneInfo Sast = Resolve();
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, Sast));

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
    }
}

public interface IWorkingDayCalculator
{
    bool IsWorkingDay(DateOnly date);
    /// <summary>Returns the date reached after adding <paramref name="days"/> working days (start excluded).</summary>
    DateOnly AddWorkingDays(DateOnly start, int days);
    /// <summary>Working days in (from, to] - from excluded, to included.</summary>
    int CountWorkingDays(DateOnly from, DateOnly to);
}

/// <summary>BR-10: weekends and public holidays are excluded.</summary>
public sealed class WorkingDayCalculator : IWorkingDayCalculator
{
    private readonly HashSet<DateOnly> _holidays;

    public WorkingDayCalculator(IOptions<IntegrationOptions> options)
        => _holidays = options.Value.PublicHolidays.ToHashSet();

    public bool IsWorkingDay(DateOnly d) =>
        d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(d);

    public DateOnly AddWorkingDays(DateOnly start, int days)
    {
        var current = start;
        var added = 0;
        while (added < days)
        {
            current = current.AddDays(1);
            if (IsWorkingDay(current)) added++;
        }
        return current;
    }

    public int CountWorkingDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var d = from.AddDays(1); d <= to; d = d.AddDays(1))
            if (IsWorkingDay(d)) count++;
        return count;
    }
}

/// <summary>
/// Reference-data checks against CIDB master data held in Dataverse (BR-02..BR-05, FR-005).
/// </summary>
public interface IReferenceDataService
{
    Task<bool> EmployerExistsAsync(string employerName, CancellationToken ct);
    Task<bool> ContractTypeExistsAsync(string contractType, CancellationToken ct);
    Task<bool> TenderValueRangeExistsAsync(string valueRange, CancellationToken ct);
    Task<bool> IsValidLocationAsync(string province, string district, string? localMunicipality, CancellationToken ct);
    Task<bool> IsValidClassAndSubCategoryAsync(string classOfWorks, string subCategory, CancellationToken ct);
}

/// <summary>
/// TODO: replace with a Dataverse-backed implementation (your master-data entity names are needed).
/// It deliberately logs a warning on every call so it cannot be mistaken for real validation.
/// </summary>
public sealed class PlaceholderReferenceDataService(ILogger<PlaceholderReferenceDataService> log) : IReferenceDataService
{
    public Task<bool> EmployerExistsAsync(string employerName, CancellationToken ct) => Pass(nameof(EmployerExistsAsync));
    public Task<bool> ContractTypeExistsAsync(string contractType, CancellationToken ct) => Pass(nameof(ContractTypeExistsAsync));
    public Task<bool> TenderValueRangeExistsAsync(string valueRange, CancellationToken ct) => Pass(nameof(TenderValueRangeExistsAsync));
    public Task<bool> IsValidLocationAsync(string p, string d, string? l, CancellationToken ct) => Pass(nameof(IsValidLocationAsync));
    public Task<bool> IsValidClassAndSubCategoryAsync(string c, string s, CancellationToken ct) => Pass(nameof(IsValidClassAndSubCategoryAsync));

    private Task<bool> Pass(string check)
    {
        log.LogWarning("Reference-data check {Check} is NOT IMPLEMENTED - returning true.", check);
        return Task.FromResult(true);
    }
}
