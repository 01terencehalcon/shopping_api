namespace ClothesShopping.Api.DTOs
{
    public class UsermanResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserRoleId { get; set; }
        public string Status { get; set; } = "active";
    }
}