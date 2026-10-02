using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ReviewsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

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
            try
            {
                var result = await _repository.GetByProductIdAsync(productId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get reviews by a specific user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetByUser(int userId)
        {
            try
            {
                var result = await _repository.GetByUserIdAsync(userId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated reviews (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a new review (UserAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            try
            {
                var entity = new Review
                {
                    UserId    = request.UserId,
                    ProductId = request.ProductId,
                    Rating    = request.Rating,
                    Comment   = request.Comment,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Review created successfully", reviewId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update a review (UserAccess)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Review not found" });

                existing.Rating  = request.Rating;
                existing.Comment = request.Comment;
                await _repository.UpdateAsync(existing);
                return Ok(new { message = "Review updated successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete a review (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Review not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
