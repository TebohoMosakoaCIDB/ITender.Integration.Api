using System.Text.Json;
using System.Text.Json.Serialization;

namespace ITender.Integration.Api.Common;

/// <summary>BRS 18.1 error categories.</summary>
public enum ErrorCategory
{
    Validation, ReferenceData, BusinessRule, Authentication, Authorisation, Duplicate, Transient, Permanent
}

public sealed record ApiError(string Code, string Message, string? Field = null, ErrorCategory Category = ErrorCategory.Validation);

/// <summary>BRS 17.2 minimum transaction contract (request side).</summary>
public sealed class TransactionContext
{
    public string CorrelationId { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public string SourceSystem { get; init; } = "";
    public string TargetSystem { get; init; } = "";
    public string BusinessObject { get; init; } = "";
    public string BusinessKey { get; init; } = "";
    public string Operation { get; init; } = "";
}

/// <summary>BRS 17.2 minimum transaction contract (response side).</summary>
public sealed class TransactionResponse<T>
{
    public string CorrelationId { get; set; } = "";
    public string SourceSystem { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public string BusinessObject { get; set; } = "";
    public string BusinessKey { get; set; } = "";
    public string Operation { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Succeeded | Failed | Duplicate</summary>
    public string Status { get; set; } = "Failed";
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();

    [JsonIgnore] public int HttpStatus { get; set; } = 200;

    public static TransactionResponse<T> For(TransactionContext c) => new()
    {
        CorrelationId = c.CorrelationId,
        SourceSystem = c.SourceSystem,
        TargetSystem = c.TargetSystem,
        BusinessObject = c.BusinessObject,
        BusinessKey = c.BusinessKey,
        Operation = c.Operation
    };

    public TransactionResponse<T> Ok(T data, int httpStatus = 200)
    {
        Status = "Succeeded"; Data = data; HttpStatus = httpStatus; return this;
    }

    public TransactionResponse<T> Fail(IEnumerable<ApiError> errors, int? httpStatus = null)
    {
        Status = "Failed";
        Errors.AddRange(errors);
        HttpStatus = httpStatus ?? Errors.Select(e => StatusFor(e.Category)).DefaultIfEmpty(400).Max();
        return this;
    }

    public static int StatusFor(ErrorCategory c) => c switch
    {
        ErrorCategory.Validation or ErrorCategory.ReferenceData or ErrorCategory.BusinessRule => 422,
        ErrorCategory.Authentication => 401,
        ErrorCategory.Authorisation => 403,
        ErrorCategory.Duplicate => 409,
        ErrorCategory.Transient => 503,
        _ => 400
    };
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}
