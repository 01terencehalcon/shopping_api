using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.UsermanModule;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthControllers : ControllerBase
    {
        private readonly IUsermanRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IWebHostEnvironment _environment;

        private static string HashRawForSave(string rawPassword)
        {
            if (string.IsNullOrWhiteSpace(rawPassword))
            {
                throw new System.ArgumentException("Password is required.", nameof(rawPassword));
            }

            return PasswordHasher.Hash(rawPassword);
        }

        public AuthControllers(IUsermanRepository userRepository, IJwtTokenService jwtTokenService, IWebHostEnvironment environment)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Username and password are required.");
            }

            if (dto.RoleId < 1 || dto.RoleId > 3)
            {
                return BadRequest("Invalid RoleId. Valid roles are: 1 (admin), 2 (staff), 3 (user).");
            }

            var existingUser = await _userRepository.GetByUserNameAsync(dto.Username);
            if (existingUser != null)
            {
                return Conflict("Username already exists.");
            }

            var user = new Userman
            {
                UserName = dto.Username,
                Email = dto.Email ?? string.Empty,
                UserPass = HashRawForSave(dto.Password),
                UserRoleId = dto.RoleId,
            };

            await _userRepository.AddAsync(user);
            var createdUser = await _userRepository.GetByUserNameAsync(dto.Username);

            if (createdUser != null)
            {
                string token = _jwtTokenService.GenerateToken(createdUser);

                return Ok(new
                {
                    message = "User registered successfully.",
                    token,
                    userId = createdUser.UserId,
                    username = createdUser.UserName,
                    email = createdUser.Email,
                    roleId = createdUser.UserRoleId,
                });
            }

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _userRepository.GetByUserNameAsync(dto.Username);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            bool isValid = PasswordHasher.Verify(user.UserPass, dto.Password);
            if (!isValid)
            {
                return Unauthorized("Invalid username or password.");
            }

            string token = _jwtTokenService.GenerateToken(user);
            return Ok(new
            {
                message = "Login successful.",
                token,
                userId = user.UserId,
                username = user.UserName,
                email = user.Email,
                roleId = user.UserRoleId,
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest("Username is required.");
            }

            string message = "If the account exists, a reset token has been generated.";
            var user = await _userRepository.GetByUserNameAsync(dto.Username);
            if (user == null)
            {
                return Ok(new { message });
            }

            string resetToken = _jwtTokenService.GeneratePasswordResetToken(user.UserId.ToString());
            if (_environment.IsDevelopment())
            {
                return Ok(new { message, resetToken });
            }

            return Ok(new { message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest("Token and new password are required.");
            }

            if (!_jwtTokenService.TryValidatePasswordResetToken(dto.Token, out string userIdText))
            {
                return BadRequest("Invalid or expired reset token.");
            }

            if (!int.TryParse(userIdText, out var userId))
            {
                return BadRequest("Invalid reset token format.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid or expired reset token.");
            }

            user.UserPass = HashRawForSave(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            return Ok(new { message = "Password has been reset successfully." });
        }
    }
}