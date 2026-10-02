using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.NotificationsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public NotificationController(INotificationRepository repository)
        {
            _repository = repository;
        }

        private static NotificationResponse ToResponse(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            UserId         = n.UserId,
            Message        = n.Message,
            IsRead         = n.IsRead == 1,
            CreatedAt      = n.CreatedAt,
        };

        /// <summary>Get notifications for a user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetByUser(int userId)
        {
            try
            {
                var result = await _repository.GetByUserIdAsync(userId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get notification by ID (UserAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<NotificationResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Notification not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get all notifications paginated (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
                return Ok(new PaginationModel<NotificationResponse>
                {
                    Items       = paged.Items.Select(ToResponse).ToList(),
                    TotalCount  = paged.TotalCount,
                    PageSize    = paged.PageSize,
                    CurrentPage = paged.CurrentPage,
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Mark notification as read (UserAccess)</summary>
        [HttpPut("{id}/read")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Notification not found" });

                await _repository.MarkAsReadAsync(id);
                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Send a new notification (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest(new { message = "Message is required." });

                var entity = new Notification
                {
                    UserId  = request.UserId,
                    Message = request.Message,
                    IsRead  = 0,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Notification sent", notificationId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete a notification (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Notification not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete all notifications for a user (AdminAccess)</summary>
        [HttpDelete("user/{userId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteByUser(int userId)
        {
            try
            {
                await _repository.DeleteByUserIdAsync(userId);
                return Ok(new { message = "All notifications deleted for user" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
