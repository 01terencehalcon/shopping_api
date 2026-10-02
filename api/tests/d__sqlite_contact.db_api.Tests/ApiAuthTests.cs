using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using ClothesShopping.Api.Controllers;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;
using ClothesShopping.Api.UsermanModule;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ClothesShopping.Api.Tests;

public sealed class ApiAuthTests
{
    [Fact]
    public void JwtTokenService_DevelopmentFallback_IsCompatibleWithStartupValidation()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var service = new JwtTokenService(configuration, new StubWebHostEnvironment("Development"));

        string token = service.GenerateToken(new Userman
        {
            UserId = 1,
            UserName = "demo",
            UserPass = "hash",
            UserRoleId = 1,
        });

        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal = handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = JwtConfiguration.DefaultIssuer,
                ValidAudience = JwtConfiguration.DefaultAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtConfiguration.DevelopmentFallbackKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            },
            out _);

        Assert.Equal("1", principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void UsernameLookup_UsesSingleEntityContract()
    {
        MethodInfo method = typeof(IUsermanRepository).GetMethod("GetByUserNameAsync")!;

        Assert.Equal(typeof(Task<Userman?>), method.ReturnType);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenUsernameAlreadyExists()
    {
        var existingUser = new Userman
        {
            UserId = 1,
            UserName = "demo",
            UserPass = "hash",
            UserRoleId = 1,
        };

        var controller = new AuthControllers(
            new StubAuthRepository(existingUser),
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var request = new RegisterRequest
        {
            Username = "demo",
            Password = "Password123!",
            RoleId = 1,
        };

        IActionResult result = await controller.Register(request);

        Assert.IsType<ConflictObjectResult>(result);
    }
}

internal sealed class StubAuthRepository : IUsermanRepository
{
    private readonly Userman? _existingUser;

    public StubAuthRepository(Userman? existingUser)
    {
        _existingUser = existingUser;
    }

    public Task<IEnumerable<Userman>> GetAllAsync() => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<Userman?> GetByIdAsync(int userid) => Task.FromResult(_existingUser);
    public Task DeleteAsync(int userid) => Task.CompletedTask;
    public Task AddAsync(Userman entity) => Task.CompletedTask;
    public Task UpdateAsync(Userman entity) => Task.CompletedTask;
    public Task<Userman?> GetByUserNameAsync(string username) => Task.FromResult(_existingUser);
    public Task<Userman?> GetByEmailAsync(string email) => Task.FromResult(_existingUser);
    public Task DeleteAllAsync() => Task.CompletedTask;
    public Task<IEnumerable<Userman>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> SearchAsync(string query) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> SearchAsyncAll(string query) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<PaginationModel<Userman>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Userman>());
    public Task BulkUploadAsync(List<Userman> dataList) => Task.CompletedTask;
}

internal sealed class StubJwtTokenService : IJwtTokenService
{
    public string GenerateToken(Userman user) => "token";
    public string GeneratePasswordResetToken(string userId) => "reset-token";
    public bool TryValidatePasswordResetToken(string token, out string userId)
    {
        userId = string.Empty;
        return false;
    }
}

internal sealed class StubWebHostEnvironment : IWebHostEnvironment
{
    public StubWebHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; } = "ClothesShopping.Api.Tests";
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}