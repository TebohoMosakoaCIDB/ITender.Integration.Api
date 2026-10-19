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
    Task<bool> EmployerExistsAsync(Guid employerId, CancellationToken ct);
    Task<bool> ContractTypeExistsAsync(int contractTypeId, CancellationToken ct);
    Task<bool> TenderValueRangeExistsAsync(int valueRangeId, CancellationToken ct);
    Task<bool> IsValidLocationAsync(Guid provinceId, Guid metroDistrictId, Guid? localMunicipalityId, CancellationToken ct);
    Task<bool> IsValidClassAndSubCategoryAsync(Guid classId, Guid subCategoryId, CancellationToken ct);
}
