namespace ITender.Integration.Api.Configuration;

public sealed class CrmOptions
{
    public const string Section = "Crm";
    public string CrmURL { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public sealed class EncryptionOptions
{
    public const string Section = "Encryption";
    public string Key { get; set; } = "";
    public string IV { get; set; } = "";
}

public sealed class ETenderApiOptions
{
    public const string Section = "ETenderApi";
    public string BaseUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class IntegrationOptions
{
    public const string Section = "Integration";
    public string SourceSystem { get; set; } = "i-Tender";

    /// <summary>BR-09: minimum advertisement period in working days.</summary>
    public int MinimumAdvertisementWorkingDays { get; set; } = 10;

    /// <summary>BR-12 / DEC-006 (unconfirmed). Null = rule disabled.</summary>
    public int? CompulsoryMeetingMinWorkingDaysBeforeClosing { get; set; }

    /// <summary>BR-10: public holidays excluded from working-day calculations.</summary>
    public List<DateOnly> PublicHolidays { get; set; } = new();

    /// <summary>DEC-007 (open): CRM option-set values per lifecycle status.</summary>
    public Dictionary<string, int> StatusOptionValues { get; set; } = new();
}
