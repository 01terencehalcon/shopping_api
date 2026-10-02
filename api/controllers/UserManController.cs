using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ClothesShopping.Api.UsermanModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // All endpoints require authentication
    public class UsermanController : ControllerBase
    {
        private readonly IUsermanRepository _repository;

        public UsermanController(IUsermanRepository repository)
        {
            _repository = repository;
        }

        private static UsermanResponse ToResponse(Userman entity)
        {
            return new UsermanResponse
            {
                UserId = entity.UserId,
                UserName = entity.UserName,
                Email = entity.Email,
                UserRoleId = entity.UserRoleId,
            };
        }

        public sealed class SearchByFilterRequest
        {
            public int? userId { get; set; }
        }

        private static Dictionary<string, object?> BuildFilterMap(SearchByFilterRequest request)
        {
            var filters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (request.userId != null)
            {
                filters["userId"] = request.userId;
            }
            return filters;
        }

        /// <summary>
        /// Get all userman (UserAccess only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> GetAll()
        {
            try
            {
                var result = await _repository.GetAllAsync();
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get userman by ID (UserAccess only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<UsermanResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(new { message = "Userman not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Dynamic exact-match filters for userman (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-ematch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchByFilterExact([FromQuery] SearchByFilterRequest request)
        {
            try
            {
                IReadOnlyDictionary<string, object?> filters = BuildFilterMap(request);
                if (filters.Count == 0)
                {
                    return BadRequest(new { message = "At least one filter must be provided." });
                }

                var result = await _repository.GetFilteredExactAsync(filters);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Dynamic partial-match filters for userman (UserAccess only)
        /// </summary>
        [HttpGet("search-by-filter-lmatch")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchByFilterLike([FromQuery] SearchByFilterRequest request)
        {
            try
            {
                IReadOnlyDictionary<string, object?> filters = BuildFilterMap(request);
                if (filters.Count == 0)
                {
                    return BadRequest(new { message = "At least one filter must be provided." });
                }

                var result = await _repository.GetFilteredLikeAsync(filters);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Search all userman (UserAccess only)
        /// </summary>
        [HttpGet("search-all")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchAll([FromQuery] string query)
        {
            try
            {
                var result = await _repository.SearchAsyncAll(query);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get paginated userman (UserAccess only)
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var pagedResult = await _repository.GetPaginatedAsync(pageNumber, pageSize);
                var response = new PaginationModel<UsermanResponse>
                {
                    Items = pagedResult.Items.Select(ToResponse).ToList(),
                    TotalCount = pagedResult.TotalCount,
                    PageSize = pagedResult.PageSize,
                    CurrentPage = pagedResult.CurrentPage
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create new userman (UserAccess only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateUsermanRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return BadRequest(new { message = "UserName is required" });
                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new { message = "Password is required" });

                var entity = new Userman
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    UserPass = PasswordHasher.Hash(request.Password),
                    UserRoleId = request.UserRoleId,
                };

                await _repository.AddAsync(entity);
                return Ok(new { message = "Userman created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update userman (UserAccess only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUsermanRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return BadRequest(new { message = "UserName is required" });

                // Get existing entity
                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return NotFound(new { message = "Userman not found" });

                // Update properties
                existingEntity.UserName = request.UserName;
                if (request.Email != null)
                {
                    existingEntity.Email = request.Email;
                }
                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    existingEntity.UserPass = PasswordHasher.Hash(request.NewPassword);
                }
                existingEntity.UserRoleId = request.UserRoleId;

                await _repository.UpdateAsync(existingEntity);
                return Ok(new { message = "Userman updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete userman (UserAccess only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Get existing entity
                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return NotFound(new { message = "Userman not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Userman deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete all userman (UserAccess only)
        /// </summary>
        [HttpDelete]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _repository.DeleteAllAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bulk upload userman (UserAccess only)
        /// </summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateUsermanRequest> dataList)
        {
            try
            {
                if (dataList == null || dataList.Count == 0)
                    return BadRequest("Data list cannot be null or empty.");

                if (dataList.Any(item => string.IsNullOrWhiteSpace(item.UserName)))
                    return BadRequest(new { message = "One or more records have empty UserName." });
                if (dataList.Any(item => string.IsNullOrWhiteSpace(item.Password)))
                    return BadRequest(new { message = "One or more records have empty Password." });

                var entities = dataList.Select(item => new Userman
                {
                    UserName = item.UserName,
                    Email = item.Email,
                    UserPass = PasswordHasher.Hash(item.Password),
                    UserRoleId = item.UserRoleId,
                }).ToList();

                await _repository.BulkUploadAsync(entities);
                return Ok(new { message = $"Successfully uploaded {entities.Count} userman records." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}