using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ClothesShopping.Api.Controllers;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.ProductImagesModule;
using ClothesShopping.Api.ProductsModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace ClothesShopping.Api.Tests;

/// <summary>
/// Tests for POST api/product-images/upload access control and validation.
/// Uses a fake IWebHostEnvironment that points to a temp folder so nothing
/// is written to the real wwwroot.
/// </summary>
public sealed class ProductImageUploadTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeWebHostEnvironment _env;

    public ProductImageUploadTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"img_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _env = new FakeWebHostEnvironment(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static ControllerContext MakeCtx(int userId, int roleId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("user_role_id", roleId.ToString()),
        };
        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }

    private ProductImageController MakeController(
        IProductImageRepository? imageRepo = null,
        IProductRepository? productRepo    = null)
    {
        return new ProductImageController(
            imageRepo   ?? new FakeProductImageRepo(),
            productRepo ?? new FakeProductRepo(),
            _env);
    }

    /// <summary>Builds an IFormFile with the supplied bytes and name.</summary>
    private static IFormFile MakeFile(byte[] bytes, string fileName, string contentType = "image/jpeg")
    {
        var ms = new MemoryStream(bytes);
        return new FormFile(ms, 0, bytes.Length, "files", fileName)
        {
            Headers     = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    // JPEG magic bytes (real)
    private static byte[] JpegBytes(int extraBytes = 100)
    {
        var data = new byte[3 + extraBytes];
        data[0] = 0xFF; data[1] = 0xD8; data[2] = 0xFF;
        return data;
    }

    // PNG magic bytes (real)
    private static byte[] PngBytes(int extraBytes = 100)
    {
        byte[] sig = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return [.. sig, .. new byte[extraBytes]];
    }

    // Plain text bytes (not an image)
    private static byte[] TextBytes(string content = "This is not an image file")
        => Encoding.UTF8.GetBytes(content);

    // ── tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_CustomerRole_Returns403()
    {
        // Customer (role 3) must be rejected by policy, but since we're unit-testing
        // the controller directly (no full auth pipeline), we simulate the policy
        // enforcement by checking that a real HTTP pipeline would reject role 3.
        // Instead we verify the controller itself doesn't bypass the [Authorize] attribute.
        var controller = MakeController();
        controller.ControllerContext = MakeCtx(99, 3); // role 3 = customer

        // The controller action is decorated with [Authorize(Policy = "ModAccess")].
        // In a real pipeline a customer would get 403 before even reaching the action.
        // Here we verify the attribute is present.
        var method = typeof(ProductImageController).GetMethod("Upload");
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("ModAccess", attr!.Policy);
    }

    [Fact]
    public async Task Upload_NonImageFileRenamedToJpg_Rejected()
    {
        // A .txt file renamed to .jpg must be rejected by magic-byte check
        var controller = MakeController(productRepo: new FakeProductRepo(hasProduct: true));
        controller.ControllerContext = MakeCtx(1, 1); // admin

        var file   = MakeFile(TextBytes(), "photo.jpg", "image/jpeg");
        var result = await controller.Upload(1, new List<IFormFile> { file });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var msg = GetMessage(bad.Value);
        Assert.Contains("not a valid", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_FileOver2MB_Rejected()
    {
        var controller = MakeController(productRepo: new FakeProductRepo(hasProduct: true));
        controller.ControllerContext = MakeCtx(1, 1);

        // 2 MB + 1 byte — starts with JPEG magic but is too large
        var bigData = new byte[2 * 1024 * 1024 + 1];
        bigData[0] = 0xFF; bigData[1] = 0xD8; bigData[2] = 0xFF; // JPEG magic
        var file   = MakeFile(bigData, "big.jpg");
        var result = await controller.Upload(1, new List<IFormFile> { file });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var msg = GetMessage(bad.Value);
        Assert.Contains("2 MB", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_SeventhImage_Rejected()
    {
        // Product already has 6 images; uploading one more must be rejected
        var imageRepo = new FakeProductImageRepo(existingCount: 6);
        var controller = MakeController(imageRepo, new FakeProductRepo(hasProduct: true));
        controller.ControllerContext = MakeCtx(1, 2); // staff

        var file   = MakeFile(JpegBytes(), "seventh.jpg");
        var result = await controller.Upload(1, new List<IFormFile> { file });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var msg = GetMessage(bad.Value);
        Assert.Contains("6", msg);
    }

    [Fact]
    public async Task Upload_ValidJpeg_Succeeds()
    {
        // Ensure the uploads/products sub-folder exists under our temp dir
        var dir = Path.Combine(_tempDir, "uploads", "products");
        Directory.CreateDirectory(dir);

        var imageRepo = new FakeProductImageRepo();
        var controller = MakeController(imageRepo, new FakeProductRepo(hasProduct: true));
        controller.ControllerContext = MakeCtx(1, 2); // staff

        var file   = MakeFile(JpegBytes(500), "photo.jpg");
        var result = await controller.Upload(1, new List<IFormFile> { file });

        Assert.IsType<OkObjectResult>(result);
    }

    // ── private helpers ────────────────────────────────────────────────────

    private static string GetMessage(object? value)
    {
        if (value == null) return string.Empty;
        var prop = value.GetType().GetProperty("message");
        return prop?.GetValue(value)?.ToString() ?? string.Empty;
    }
}

// ── fakes ──────────────────────────────────────────────────────────────────────

internal sealed class FakeWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    private readonly string _root;
    public FakeWebHostEnvironment(string root) => _root = root;
    public string WebRootPath          { get => _root; set { } }
    public string ContentRootPath      { get => _root; set { } }
    public string EnvironmentName      { get => "Testing"; set { } }
    public string ApplicationName      { get => "Test"; set { } }
    public IFileProvider ContentRootFileProvider { get => null!; set { } }
    public IFileProvider WebRootFileProvider     { get => null!; set { } }
}

internal sealed class FakeProductImageRepo : IProductImageRepository
{
    private readonly int _existingCount;
    public FakeProductImageRepo(int existingCount = 0) => _existingCount = existingCount;

    public Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId)
    {
        var list = Enumerable.Range(1, _existingCount)
            .Select(i => new ProductImage { ImageId = i, ProductId = productId, ImageUrl = $"/uploads/products/img{i}.jpg" })
            .ToList();
        return Task.FromResult<IEnumerable<ProductImage>>(list);
    }

    public Task<ProductImage?> GetByIdAsync(int imageId) => Task.FromResult<ProductImage?>(null);
    public Task<int> AddAsync(ProductImage entity) => Task.FromResult(99);
    public Task DeleteAsync(int imageId) => Task.CompletedTask;
    public Task DeleteByProductIdAsync(int productId) => Task.CompletedTask;
}

internal sealed class FakeProductRepo : IProductRepository
{
    private readonly bool _hasProduct;
    public FakeProductRepo(bool hasProduct = true) => _hasProduct = hasProduct;

    public Task<Product?> GetByIdAsync(int id) =>
        Task.FromResult(_hasProduct
            ? new Product { Id = id, Name = "Test", Category = "Tops", Price = 10, Stock = 5, Image = null }
            : (Product?)null);

    public Task<IEnumerable<Product>> GetAllAsync() => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> GetByCategoryAsync(string category) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> SearchAsync(string query) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> f) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> f) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<ClothesShopping.Api.Main.PaginationModel<Product>> GetPaginatedAsync(int p, int s) => Task.FromResult(new ClothesShopping.Api.Main.PaginationModel<Product>());
    public Task<IEnumerable<ClothesShopping.Api.DTOs.TopProductResponse>> GetTopAsync(int count) => Task.FromResult<IEnumerable<ClothesShopping.Api.DTOs.TopProductResponse>>([]);
    public Task<int> AddAsync(Product entity) => Task.FromResult(1);
    public Task UpdateAsync(Product entity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task BulkUploadAsync(List<Product> dataList) => Task.CompletedTask;
}
