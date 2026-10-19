using FluentValidation;
using ITender.Integration.Api.Middleware;
using ITender.Integration.Api.Services;
using ITender.Integration.Api.TranferObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ITender.Integration.Api.Controllers;

[ApiController]
[Route("api/v1/tenders")]
[Authorize]
public sealed class TendersController(
    ITenderService service,
    IValidator<CreateTenderModel> createValidator,
    IValidator<CancelTenderRequest> cancelValidator,
    IValidator<AwardTenderRequest> awardValidator,
    ILogger<TendersController> log) : ControllerBase
{
    [HttpGet("cancellation-reasons")]
    [Authorize(Policy = "EditAndAdvertiseTender")] // Or allow any authenticated user
    public async Task<IActionResult> GetCancellationReasons(CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var reasons = await service.GetCancellationReasonsAsync(ct);

        return Ok(new
        {
            success = true,
            correlationId,
            count = reasons.Count,
            items = reasons
        });
    }

    /// <summary>Advertise a draft tender in CRM only. Idempotent.</summary>
    [HttpPost("{id:guid}/advertise-itender")]
    [Authorize(Policy = "EditAndAdvertiseTender")]
    public async Task<IActionResult> AdvertiseToITender(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        try
        {
            var result = await service.AdvertiseToITenderAsync(id, ct);
            if (result is null)
                return NotFound(new { success = false, correlationId, message = "Tender not found." });

            return Ok(new { success = true, correlationId, tender = result });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "AdvertiseToITender rejected. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }

    /// <summary>Publish an already-advertised tender to e-Tender.</summary>
    [HttpPost("{id:guid}/advertise")]
    [Authorize(Policy = "EditAndAdvertiseTender")]
    public async Task<IActionResult> Advertise(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        try
        {
            var result = await service.AdvertiseToETenderAsync(id, correlationId, ct);
            if (result is null)
                return NotFound(new { success = false, correlationId, message = "Tender not found." });
            return Ok(new { success = true, correlationId, result });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Advertise rejected. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "CreateAndEditTender")]
    public async Task<IActionResult> Create([FromBody] CreateTenderModel model, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await createValidator.ValidateAsync(model, ct);
        if (!validation.IsValid)
            return BadRequest(new
            {
                success = false,
                correlationId,
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });

        var created = await service.CreateAsync(model, ct);
        return StatusCode(201, new { success = true, correlationId, tender = created });
    }

    [HttpPost("create-and-publish")]
    [Authorize(Policy = "EditAndAdvertiseTender")]
    public async Task<IActionResult> CreateAndPublish([FromBody] CreateTenderModel model, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await createValidator.ValidateAsync(model, ct);
        if (!validation.IsValid)
            return BadRequest(new
            {
                success = false,
                correlationId,
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });

        try
        {
            var result = await service.CreateAndPublishAsync(model, correlationId, ct);
            return StatusCode(201, new { success = true, correlationId, result });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "CreateAndPublish failed. Correlation {CorrelationId}", correlationId);
            return StatusCode(502, new
            {
                success = false,
                correlationId,
                message = "Tender created but publishing to e-Tender failed.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "EditAndAdvertiseTender")]
    public async Task<IActionResult> Cancel(
    Guid id, [FromBody] CancelTenderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await cancelValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new
            {
                success = false,
                correlationId,
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });

        try
        {
            var result = await service.CancelAsync(id, request, correlationId, ct);
            if (result is null)
                return NotFound(new { success = false, correlationId, message = "Tender not found." });

            return Ok(new { success = true, correlationId, tender = result });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Cancel rejected. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CreateAndEditTender")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var tender = await service.GetByIdAsync(id, ct);

        if (tender is null)
            return NotFound(new { success = false, correlationId, message = "Tender not found." });

        return Ok(new { success = true, correlationId, tender });
    }

    [HttpGet]
    //[Authorize(Policy = "CreateAndEditTender")]
    public async Task<IActionResult> GetList([FromQuery] TenderFilterRequest filter, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var (items, total) = await service.GetPagedAsync(filter, ct);

        return Ok(new
        {
            success = true,
            correlationId,
            pageNumber = filter.PageNumber,
            pageSize = filter.PageSize,
            totalCount = total,
            items
        });
    }

    [HttpPost("{id:guid}/award")]
    [Authorize(Policy = "CanAwardTenders")]
    public async Task<IActionResult> Award(Guid id, [FromBody] AwardTenderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await awardValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                correlationId,
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        try
        {
            var contractId = await service.AwardAsync(id, request, correlationId, ct);
            return StatusCode(201, new { success = true, correlationId, contractId });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Award rejected. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }
}