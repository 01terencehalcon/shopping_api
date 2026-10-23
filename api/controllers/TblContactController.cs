using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ClothesShopping.Api.TblContactModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // All endpoints require authentication
    public class TblContactController : ControllerBase
    {
        private readonly ITblContactRepository _repository;

        public TblContactController(ITblContactRepository repository)
        {
            _repository = repository;
        }

        private static TblContactResponse ToResponse(TblContact entity)
        {
            return new TblContactResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Gender = entity.Gender,
                Birthday = entity.Birthday,
                Address = entity.Address,
                Contactnum = entity.Contactnum,
            };
        }

        public sealed class SearchByFilterRequest
        {
            public int? id { get; set; }
        }

        private static Dictionary<string, object?> BuildFilterMap(SearchByFilterRequest request)
        {
            var filters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (request.id != null)
            {
                filters["id"] = request.id;
            }
            return filters;
        }

        /// <summary>
        /// Get all tblcontact (UserAccess only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<TblContactResponse>>> GetAll()
        {
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get tblcontact by ID (UserAccess only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<TblContactResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "TblContact not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>
        /// Dynamic exact-match filters for tblcontact (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-ematch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<TblContactResponse>>> SearchByFilterExact([FromQuery] SearchByFilterRequest request)
        {
            IReadOnlyDictionary<string, object?> filters = BuildFilterMap(request);
            if (filters.Count == 0)
            {
                return BadRequest(new { message = "At least one filter must be provided." });
            }

            var result = await _repository.GetFilteredExactAsync(filters);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Dynamic partial-match filters for tblcontact (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-lmatch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<TblContactResponse>>> SearchByFilterLike([FromQuery] SearchByFilterRequest request)
        {
            IReadOnlyDictionary<string, object?> filters = BuildFilterMap(request);
            if (filters.Count == 0)
            {
                return BadRequest(new { message = "At least one filter must be provided." });
            }

            var result = await _repository.GetFilteredLikeAsync(filters);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Search all tblcontact (UserAccess only)
        /// </summary>
        [HttpGet("search-all")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<TblContactResponse>>> SearchAll([FromQuery] string query)
        {
            var result = await _repository.SearchAsyncAll(query);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get paginated tblcontact (UserAccess only)
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            var pagedResult = await _repository.GetPaginatedAsync(pageNumber, pageSize);
            var response = new PaginationModel<TblContactResponse>
            {
                Items = pagedResult.Items.Select(ToResponse).ToList(),
                TotalCount = pagedResult.TotalCount,
                PageSize = pagedResult.PageSize,
                CurrentPage = pagedResult.CurrentPage
            };

            return Ok(response);
        }

        /// <summary>
        /// Create new tblcontact (UserAccess only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateTblContactRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name is required" });
            if (string.IsNullOrWhiteSpace(request.Gender))
                return BadRequest(new { message = "Gender is required" });
            if (string.IsNullOrWhiteSpace(request.Birthday))
                return BadRequest(new { message = "Birthday is required" });
            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest(new { message = "Address is required" });
            if (string.IsNullOrWhiteSpace(request.Contactnum))
                return BadRequest(new { message = "Contactnum is required" });

            var entity = new TblContact
            {
                Name = request.Name,
                Gender = request.Gender,
                Birthday = request.Birthday,
                Address = request.Address,
                Contactnum = request.Contactnum,
            };

            await _repository.AddAsync(entity);
            return Ok(new { message = "TblContact created successfully" });
        }

        /// <summary>
        /// Update tblcontact (UserAccess only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTblContactRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name is required" });
            if (string.IsNullOrWhiteSpace(request.Gender))
                return BadRequest(new { message = "Gender is required" });
            if (string.IsNullOrWhiteSpace(request.Birthday))
                return BadRequest(new { message = "Birthday is required" });
            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest(new { message = "Address is required" });
            if (string.IsNullOrWhiteSpace(request.Contactnum))
                return BadRequest(new { message = "Contactnum is required" });

            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "TblContact not found" });

            // Update properties
            existingEntity.Name = request.Name;
            existingEntity.Gender = request.Gender;
            existingEntity.Birthday = request.Birthday;
            existingEntity.Address = request.Address;
            existingEntity.Contactnum = request.Contactnum;

            await _repository.UpdateAsync(existingEntity);
            return Ok(new { message = "TblContact updated successfully" });
        }

        /// <summary>
        /// Delete tblcontact (UserAccess only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "TblContact not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "TblContact deleted successfully" });
        }

        /// <summary>
        /// Delete all tblcontact (UserAccess only)
        /// </summary>
        [HttpDelete]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> DeleteAll()
        {
            await _repository.DeleteAllAsync();
            return NoContent();
        }

        /// <summary>
        /// Bulk upload tblcontact (UserAccess only)
        /// </summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateTblContactRequest> dataList)
        {
            if (dataList == null || dataList.Count == 0)
                return BadRequest("Data list cannot be null or empty.");

            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Name)))
                return BadRequest(new { message = "One or more records have empty Name." });
            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Gender)))
                return BadRequest(new { message = "One or more records have empty Gender." });
            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Birthday)))
                return BadRequest(new { message = "One or more records have empty Birthday." });
            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Address)))
                return BadRequest(new { message = "One or more records have empty Address." });
            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Contactnum)))
                return BadRequest(new { message = "One or more records have empty Contactnum." });

            var entities = dataList.Select(item => new TblContact
            {
                Name = item.Name,
                Gender = item.Gender,
                Birthday = item.Birthday,
                Address = item.Address,
                Contactnum = item.Contactnum,
            }).ToList();

            await _repository.BulkUploadAsync(entities);
            return Ok(new { message = $"Successfully uploaded {entities.Count} tblcontact records." });
        }
    }
}