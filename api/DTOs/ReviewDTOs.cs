using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateReviewRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        [MinLength(1, ErrorMessage = "Comment cannot be empty.")]
        public string Comment { get; set; } = string.Empty;
    }

    public class UpdateReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        [MinLength(1, ErrorMessage = "Comment cannot be empty.")]
        public string Comment { get; set; } = string.Empty;
    }

    public class ReviewResponse
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string ReviewDate { get; set; } = string.Empty;
    }
}
