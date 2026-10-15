using ITender.Integration.Api.Services;
using ITender.Integration.Api.TranferObjects;
using Microsoft.AspNetCore.Mvc;

namespace ITender.Integration.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Tags("Look Ups")]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupService _lookupService;

        public LookupsController(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        [HttpGet("Designations")]
        public async Task<IActionResult> GetTenderValueRanges(CancellationToken ct)
        {
            try
            {
                var result = await _lookupService.GetAllTenderValueRange(ct);

                if (result == null || !result.Any())
                    return NotFound("No tender value ranges found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                // ideally log ex here
                return StatusCode(500, "An error occurred while retrieving tender value ranges.");
            }
        }

        [HttpGet("TypesOfContracts")]
        public async Task<IActionResult> GetTypesOfContracts(CancellationToken ct)
        {
            try
            {
                var result = await _lookupService.GetAllTypeOfContracts(ct);

                if (result == null || !result.Any())
                    return NotFound("No tender value ranges found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                // ideally log ex here
                return StatusCode(500, "An error occurred while retrieving tender value ranges.");
            }
        }

        [HttpGet("Cities")]
        public async Task<IActionResult> GetAllCities([FromQuery] PagedRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetAllCitiesAsync(request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No cities found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                // log exception here
                return StatusCode(500, "An error occurred while retrieving cities.");
            }
        }

        [HttpGet("Cities/{id:guid}")]
        public async Task<IActionResult> GetCityById(Guid id, CancellationToken ct)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid city id.");

                var result = await _lookupService.GetCityByIdAsync(id, ct);

                if (result == null)
                    return NotFound($"City with id '{id}' was not found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                // log exception here
                return StatusCode(500, "An error occurred while retrieving the city.");
            }
        }

        [HttpGet("Cities/GetCitiesByProvinceId/{provinceId:guid}")]
        public async Task<IActionResult> GetCitiesByProvinceId(
            Guid provinceId,
            [FromQuery] PagedRequest request,
            CancellationToken ct)
        {
            try
            {
                if (provinceId == Guid.Empty)
                    return BadRequest("Invalid province id.");

                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetCitiesByProvinceIdAsync(provinceId, request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No cities found for the specified province.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                // log exception here
                return StatusCode(500, "An error occurred while retrieving cities by province.");
            }
        }

        [HttpGet("Provinces")]
        public async Task<IActionResult> GetAllProvinces([FromQuery] PagedRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetAllProvinces(request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No provinces found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                // log exception here
                return StatusCode(500, "An error occurred while retrieving provinces.");
            }
        }

        [HttpGet("Provinces/{provinceId:guid}")]
        public async Task<IActionResult> GetProvinceById(Guid provinceId, CancellationToken ct)
        {
            try
            {
                if (provinceId == Guid.Empty)
                    return BadRequest("Invalid province id.");

                var result = await _lookupService.GetProvinceByIdAsync(provinceId, ct);

                if (result == null)
                    return NotFound($"Province with id '{provinceId}' was not found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the province.");
            }
        }

        [HttpGet("ClassOfConstructionWorks")]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetAllClassOfConstructionWorksAsync(request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No class of construction works found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving class of construction works.");
            }
        }

        [HttpGet("ClassOfWorkTypeSubCategory")]
        public async Task<IActionResult> GetClassOfWorkTypeSubCategoryAll([FromQuery] PagedRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetAllClassOfWorkTypeSubCategoriesAsync(request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No class of work type sub categories found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving sub categories.");
            }
        }

        [HttpGet("ClassOfWorkTypeSubCategory/{classOfWorkTypeId:guid}")]
        public async Task<IActionResult> GetByWorkTypeId(
            Guid classOfWorkTypeId,
            [FromQuery] PagedRequest request,
            [FromQuery] string[]? columns,
            CancellationToken ct)
        {
            try
            {
                if (classOfWorkTypeId == Guid.Empty)
                    return BadRequest("Invalid class of work type id.");

                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetClassOfWorkTypeSubCategoriesByWorkTypeIdAsync(
                    classOfWorkTypeId,
                    request,
                    columns,
                    ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No sub categories found for the specified work type.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving sub categories by work type.");
            }
        }

        [HttpGet("ClassOfWorkTypeSubCategory/{id:guid}")]
        public async Task<IActionResult> GetClassOfWorkTypeSubCategoryById(Guid id, CancellationToken ct)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid id.");

                var result = await _lookupService.GetClassOfWorkTypeSubCategoryByIdAsync(id, ct);

                if (result == null)
                    return NotFound($"Sub category with id '{id}' was not found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the sub category.");
            }
        }

        [HttpGet("Metros")]
        public async Task<IActionResult> GetAllMetro([FromQuery] PagedRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetAllMetroDistrictsAsync(request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No metro districts found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving metro districts.");
            }
        }

        [HttpGet("Metros/{id:guid}")]
        public async Task<IActionResult> GetMetroById(Guid id, CancellationToken ct)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid metro district id.");

                var result = await _lookupService.GetMetroDistrictByIdAsync(id, ct);

                if (result == null)
                    return NotFound($"Metro district with id '{id}' was not found.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the metro district.");
            }
        }

        [HttpGet("GetMetroByProvinceId/{provinceId:guid}")]
        public async Task<IActionResult> GetByProvinceId(
            Guid provinceId,
            [FromQuery] PagedRequest request,
            CancellationToken ct)
        {
            try
            {
                if (provinceId == Guid.Empty)
                    return BadRequest("Invalid province id.");

                if (request == null || request.PageNumber <= 0 || request.PageSize <= 0)
                    return BadRequest("Invalid paging parameters.");

                var result = await _lookupService.GetMetroDistrictsByProvinceIdAsync(provinceId, request, ct);

                if (result == null || !result.Items.Any())
                    return NotFound("No metro districts found for the specified province.");

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Request was cancelled.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving metro districts by province.");
            }
        }
    }
}
