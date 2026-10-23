using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.AddressModule;
using ClothesShopping.Api.DTOs;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "UserAccess")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressRepository _repository;

        public AddressController(IAddressRepository repository)
        {
            _repository = repository;
        }

        private static AddressResponse ToResponse(Address a) => new()
        {
            AddressId     = a.AddressId,
            UserId        = a.UserId,
            RecipientName = a.RecipientName,
            Phone         = a.Phone,
            Region        = a.Region,
            Province      = a.Province,
            City          = a.City,
            Barangay      = a.Barangay,
            Street        = a.Street,
            PostalCode    = a.PostalCode,
            Latitude      = a.Latitude,
            Longitude     = a.Longitude,
        };

        /// <summary>Get all addresses for a user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AddressResponse>>> GetByUser(int userId)
        {
            var result = await _repository.GetByUserIdAsync(userId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get address by ID (UserAccess)</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Address not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>Create a new address (UserAccess)</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressRequest request)
        {
            var entity = new Address
            {
                UserId        = request.UserId,
                RecipientName = request.RecipientName,
                Phone         = request.Phone,
                Region        = request.Region,
                Province      = request.Province,
                City          = request.City,
                Barangay      = request.Barangay,
                Street        = request.Street,
                PostalCode    = request.PostalCode,
                Latitude      = request.Latitude,
                Longitude     = request.Longitude,
            };

            var newId = await _repository.AddAsync(entity);
            return Ok(new { message = "Address created successfully", addressId = newId });
        }

        /// <summary>Update an address (UserAccess)</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Address not found" });

            existing.RecipientName = request.RecipientName ?? existing.RecipientName;
            existing.Phone         = request.Phone         ?? existing.Phone;
            existing.Region        = request.Region        ?? existing.Region;
            existing.Province      = request.Province      ?? existing.Province;
            existing.City          = request.City          ?? existing.City;
            existing.Barangay      = request.Barangay      ?? existing.Barangay;
            existing.Street        = request.Street        ?? existing.Street;
            existing.PostalCode    = request.PostalCode    ?? existing.PostalCode;
            if (request.Latitude.HasValue) existing.Latitude = request.Latitude;
            if (request.Longitude.HasValue) existing.Longitude = request.Longitude;

            await _repository.UpdateAsync(existing);
            return Ok(new { message = "Address updated successfully" });
        }

        /// <summary>Delete an address (UserAccess)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Address not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Address deleted successfully" });
        }

        /// <summary>Delete all addresses for a user (UserAccess)</summary>
        [HttpDelete("user/{userId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteByUser(int userId)
        {
            await _repository.DeleteByUserIdAsync(userId);
            return Ok(new { message = "All addresses deleted for user" });
        }
    }
}
