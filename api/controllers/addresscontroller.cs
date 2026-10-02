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
            Street        = a.Street,
            City          = a.City,
            Province      = a.Province,
            PostalCode    = a.PostalCode,
        };

        /// <summary>Get all addresses for a user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AddressResponse>>> GetByUser(int userId)
        {
            try
            {
                var result = await _repository.GetByUserIdAsync(userId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get address by ID (UserAccess)</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Address not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a new address (UserAccess)</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressRequest request)
        {
            try
            {
                var entity = new Address
                {
                    UserId        = request.UserId,
                    RecipientName = request.RecipientName,
                    Phone         = request.Phone,
                    Street        = request.Street,
                    City          = request.City,
                    Province      = request.Province,
                    PostalCode    = request.PostalCode,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Address created successfully", addressId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update an address (UserAccess)</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Address not found" });

                existing.RecipientName = request.RecipientName ?? existing.RecipientName;
                existing.Phone         = request.Phone         ?? existing.Phone;
                existing.Street        = request.Street        ?? existing.Street;
                existing.City          = request.City          ?? existing.City;
                existing.Province      = request.Province      ?? existing.Province;
                existing.PostalCode    = request.PostalCode    ?? existing.PostalCode;

                await _repository.UpdateAsync(existing);
                return Ok(new { message = "Address updated successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete an address (UserAccess)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Address not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Address deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete all addresses for a user (UserAccess)</summary>
        [HttpDelete("user/{userId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteByUser(int userId)
        {
            try
            {
                await _repository.DeleteByUserIdAsync(userId);
                return Ok(new { message = "All addresses deleted for user" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
