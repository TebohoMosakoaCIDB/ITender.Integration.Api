using FluentValidation;
using ITender.Integration.Api.Middleware;
using ITender.Integration.Api.Services;
using ITender.Integration.Api.TranferObjects;
using ITender.Integration.Api.Validation;
using Microsoft.AspNetCore.Authorization;
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
    IValidator<PracticalCompletionRequest> completionValidator,
    IValidator<TerminateContractRequest> terminationValidator,
    IValidator<SubmitChangeRequest> changeRequestValidator,
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
    [HttpPost("{id:guid}/Publish-to-eTender")]
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
        if (!validation.IsValid) return BadRequest(new
        {
            success = false,
            correlationId,
            errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
        });

        try
        {
            var created = await service.CreateAsync(model, ct);
            return StatusCode(201, new { success = true, correlationId, tender = created });
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Duplicate tender creation attempted. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
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

    [HttpPost("/api/v1/contracts/{id:guid}/complete")]
    [Authorize(Policy = "CanPracticallyCompleteContracts")]
    public async Task<IActionResult> CompleteContract(Guid id, [FromBody] PracticalCompletionRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await completionValidator.ValidateAsync(request, ct);
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
            await service.CompleteContractAsync(id, request, correlationId, ct);
            return Ok(new { success = true, correlationId, message = "Contract marked as practically completed." });
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Complete contract failed. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }

    [HttpPost("/api/v1/contracts/{id:guid}/terminate")]
    [Authorize(Policy = "CanTerminateContracts")] // Using the policy we set up in Phase 1
    public async Task<IActionResult> TerminateContract(Guid id, [FromBody] TerminateContractRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await terminationValidator.ValidateAsync(request, ct);
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
            await service.TerminateContractAsync(id, request, correlationId, ct);
            return Ok(new { success = true, correlationId, message = "Contract successfully terminated." });
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Terminate contract failed. Correlation {CorrelationId}", correlationId);
            return BadRequest(new { success = false, correlationId, message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/change-requests")]
    [Authorize(Policy = "CanChangeTenderAndSubmitCR")]
    public async Task<IActionResult> SubmitChangeRequest(Guid id, [FromBody] SubmitChangeRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var validation = await changeRequestValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(new { success = false, errors = validation.Errors });

        // Get User ID from JWT
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        await service.SubmitChangeRequestAsync(id, request, userId, correlationId, ct);
        return Ok(new { success = true, correlationId, message = "Change request submitted and pending approval." });
    }

    [HttpPost("{id:guid}/change-requests/approve")]
    [Authorize(Policy = "CanChangeTenderAndSubmitCR")]
    public async Task<IActionResult> ApproveChangeRequest(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        await service.ApproveChangeRequestAsync(id, correlationId, ct);
        return Ok(new { success = true, correlationId, message = "Change request approved." });
    }
}