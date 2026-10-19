namespace ITender.Integration.Api.TranferObjects
{
    public sealed record LookupItemDto(Guid Id, string Name, string? Message = null);
}
