namespace ClothesShopping.Api.DTOs
{
    public class TblContactResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Contactnum { get; set; } = string.Empty;
    }
}