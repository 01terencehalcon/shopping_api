using System.Security.Claims;
using System.Text.Json;
using ClothesShopping.Api.Controllers;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.OrdersModule;
using ClothesShopping.Api.OrderItemsModule;
using ClothesShopping.Api.CartModule;
using ClothesShopping.Api.CartItemsModule;
using ClothesShopping.Api.CouponsModule;
using ClothesShopping.Api.PaymentsModule;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.Security;
using ClothesShopping.Api.UsermanModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ClothesShopping.Api.Tests;

public sealed class SecurityAndAuthorizationTests
{
    private static ControllerContext CreateContext(int userId, int roleId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("user_role_id", roleId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // =========================================================================
    // 1. REGISTER ROLE ASSIGNMENT
    // =========================================================================

    [Fact]
    public async Task Register_WithoutExplicitRole_DefaultsToRole3()
    {
        Userman? savedUser = null;
        var stubRepo = new TestUsermanRepository
        {
            OnAddAsync = u =>
            {
                savedUser = u;
                return Task.CompletedTask;
            },
            OnGetByUserNameAsync = _ => Task.FromResult(savedUser)
        };

        var controller = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        // Register request with valid fields but no roleId
        var request = new RegisterRequest
        {
            Username = "customer_user",
            Email = "customer@example.com",
            Password = "Password123!",
        };

        var result = await controller.Register(request);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedUser);
        Assert.Equal(3, savedUser.UserRoleId); // Role defaults to 3 (regular customer)
    }

    [Fact]
    public async Task Register_AlwaysAssignsCustomerRole()
    {
        Userman? savedUser = null;
        var stubRepo = new TestUsermanRepository
        {
            OnAddAsync = u =>
            {
                savedUser = u;
                return Task.CompletedTask;
            },
            OnGetByUserNameAsync = _ => Task.FromResult(savedUser)
        };

        var controller = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var request = new RegisterRequest
        {
            Username = "user_role_test",
            Email = "test@example.com",
            Password = "Password123!"
        };

        var result = await controller.Register(request);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedUser);
        Assert.Equal(3, savedUser.UserRoleId);
    }

    [Fact]
    public void RegisterRequestJsonConverter_IgnoresRoleIdInPayload()
    {
        const string jsonWithRole = "{\"username\":\"alice\",\"email\":\"alice@test.com\",\"password\":\"Secret123!\",\"roleId\":2}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new RegisterRequestJsonConverter() }
        };

        var request = JsonSerializer.Deserialize<RegisterRequest>(jsonWithRole, options);

        Assert.NotNull(request);
        Assert.Equal("alice", request.Username);
        Assert.Equal("alice@test.com", request.Email);
        Assert.Equal("Secret123!", request.Password);
    }

    [Fact]
    public async Task StaffRegister_InvalidInviteCode_Fails()
    {
        var stubRepo = new TestUsermanRepository();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Staff:InviteCode", "SECRET_KEY" } })
            .Build();

        var controller = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"),
            config);

        var request = new StaffRegisterRequest
        {
            Username = "staff_candidate",
            Email = "staff@example.com",
            Password = "Password123!",
            InviteCode = "WRONG_CODE"
        };

        var result = await controller.StaffRegister(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StaffRegister_ValidCode_CreatesRole2AndPendingStatus_NeverRole1()
    {
        Userman? createdUser = null;
        var stubRepo = new TestUsermanRepository
        {
            OnAddAsync = u =>
            {
                createdUser = u;
                return Task.CompletedTask;
            }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Staff:InviteCode", "SECRET_KEY" } })
            .Build();

        var controller = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"),
            config);

        var request = new StaffRegisterRequest
        {
            Username = "staff_candidate",
            Email = "staff@example.com",
            Password = "Password123!",
            InviteCode = "SECRET_KEY"
        };

        var result = await controller.StaffRegister(request);
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(createdUser);
        Assert.Equal(2, createdUser.UserRoleId); // Always role 2 (Staff), never 1 (Admin)
        Assert.Equal("pending", createdUser.Status); // Starts as pending
    }

    [Fact]
    public async Task PendingAccount_CannotLogin_ReturnsUnauthorized()
    {
        var pendingStaff = new Userman
        {
            UserId = 10,
            UserName = "pending_staff",
            Email = "staff@example.com",
            UserPass = PasswordHasher.Hash("Password123!"),
            UserRoleId = 2,
            Status = "pending"
        };

        var stubRepo = new TestUsermanRepository
        {
            OnGetByUserNameAsync = _ => Task.FromResult<Userman?>(pendingStaff)
        };

        var controller = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var loginRequest = new LoginRequest
        {
            Username = "pending_staff",
            Password = "Password123!"
        };

        var result = await controller.StaffLogin(loginRequest);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void CustomerOrStaff_CallingApprove_HasAdminAccessPolicy()
    {
        var approveMethod = typeof(UsermanController).GetMethod("Approve");
        Assert.NotNull(approveMethod);

        var authorizeAttr = (AuthorizeAttribute?)Attribute.GetCustomAttribute(approveMethod, typeof(AuthorizeAttribute));
        Assert.NotNull(authorizeAttr);
        Assert.Equal("AdminAccess", authorizeAttr.Policy);
    }

    [Fact]
    public async Task AdminApproving_MakesLoginWork()
    {
        var user = new Userman
        {
            UserId = 20,
            UserName = "approved_staff",
            Email = "staff@example.com",
            UserPass = PasswordHasher.Hash("Password123!"),
            UserRoleId = 2,
            Status = "pending"
        };

        var stubRepo = new TestUsermanRepository
        {
            OnGetByIdAsync = _ => Task.FromResult<Userman?>(user),
            OnGetByUserNameAsync = _ => Task.FromResult<Userman?>(user),
            OnUpdateAsync = u =>
            {
                user = u;
                return Task.CompletedTask;
            }
        };

        var userManController = new UsermanController(stubRepo)
        {
            ControllerContext = CreateContext(userId: 1, roleId: 1) // Admin
        };

        // Admin approves
        var approveResult = await userManController.Approve(user.UserId);
        Assert.IsType<OkObjectResult>(approveResult);
        Assert.Equal("active", user.Status);

        // Login now works
        var authController = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var loginResult = await authController.StaffLogin(new LoginRequest
        {
            Username = "approved_staff",
            Password = "Password123!"
        });

        Assert.IsType<OkObjectResult>(loginResult);
    }

    [Theory]
    [InlineData(1)] // Admin
    [InlineData(2)] // Staff
    [InlineData(3)] // Customer
    public async Task Login_AllowsCustomerStaffAndAdmin_WhenActive(int roleId)
    {
        var user = new Userman
        {
            UserId = 10,
            UserName = $"user_role_{roleId}",
            Email = $"role{roleId}@example.com",
            UserPass = PasswordHasher.Hash("Password123!"),
            UserRoleId = roleId,
            Status = "active"
        };

        var stubRepo = new TestUsermanRepository
        {
            OnGetByUserNameAsync = _ => Task.FromResult<Userman?>(user)
        };

        var authController = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var result = await authController.Login(new LoginRequest
        {
            Username = $"user_role_{roleId}",
            Password = "Password123!"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    public async Task Login_RejectsNonActiveStatus_WithGenericUnauthorized(string status)
    {
        var user = new Userman
        {
            UserId = 11,
            UserName = "inactive_user",
            Email = "inactive@example.com",
            UserPass = PasswordHasher.Hash("Password123!"),
            UserRoleId = 2,
            Status = status
        };

        var stubRepo = new TestUsermanRepository
        {
            OnGetByUserNameAsync = _ => Task.FromResult<Userman?>(user)
        };

        var authController = new AuthControllers(
            stubRepo,
            new StubJwtTokenService(),
            new StubWebHostEnvironment("Development"));

        var result = await authController.Login(new LoginRequest
        {
            Username = "inactive_user",
            Password = "Password123!"
        });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid username or password.", unauthorizedResult.Value);
    }

    // =========================================================================
    // 2. USER A CANNOT READ USER B'S ORDER / CART / PAYMENT
    // =========================================================================

    [Fact]
    public async Task UserA_CannotGet_UserB_OrderById_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int orderId = 50;

        var orderRepo = new TestOrderRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Order?>(new Order
            {
                Id = id,
                UserId = userBId, // Belongs to User B
                Total = 150.0,
                Status = "pending"
            })
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3) // User A
        };

        var result = await controller.GetById(orderId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotGet_UserB_OrdersByUser_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;

        var controller = new OrderController(
            new TestOrderRepository(),
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3) // User A
        };

        var result = await controller.GetByUser(userBId); // Requesting User B's orders

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotGet_UserB_OrderItems_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int orderId = 50;

        var orderRepo = new TestOrderRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Order?>(new Order
            {
                Id = id,
                UserId = userBId,
                Total = 150.0,
                Status = "pending"
            })
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3) // User A
        };

        var result = await controller.GetOrderItems(orderId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Admin_CanGet_AnyUser_OrderById()
    {
        const int adminId = 999;
        const int userBId = 102;
        const int orderId = 50;

        var orderRepo = new TestOrderRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Order?>(new Order
            {
                Id = id,
                UserId = userBId,
                Total = 150.0,
                Status = "pending"
            })
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: adminId, roleId: 1) // Admin
        };

        var result = await controller.GetById(orderId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var orderResponse = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal(orderId, orderResponse.Id);
    }

    [Fact]
    public async Task UserA_CannotGet_UserB_Cart_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;

        var controller = new CartController(
            new TestCartRepository(),
            new TestCartItemRepository())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3)
        };

        var result = await controller.GetCartByUser(userBId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotClear_UserB_Cart_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;

        var controller = new CartController(
            new TestCartRepository(),
            new TestCartItemRepository())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3)
        };

        var result = await controller.ClearCart(userBId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotUpdate_UserB_CartItem_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int cartId = 30;
        const int cartItemId = 301;

        var cartItemRepo = new TestCartItemRepository
        {
            OnGetByIdAsync = id => Task.FromResult<CartItem?>(new CartItem
            {
                CartItemId = id,
                CartId = cartId,
                ProductId = 1,
                Quantity = 2
            })
        };

        var cartRepo = new TestCartRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Cart?>(new Cart
            {
                CartId = id,
                UserId = userBId // Cart belongs to User B
            })
        };

        var controller = new CartController(cartRepo, cartItemRepo)
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3)
        };

        var result = await controller.UpdateItemQuantity(cartItemId, new UpdateCartItemRequest { Quantity = 5 });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotGet_UserB_PaymentById_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int orderId = 88;
        const int paymentId = 400;

        var paymentRepo = new TestPaymentRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Payment?>(new Payment
            {
                PaymentId = id,
                OrderId = orderId,
                Amount = 99.0,
                PaymentMethod = "Credit Card",
                PaymentStatus = "pending"
            })
        };

        var orderRepo = new TestOrderRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Order?>(new Order
            {
                Id = id,
                UserId = userBId // Order belongs to User B
            })
        };

        var controller = new PaymentController(paymentRepo, orderRepo)
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3)
        };

        var result = await controller.GetById(paymentId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UserA_CannotGet_UserB_PaymentByOrder_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int orderId = 88;

        var orderRepo = new TestOrderRepository
        {
            OnGetByIdAsync = id => Task.FromResult<Order?>(new Order
            {
                Id = id,
                UserId = userBId // Order belongs to User B
            })
        };

        var controller = new PaymentController(new TestPaymentRepository(), orderRepo)
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3)
        };

        var result = await controller.GetByOrder(orderId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    // =========================================================================
    // 3. ORDER TOTAL COMPUTED ON SERVER
    // =========================================================================

    [Fact]
    public async Task CreateOrder_ComputesTotalOnServer_WithoutCoupon()
    {
        const int userId = 101;
        const int cartId = 10;

        var cartRepo = new TestCartRepository
        {
            OnGetByUserIdAsync = id => Task.FromResult<Cart?>(new Cart { CartId = cartId, UserId = id })
        };

        var cartItemRepo = new TestCartItemRepository
        {
            OnGetByCartIdAsync = cid => Task.FromResult<IEnumerable<CartItem>>(new List<CartItem>
            {
                new() { CartItemId = 1, CartId = cid, ProductId = 1, Quantity = 2 },
                new() { CartItemId = 2, CartId = cid, ProductId = 2, Quantity = 3 },
            })
        };

        var productRepo = new TestProductRepository
        {
            OnGetByIdAsync = pid => Task.FromResult<Product?>(pid switch
            {
                1 => new Product { Id = 1, Name = "Item 1", Price = 20.0, Stock = 10 },
                2 => new Product { Id = 2, Name = "Item 2", Price = 15.0, Stock = 10 },
                _ => null
            })
        };

        double? capturedTotal = null;
        var orderRepo = new TestOrderRepository
        {
            OnCreateOrderTransactionalAsync = (uid, total, items, cid) =>
            {
                capturedTotal = total;
                return Task.FromResult(1234);
            }
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            productRepo,
            cartRepo,
            cartItemRepo,
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: userId, roleId: 3)
        };

        // Client passes empty/no price data
        var request = new CreateOrderRequest();
        var result = await controller.Create(request);

        Assert.IsType<OkObjectResult>(result);
        // Server computed total: 2 * 20.0 + 3 * 15.0 = 40.0 + 45.0 = 85.0
        Assert.NotNull(capturedTotal);
        Assert.Equal(85.0, capturedTotal.Value);
    }

    [Fact]
    public async Task CreateOrder_ComputesTotalOnServer_WithValidCouponDiscount()
    {
        const int userId = 101;
        const int cartId = 10;

        var cartRepo = new TestCartRepository
        {
            OnGetByUserIdAsync = id => Task.FromResult<Cart?>(new Cart { CartId = cartId, UserId = id })
        };

        var cartItemRepo = new TestCartItemRepository
        {
            OnGetByCartIdAsync = cid => Task.FromResult<IEnumerable<CartItem>>(new List<CartItem>
            {
                new() { CartItemId = 1, CartId = cid, ProductId = 1, Quantity = 2 },
            })
        };

        var productRepo = new TestProductRepository
        {
            OnGetByIdAsync = pid => Task.FromResult<Product?>(new Product
            {
                Id = pid,
                Name = "Item 1",
                Price = 50.0,
                Stock = 5
            })
        };

        var couponRepo = new TestCouponRepository
        {
            OnGetByCodeAsync = code => Task.FromResult<Coupon?>(new Coupon
            {
                CouponId = 1,
                Code = code,
                Discount = 20.0, // 20% discount
                Status = "active",
                ExpirationDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
            })
        };

        double? capturedTotal = null;
        var orderRepo = new TestOrderRepository
        {
            OnCreateOrderTransactionalAsync = (uid, total, items, cid) =>
            {
                capturedTotal = total;
                return Task.FromResult(5678);
            }
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            productRepo,
            cartRepo,
            cartItemRepo,
            couponRepo)
        {
            ControllerContext = CreateContext(userId: userId, roleId: 3)
        };

        var request = new CreateOrderRequest { CouponCode = "SAVE20" };
        var result = await controller.Create(request);

        Assert.IsType<OkObjectResult>(result);
        // Total before coupon: 2 * 50 = 100. With 20% off: 80.0
        Assert.NotNull(capturedTotal);
        Assert.Equal(80.0, capturedTotal.Value);
    }

    [Fact]
    public async Task CreateOrder_RejectsWhenStockIsInsufficient()
    {
        const int userId = 101;
        const int cartId = 10;

        var cartRepo = new TestCartRepository
        {
            OnGetByUserIdAsync = id => Task.FromResult<Cart?>(new Cart { CartId = cartId, UserId = id })
        };

        var cartItemRepo = new TestCartItemRepository
        {
            OnGetByCartIdAsync = cid => Task.FromResult<IEnumerable<CartItem>>(new List<CartItem>
            {
                new() { CartItemId = 1, CartId = cid, ProductId = 1, Quantity = 10 }, // asks for 10
            })
        };

        var productRepo = new TestProductRepository
        {
            OnGetByIdAsync = pid => Task.FromResult<Product?>(new Product
            {
                Id = pid,
                Name = "Limited Item",
                Price = 50.0,
                Stock = 3 // only 3 in stock
            })
        };

        var controller = new OrderController(
            new TestOrderRepository(),
            new TestOrderItemRepository(),
            productRepo,
            cartRepo,
            cartItemRepo,
            new TestCouponRepository())
        {
            ControllerContext = CreateContext(userId: userId, roleId: 3)
        };

        var result = await controller.Create(new CreateOrderRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void ProductManagement_Endpoints_RequireModAccessPolicy_StaffAndAdminOnly()
    {
        var createAttr = typeof(ProductController).GetMethod(nameof(ProductController.Create))?
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(createAttr);
        Assert.Equal("ModAccess", createAttr.Policy);

        var uploadAttr = typeof(ProductController).GetMethod(nameof(ProductController.UploadImage))?
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(uploadAttr);
        Assert.Equal("ModAccess", uploadAttr.Policy);

        var withImageAttr = typeof(ProductController).GetMethod(nameof(ProductController.CreateWithImage))?
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(withImageAttr);
        Assert.Equal("ModAccess", withImageAttr.Policy);

        var updateAttr = typeof(ProductController).GetMethod(nameof(ProductController.Update))?
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(updateAttr);
        Assert.Equal("ModAccess", updateAttr.Policy);

        var deleteAttr = typeof(ProductController).GetMethod(nameof(ProductController.Delete))?
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(deleteAttr);
        Assert.Equal("ModAccess", deleteAttr.Policy);
    }

    [Theory]
    [InlineData(1)] // Admin (role 1)
    [InlineData(2)] // Staff (role 2)
    public async Task StaffAndAdmin_CanCreateProduct(int roleId)
    {
        Product? addedProduct = null;
        var productRepo = new TestProductRepository
        {
            OnAddAsync = p =>
            {
                addedProduct = p;
                return Task.FromResult(100);
            }
        };

        var controller = new ProductController(productRepo, new StubWebHostEnvironment("Development"))
        {
            ControllerContext = CreateContext(userId: 101, roleId: roleId)
        };

        var request = new CreateProductRequest
        {
            Name = "Organic Cotton T-Shirt",
            Category = "Tops",
            Price = 45.0,
            Stock = 20,
            Image = "https://example.com/tee.jpg",
            Description = "Comfortable everyday tee."
        };

        var result = await controller.Create(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(addedProduct);
        Assert.Equal("Organic Cotton T-Shirt", addedProduct.Name);
        Assert.Equal(45.0, addedProduct.Price);
    }
}

// =============================================================================
// TEST REPOSITORY IMPLEMENTATIONS
// =============================================================================

internal sealed class TestOrderRepository : IOrderRepository
{
    public Func<int, Task<Order?>>? OnGetByIdAsync { get; set; }
    public Func<int, double, IReadOnlyList<(int ProductId, int Quantity, double Price)>, int, Task<int>>? OnCreateOrderTransactionalAsync { get; set; }

    public Task<IEnumerable<Order>> GetAllAsync() => Task.FromResult<IEnumerable<Order>>([]);
    public Task<Order?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<Order?>(null);
    public Task<IEnumerable<Order>> GetByUserIdAsync(int userId) => Task.FromResult<IEnumerable<Order>>([]);
    public Task<PaginationModel<Order>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Order>());
    public Task<int> AddAsync(Order entity) => Task.FromResult(1);
    public Task UpdateAsync(Order entity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task<int> CreateOrderTransactionalAsync(
        int userId,
        double total,
        IReadOnlyList<(int ProductId, int Quantity, double Price)> items,
        int cartId,
        ClothesShopping.Api.AddressModule.Address? shippingAddress = null)
        => OnCreateOrderTransactionalAsync != null
            ? OnCreateOrderTransactionalAsync(userId, total, items, cartId)
            : Task.FromResult(1);

    public Task EnsureDatabaseMigratedAsync() => Task.CompletedTask;
}

internal sealed class TestOrderItemRepository : IOrderItemRepository
{
    public Task<IEnumerable<OrderItem>> GetAllAsync() => Task.FromResult<IEnumerable<OrderItem>>([]);
    public Task<OrderItem?> GetByIdAsync(int id) => Task.FromResult<OrderItem?>(null);
    public Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId) => Task.FromResult<IEnumerable<OrderItem>>([]);
    public Task AddAsync(OrderItem entity) => Task.CompletedTask;
    public Task UpdateAsync(OrderItem entity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task DeleteByOrderIdAsync(int orderId) => Task.CompletedTask;
}

internal sealed class TestCartRepository : ICartRepository
{
    public Func<int, Task<Cart?>>? OnGetByUserIdAsync { get; set; }
    public Func<int, Task<Cart?>>? OnGetByIdAsync { get; set; }

    public Task<Cart?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<Cart?>(null);
    public Task<Cart?> GetByUserIdAsync(int userId) => OnGetByUserIdAsync != null ? OnGetByUserIdAsync(userId) : Task.FromResult<Cart?>(null);
    public Task<int> AddAsync(Cart entity) => Task.FromResult(1);
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task DeleteByUserIdAsync(int userId) => Task.CompletedTask;
}

internal sealed class TestCartItemRepository : ICartItemRepository
{
    public Func<int, Task<CartItem?>>? OnGetByIdAsync { get; set; }
    public Func<int, Task<IEnumerable<CartItem>>>? OnGetByCartIdAsync { get; set; }

    public Task<IEnumerable<CartItem>> GetAllAsync() => Task.FromResult<IEnumerable<CartItem>>([]);
    public Task<CartItem?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<CartItem?>(null);
    public Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId) => OnGetByCartIdAsync != null ? OnGetByCartIdAsync(cartId) : Task.FromResult<IEnumerable<CartItem>>([]);
    public Task<int> AddAsync(CartItem entity) => Task.FromResult(1);
    public Task UpdateQuantityAsync(int cartItemId, int quantity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task DeleteByCartIdAsync(int cartId) => Task.CompletedTask;
}

internal sealed class TestPaymentRepository : IPaymentRepository
{
    public Func<int, Task<Payment?>>? OnGetByIdAsync { get; set; }

    public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>([]);
    public Task<Payment?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<Payment?>(null);
    public Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId) => Task.FromResult<IEnumerable<Payment>>([]);
    public Task<PaginationModel<Payment>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Payment>());
    public Task<int> AddAsync(Payment entity) => Task.FromResult(1);
    public Task UpdateStatusAsync(int id, string status) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class TestProductRepository : IProductRepository
{
    public Func<int, Task<Product?>>? OnGetByIdAsync { get; set; }
    public Func<Product, Task<int>>? OnAddAsync { get; set; }

    public Func<int, Task<IEnumerable<TopProductResponse>>>? OnGetTopAsync { get; set; }

    public Task<IEnumerable<Product>> GetAllAsync() => Task.FromResult<IEnumerable<Product>>([]);
    public Task<Product?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<Product?>(null);
    public Task<IEnumerable<Product>> GetByCategoryAsync(string category) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> SearchAsync(string query) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<IEnumerable<Product>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Product>>([]);
    public Task<PaginationModel<Product>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Product>());
    public Task<IEnumerable<TopProductResponse>> GetTopAsync(int count) => OnGetTopAsync != null ? OnGetTopAsync(count) : Task.FromResult<IEnumerable<TopProductResponse>>([]);
    public Task<int> AddAsync(Product entity) => OnAddAsync != null ? OnAddAsync(entity) : Task.FromResult(1);
    public Task UpdateAsync(Product entity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task BulkUploadAsync(List<Product> dataList) => Task.CompletedTask;
}

internal sealed class TestCouponRepository : ICouponRepository
{
    public Func<string, Task<Coupon?>>? OnGetByCodeAsync { get; set; }

    public Task<IEnumerable<Coupon>> GetAllAsync() => Task.FromResult<IEnumerable<Coupon>>([]);
    public Task<Coupon?> GetByIdAsync(int id) => Task.FromResult<Coupon?>(null);
    public Task<Coupon?> GetByCodeAsync(string code) => OnGetByCodeAsync != null ? OnGetByCodeAsync(code) : Task.FromResult<Coupon?>(null);
    public Task<PaginationModel<Coupon>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Coupon>());
    public Task<int> AddAsync(Coupon entity) => Task.FromResult(1);
    public Task UpdateAsync(Coupon entity) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class TestUsermanRepository : IUsermanRepository
{
    public Func<Userman, Task>? OnAddAsync { get; set; }
    public Func<string, Task<Userman?>>? OnGetByUserNameAsync { get; set; }
    public Func<int, Task<Userman?>>? OnGetByIdAsync { get; set; }
    public Func<Userman, Task>? OnUpdateAsync { get; set; }

    public Task<IEnumerable<Userman>> GetAllAsync() => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<Userman?> GetByIdAsync(int userid) => OnGetByIdAsync != null ? OnGetByIdAsync(userid) : Task.FromResult<Userman?>(null);
    public Task DeleteAsync(int userid) => Task.CompletedTask;
    public Task AddAsync(Userman entity) => OnAddAsync != null ? OnAddAsync(entity) : Task.CompletedTask;
    public Task UpdateAsync(Userman entity) => OnUpdateAsync != null ? OnUpdateAsync(entity) : Task.CompletedTask;
    public Task<Userman?> GetByUserNameAsync(string username) => OnGetByUserNameAsync != null ? OnGetByUserNameAsync(username) : Task.FromResult<Userman?>(null);
    public Task<Userman?> GetByEmailAsync(string email) => Task.FromResult<Userman?>(null);
    public Task DeleteAllAsync() => Task.CompletedTask;
    public Task<IEnumerable<Userman>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> SearchAsync(string query) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<IEnumerable<Userman>> SearchAsyncAll(string query) => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<PaginationModel<Userman>> GetPaginatedAsync(int pageNumber, int pageSize) => Task.FromResult(new PaginationModel<Userman>());
    public Task BulkUploadAsync(List<Userman> dataList) => Task.CompletedTask;
    public Task EnsureDatabaseMigratedAsync() => Task.CompletedTask;
    public Task<IEnumerable<Userman>> GetPendingStaffAsync() => Task.FromResult<IEnumerable<Userman>>([]);
    public Task<int> CountActiveAdminsAsync() => Task.FromResult(1);
}
