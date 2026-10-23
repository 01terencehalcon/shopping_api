using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IConfiguration? _configuration;

        private static string HashRawForSave(string rawPassword)
        {
            if (string.IsNullOrWhiteSpace(rawPassword))
            {
                throw new System.ArgumentException("Password is required.", nameof(rawPassword));
            }

            return PasswordHasher.Hash(rawPassword);
        }

        private bool ValidateInviteCode(string? providedCode)
        {
            if (string.IsNullOrWhiteSpace(providedCode))
                return false;

            var configuredCode = _configuration?["Staff:InviteCode"]
                ?? _configuration?["Staff__InviteCode"]
                ?? Environment.GetEnvironmentVariable("Staff__InviteCode")
                ?? Environment.GetEnvironmentVariable("Staff:InviteCode");

            if (string.IsNullOrWhiteSpace(configuredCode))
                return false;

            byte[] providedBytes = Encoding.UTF8.GetBytes(providedCode.Trim());
            byte[] configuredBytes = Encoding.UTF8.GetBytes(configuredCode.Trim());

            return CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
        }

        [ActivatorUtilitiesConstructor]
        public AuthControllers(IUsermanRepository userRepository, IJwtTokenService jwtTokenService, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _environment = environment;
            _configuration = configuration;
        }

        public AuthControllers(IUsermanRepository userRepository, IJwtTokenService jwtTokenService, IWebHostEnvironment environment)
            : this(userRepository, jwtTokenService, environment, null!)
        {
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var existingUser = await _userRepository.GetByUserNameAsync(dto.Username);
            if (existingUser != null)
            {
                return Conflict("Username already exists.");
            }

            // Always create customer role (3), ignoring any client input
            var user = new Userman
            {
                UserName = dto.Username,
                Email = dto.Email ?? string.Empty,
                UserPass = HashRawForSave(dto.Password),
                UserRoleId = 3,
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
            if (!isValid || (user.UserRoleId != 1 && user.UserRoleId != 2 && user.UserRoleId != 3) || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
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

        [HttpPost("staff-login")]
        public async Task<IActionResult> StaffLogin([FromBody] LoginRequest dto)
        {
            return await Login(dto);
        }

        [HttpPost("staff-register")]
        public async Task<IActionResult> StaffRegister([FromBody] StaffRegisterRequest dto)
        {
            if (dto == null)
            {
                return BadRequest("Request body is required.");
            }

            // Check inviteCode against configured Staff__InviteCode in constant time
            if (!ValidateInviteCode(dto.InviteCode))
            {
                return BadRequest("Invalid or expired staff invite code.");
            }

            // Validate username (3-50 chars)
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Trim().Length < 3 || dto.Username.Trim().Length > 50)
            {
                return BadRequest("Username must be between 3 and 50 characters.");
            }

            // Validate email format
            if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email.Trim()))
            {
                return BadRequest("A valid email address is required.");
            }

            // Validate password (at least 8 characters)
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters.");
            }

            // Reject duplicate usernames
            var existingUser = await _userRepository.GetByUserNameAsync(dto.Username.Trim());
            if (existingUser != null)
            {
                return Conflict("Username already exists.");
            }

            // Always create role 2 (Staff) and status "pending". Never create admins here.
            var user = new Userman
            {
                UserName = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                UserPass = HashRawForSave(dto.Password),
                UserRoleId = 2,
                Status = "pending",
            };

            await _userRepository.AddAsync(user);

            return Ok(new
            {
                message = "Request received. An admin must approve your account."
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