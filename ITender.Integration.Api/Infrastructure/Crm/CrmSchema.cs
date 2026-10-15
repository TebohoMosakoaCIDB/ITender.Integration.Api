namespace ITender.Integration.Api.Infrastructure.Crm;

// ---------------------------------------------------------------------------
// TODO: align every logical name below with the real Dataverse schema.
// Names marked [NEW] do not exist yet and must be created (see README).
// ---------------------------------------------------------------------------

public static class IntegrationTransactionSchema
{
    public const string Name = "nv_name";                           // primary name = correlation id
    public const string IdempotencyKey = "nv_idempotencykey";       // [NEW] text(200) - make an ALTERNATE KEY
    public const string BusinessObject = "nv_businessobject";
    public const string Operation = "nv_operation";
    public const string BusinessKey = "nv_businesskey";
    public const string SourceSystem = "nv_sourcesystem";
    public const string TargetSystem = "nv_targetsystem";
    public const string Status = "nv_status";                       // text: InProgress | Succeeded | Failed
    public const string RequestJson = "nv_requestjson";             // multiline text
    public const string ResponseJson = "nv_responsejson";           // multiline text
    public const string RetryCount = "nv_retrycount";               // whole number
}