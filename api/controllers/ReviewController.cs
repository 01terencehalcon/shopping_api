using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ReviewsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _repository;

        public ReviewController(IReviewRepository repository)
        {
            _repository = repository;
        }

        private static ReviewResponse ToResponse(Review r) => new()
        {
            ReviewId   = r.ReviewId,
            UserId     = r.UserId,
            ProductId  = r.ProductId,
            Rating     = r.Rating,
            Comment    = r.Comment,
            ReviewDate = r.ReviewDate,
        };

        /// <summary>Get all reviews for a product (public)</summary>
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetByProduct(int productId)
        {
            var result = await _repository.GetByProductIdAsync(productId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get reviews by a specific user (own reviews or admin)</summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetByUser(int userId)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && userId != currentUserId)
                return NotFound(new { message = "Reviews not found" });

            var result = await _repository.GetByUserIdAsync(userId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get paginated reviews (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
            return Ok(new PaginationModel<ReviewResponse>
            {
                Items       = paged.Items.Select(ToResponse).ToList(),
                TotalCount  = paged.TotalCount,
                PageSize    = paged.PageSize,
                CurrentPage = paged.CurrentPage,
            });
        }

        /// <summary>Create a new review (UserAccess — userId from JWT)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            if (string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest(new { message = "Comment cannot be empty." });

            var currentUserId = ClaimsHelper.CurrentUserId(User);

            var entity = new Review
            {
                UserId    = currentUserId,
                ProductId = request.ProductId,
                Rating    = request.Rating,
                Comment   = request.Comment.Trim(),
            };

            var newId = await _repository.AddAsync(entity);
            return Ok(new { message = "Review created successfully", reviewId = newId });
        }

        /// <summary>Update a review (own review or admin)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            if (string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest(new { message = "Comment cannot be empty." });

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Review not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && existing.UserId != currentUserId)
                return NotFound(new { message = "Review not found" });

            existing.Rating  = request.Rating;
            existing.Comment = request.Comment.Trim();
            await _repository.UpdateAsync(existing);
            return Ok(new { message = "Review updated successfully" });
        }

        /// <summary>Delete a review (own review or admin)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Review not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && existing.UserId != currentUserId)
                return NotFound(new { message = "Review not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Review deleted successfully" });
        }
    }
}
