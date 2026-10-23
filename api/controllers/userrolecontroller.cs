using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ClothesShopping.Api.UserroleModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // All endpoints require authentication
    public class UserroleController : ControllerBase
    {
        private readonly IUserroleRepository _repository;

        public UserroleController(IUserroleRepository repository)
        {
            _repository = repository;
        }

        private static UserroleResponse ToResponse(Userrole entity)
        {
            return new UserroleResponse
            {
                UserRoleId = entity.UserRoleId,
                UserRole = entity.UserRole,
            };
        }

        public sealed class SearchByFilterRequest
        {
            public int? userRoleId { get; set; }
        }

        private static Dictionary<string, object?> BuildFilterMap(SearchByFilterRequest request)
        {
            var filters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (request.userRoleId != null)
            {
                filters["userRoleId"] = request.userRoleId;
            }
            return filters;
        }

        /// <summary>
        /// Get all userrole (UserAccess only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UserroleResponse>>> GetAll()
        {
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get userrole by ID (UserAccess only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<UserroleResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "Userrole not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>
        /// Dynamic exact-match filters for userrole (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-ematch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UserroleResponse>>> SearchByFilterExact([FromQuery] SearchByFilterRequest request)
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
        /// Dynamic partial-match filters for userrole (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-lmatch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UserroleResponse>>> SearchByFilterLike([FromQuery] SearchByFilterRequest request)
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
        /// Search all userrole (UserAccess only)
        /// </summary>
        [HttpGet("search-all")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UserroleResponse>>> SearchAll([FromQuery] string query)
        {
            var result = await _repository.SearchAsyncAll(query);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get paginated userrole (UserAccess only)
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            var pagedResult = await _repository.GetPaginatedAsync(pageNumber, pageSize);
            var response = new PaginationModel<UserroleResponse>
            {
                Items = pagedResult.Items.Select(ToResponse).ToList(),
                TotalCount = pagedResult.TotalCount,
                PageSize = pagedResult.PageSize,
                CurrentPage = pagedResult.CurrentPage
            };

            return Ok(response);
        }

        /// <summary>
        /// Create new userrole (UserAccess only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateUserroleRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.UserRole))
                return BadRequest(new { message = "UserRole is required" });

            var entity = new Userrole
            {
                UserRole = request.UserRole,
            };

            await _repository.AddAsync(entity);
            return Ok(new { message = "Userrole created successfully" });
        }

        /// <summary>
        /// Update userrole (UserAccess only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserroleRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.UserRole))
                return BadRequest(new { message = "UserRole is required" });

            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "Userrole not found" });

            // Update properties
            existingEntity.UserRole = request.UserRole;

            await _repository.UpdateAsync(existingEntity);
            return Ok(new { message = "Userrole updated successfully" });
        }

        /// <summary>
        /// Delete userrole (UserAccess only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "Userrole not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Userrole deleted successfully" });
        }

        /// <summary>
        /// Delete all userrole (UserAccess only)
        /// </summary>
        [HttpDelete]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> DeleteAll()
        {
            await _repository.DeleteAllAsync();
            return NoContent();
        }

        /// <summary>
        /// Bulk upload userrole (UserAccess only)
        /// </summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateUserroleRequest> dataList)
        {
            if (dataList == null || dataList.Count == 0)
                return BadRequest("Data list cannot be null or empty.");

            if (dataList.Any(item => string.IsNullOrWhiteSpace(item.UserRole)))
                return BadRequest(new { message = "One or more records have empty UserRole." });

            var entities = dataList.Select(item => new Userrole
            {
                UserRole = item.UserRole,
            }).ToList();

            await _repository.BulkUploadAsync(entities);
            return Ok(new { message = $"Successfully uploaded {entities.Count} userrole records." });
        }
    }
}