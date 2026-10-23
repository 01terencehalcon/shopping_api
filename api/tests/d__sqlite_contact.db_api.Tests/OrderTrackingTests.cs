using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClothesShopping.Api.Controllers;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.OrdersModule;
using ClothesShopping.Api.OrderItemsModule;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.CartModule;
using ClothesShopping.Api.CartItemsModule;
using ClothesShopping.Api.CouponsModule;
using ClothesShopping.Api.AddressModule;
using ClothesShopping.Api.NotificationsModule;
using ClothesShopping.Api.OrderTrackingModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ClothesShopping.Api.Tests;

public sealed class OrderTrackingTests
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

    private class MockOrderRepo : IOrderRepository
    {
        public Func<int, Task<Order?>>? OnGetByIdAsync;
        public Func<Order, Task>? OnUpdateAsync;

        public Task<Order?> GetByIdAsync(int id) => OnGetByIdAsync != null ? OnGetByIdAsync(id) : Task.FromResult<Order?>(null);
        public Task<IEnumerable<Order>> GetAllAsync() => Task.FromResult<IEnumerable<Order>>(new List<Order>());
        public Task<IEnumerable<Order>> GetByUserIdAsync(int userId) => Task.FromResult<IEnumerable<Order>>(new List<Order>());
        public Task<ClothesShopping.Api.Main.PaginationModel<Order>> GetPaginatedAsync(int p, int s) => throw new NotImplementedException();
        public Task<int> AddAsync(Order entity) => Task.FromResult(1);
        public Task UpdateAsync(Order entity) => OnUpdateAsync != null ? OnUpdateAsync(entity) : Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<int> CreateOrderTransactionalAsync(int userId, double total, IReadOnlyList<(int ProductId, int Quantity, double Price)> items, int cartId) => Task.FromResult(1);
    }

    private class MockTrackingRepo : IOrderTrackingRepository
    {
        public List<OrderTracking> Checkpoints = new();
        public Task<IEnumerable<OrderTracking>> GetByOrderIdAsync(int orderId) => Task.FromResult<IEnumerable<OrderTracking>>(Checkpoints.Where(c => c.OrderId == orderId));
        public Task<int> AddAsync(OrderTracking entity)
        {
            entity.Id = Checkpoints.Count + 1;
            Checkpoints.Add(entity);
            return Task.FromResult(entity.Id);
        }
        public Task EnsureDatabaseMigratedAsync() => Task.CompletedTask;
    }

    private class MockAddressRepo : IAddressRepository
    {
        public List<Address> Addresses = new();
        public Task<IEnumerable<Address>> GetByUserIdAsync(int userId) => Task.FromResult<IEnumerable<Address>>(Addresses.Where(a => a.UserId == userId));
        public Task<Address?> GetByIdAsync(int id) => Task.FromResult(Addresses.FirstOrDefault(a => a.AddressId == id));
        public Task<int> AddAsync(Address entity) => Task.FromResult(1);
        public Task UpdateAsync(Address entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task DeleteByUserIdAsync(int userId) => Task.CompletedTask;
    }

    private class MockNotificationRepo : INotificationRepository
    {
        public List<Notification> Notifications = new();
        public Task<IEnumerable<Notification>> GetAllAsync() => Task.FromResult<IEnumerable<Notification>>(Notifications);
        public Task<Notification?> GetByIdAsync(int id) => Task.FromResult(Notifications.FirstOrDefault(n => n.NotificationId == id));
        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) => Task.FromResult<IEnumerable<Notification>>(Notifications.Where(n => n.UserId == userId));
        public Task<ClothesShopping.Api.Main.PaginationModel<Notification>> GetPaginatedAsync(int p, int s) => throw new NotImplementedException();
        public Task<int> AddAsync(Notification entity)
        {
            Notifications.Add(entity);
            return Task.FromResult(Notifications.Count);
        }
        public Task MarkAsReadAsync(int id) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task DeleteByUserIdAsync(int userId) => Task.CompletedTask;
    }

    [Fact]
    public async Task Customer_CannotRead_AnotherCustomer_Tracking_Returns404()
    {
        const int userAId = 101;
        const int userBId = 102;
        const int orderId = 10;

        var orderRepo = new MockOrderRepo
        {
            OnGetByIdAsync = _ => Task.FromResult<Order?>(new Order
            {
                Id = orderId,
                UserId = userBId, // Belongs to user B
                Status = "shipped"
            })
        };

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            new MockTrackingRepo(),
            new MockAddressRepo(),
            new MockNotificationRepo(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: userAId, roleId: 3) // Customer user A
        };

        var result = await controller.GetTracking(orderId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Admin_CanRead_AnyCustomer_Tracking_Returns200()
    {
        const int adminId = 999;
        const int customerId = 102;
        const int orderId = 10;

        var orderRepo = new MockOrderRepo
        {
            OnGetByIdAsync = _ => Task.FromResult<Order?>(new Order
            {
                Id = orderId,
                UserId = customerId,
                Status = "pending"
            })
        };

        var trackingRepo = new MockTrackingRepo();
        trackingRepo.Checkpoints.Add(new OrderTracking
        {
            Id = 1,
            OrderId = orderId,
            Status = "pending",
            Latitude = 40.7128,
            Longitude = -74.0060,
            Note = "Order received",
            CreatedAt = DateTime.UtcNow.ToString("o")
        });

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            trackingRepo,
            new MockAddressRepo(),
            new MockNotificationRepo(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: adminId, roleId: 1) // Admin
        };

        var result = await controller.GetTracking(orderId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<OrderTrackingResponse>(okResult.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Single(response.Checkpoints);
    }

    [Theory]
    [InlineData(91.0, 10.0)]
    [InlineData(-95.0, 10.0)]
    public async Task PostCheckpoint_InvalidLatitude_Returns400(double lat, double lng)
    {
        var controller = new OrderController(
            new MockOrderRepo(),
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            new MockTrackingRepo(),
            new MockAddressRepo(),
            new MockNotificationRepo(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: 1, roleId: 1)
        };

        var result = await controller.AddCheckpoint(1, new AddTrackingCheckpointRequest
        {
            Status = "shipped",
            Latitude = lat,
            Longitude = lng
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(10.0, 185.0)]
    [InlineData(10.0, -190.0)]
    public async Task PostCheckpoint_InvalidLongitude_Returns400(double lat, double lng)
    {
        var controller = new OrderController(
            new MockOrderRepo(),
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            new MockTrackingRepo(),
            new MockAddressRepo(),
            new MockNotificationRepo(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: 1, roleId: 1)
        };

        var result = await controller.AddCheckpoint(1, new AddTrackingCheckpointRequest
        {
            Status = "shipped",
            Latitude = lat,
            Longitude = lng
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PostCheckpoint_InvalidStatus_Returns400()
    {
        var controller = new OrderController(
            new MockOrderRepo(),
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            new MockTrackingRepo(),
            new MockAddressRepo(),
            new MockNotificationRepo(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: 1, roleId: 1)
        };

        var result = await controller.AddCheckpoint(1, new AddTrackingCheckpointRequest
        {
            Status = "non_existent_status",
            Latitude = 40.0,
            Longitude = -70.0
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Admin_CanPost_ValidCheckpoint_UpdatesOrderAndCreatesNotification()
    {
        const int orderId = 25;
        const int customerId = 44;

        Order? updatedOrder = null;
        var orderRepo = new MockOrderRepo
        {
            OnGetByIdAsync = _ => Task.FromResult<Order?>(new Order
            {
                Id = orderId,
                UserId = customerId,
                Status = "pending"
            }),
            OnUpdateAsync = o =>
            {
                updatedOrder = o;
                return Task.CompletedTask;
            }
        };

        var trackingRepo = new MockTrackingRepo();
        var notifRepo = new MockNotificationRepo();

        var controller = new OrderController(
            orderRepo,
            new TestOrderItemRepository(),
            new TestProductRepository(),
            new TestCartRepository(),
            new TestCartItemRepository(),
            new TestCouponRepository(),
            trackingRepo,
            new MockAddressRepo(),
            notifRepo,
            new ConfigurationBuilder().Build())
        {
            ControllerContext = CreateContext(userId: 1, roleId: 1) // Admin
        };

        var result = await controller.AddCheckpoint(orderId, new AddTrackingCheckpointRequest
        {
            Status = "out_for_delivery",
            Latitude = 40.7306,
            Longitude = -73.9352,
            Note = "Driver is on the way"
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(trackingRepo.Checkpoints);
        Assert.Equal("out_for_delivery", trackingRepo.Checkpoints[0].Status);
        Assert.NotNull(updatedOrder);
        Assert.Equal("out_for_delivery", updatedOrder.Status);
        Assert.Single(notifRepo.Notifications);
        Assert.Equal(customerId, notifRepo.Notifications[0].UserId);
        Assert.Contains("Driver is on the way", notifRepo.Notifications[0].Message);
    }
}
