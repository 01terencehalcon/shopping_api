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
                Status = entity.Status,
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
        /// Get all userman (AdminAccess only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> GetAll()
        {
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get userman by ID (own record or admin)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<UsermanResponse>> GetById(int id)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && id != currentUserId)
                return NotFound(new { message = "Userman not found" });

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "Userman not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>
        /// Dynamic exact-match filters for userman (AdminAccess only)
        /// </summary>
        [HttpGet("search-by-filter-ematch")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchByFilterExact([FromQuery] SearchByFilterRequest request)
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
        /// Dynamic partial-match filters for userman (AdminAccess only)
        /// </summary>
        [HttpGet("search-by-filter-lmatch")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchByFilterLike([FromQuery] SearchByFilterRequest request)
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
        /// Search all userman (AdminAccess only)
        /// </summary>
        [HttpGet("search-all")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> SearchAll([FromQuery] string query)
        {
            var result = await _repository.SearchAsyncAll(query);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Get paginated userman (AdminAccess only)
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
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

        /// <summary>
        /// Create new userman (AdminAccess only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateUsermanRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest(new { message = "UserName is required" });
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Password is required" });
            if (request.UserRoleId != 1 && request.UserRoleId != 2 && request.UserRoleId != 3)
                return BadRequest(new { message = "Invalid role. Role must be 1 (Admin), 2 (Staff), or 3 (Customer)." });

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

        /// <summary>
        /// List pending staff accounts (AdminAccess only)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<UsermanResponse>>> GetPending()
        {
            var pending = await _repository.GetPendingStaffAsync();
            return Ok(pending.Select(ToResponse).ToList());
        }

        /// <summary>
        /// Approve pending staff account (AdminAccess only)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Userman not found" });
            }

            user.Status = "active";
            await _repository.UpdateAsync(user);
            return Ok(new { message = "Staff account approved successfully.", user = ToResponse(user) });
        }

        /// <summary>
        /// Reject pending staff account (AdminAccess only)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Userman not found" });
            }

            user.Status = "rejected";
            await _repository.UpdateAsync(user);
            return Ok(new { message = "Staff account rejected.", user = ToResponse(user) });
        }

        /// <summary>
        /// Update userman (AdminAccess only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUsermanRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest(new { message = "UserName is required" });

            if (request.UserRoleId != 1 && request.UserRoleId != 2 && request.UserRoleId != 3)
                return BadRequest(new { message = "Invalid role. Role must be 1 (Admin), 2 (Staff), or 3 (Customer)." });

            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "Userman not found" });

            // Rule: A user must never be able to change their own role
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (id == currentUserId && request.UserRoleId != existingEntity.UserRoleId)
            {
                return BadRequest(new { message = "You cannot change your own role." });
            }

            // Rule: Admins can't delete or demote the last remaining active admin
            if (existingEntity.UserRoleId == 1 && string.Equals(existingEntity.Status, "active", StringComparison.OrdinalIgnoreCase) && request.UserRoleId != 1)
            {
                int activeAdminCount = await _repository.CountActiveAdminsAsync();
                if (activeAdminCount <= 1)
                {
                    return BadRequest(new { message = "Cannot demote the last remaining active admin." });
                }
            }

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

        /// <summary>
        /// Delete userman (AdminAccess only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            // Get existing entity
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound(new { message = "Userman not found" });

            // Rule: Admins can't delete the last remaining active admin
            if (existingEntity.UserRoleId == 1 && string.Equals(existingEntity.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                int activeAdminCount = await _repository.CountActiveAdminsAsync();
                if (activeAdminCount <= 1)
                {
                    return BadRequest(new { message = "Cannot delete the last remaining active admin." });
                }
            }

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Userman deleted successfully" });
        }

        /// <summary>
        /// Delete all userman (AdminAccess only)
        /// </summary>
        [HttpDelete]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteAll()
        {
            int activeAdminCount = await _repository.CountActiveAdminsAsync();
            if (activeAdminCount > 0)
            {
                return BadRequest(new { message = "Cannot delete all users while active admin accounts exist." });
            }

            await _repository.DeleteAllAsync();
            return NoContent();
        }

        /// <summary>
        /// Bulk upload userman (AdminAccess only)
        /// </summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateUsermanRequest> dataList)
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
    }
}