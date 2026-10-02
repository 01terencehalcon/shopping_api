using System.Net;
using ClothesShopping.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ClothesShopping.Api.Tests;

public sealed class ApiStartupTests
{
    [Fact]
    public async Task Health_ReturnsOk_InDevelopment()
    {
        await using var factory = new ApiWebApplicationFactory("Development");
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Production_FailsWithoutJwtKey()
    {
        using var factory = new ApiWebApplicationFactory("Production");

        Exception ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt:Key", ex.ToString());
    }

    [Fact]
    public void Production_FailsWithDevelopmentFallbackKey()
    {
        using var factory = new ApiWebApplicationFactory(
            "Production",
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtConfiguration.DevelopmentFallbackKey
            });

        Exception ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("fallback", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environmentName;
    private readonly IReadOnlyDictionary<string, string?> _configurationValues;

    public ApiWebApplicationFactory(
        string environmentName,
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        _environmentName = environmentName;
        _configurationValues = configurationValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(_configurationValues);
        });
    }
}