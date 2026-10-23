using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Controllers;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.ProductsModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ClothesShopping.Api.Tests;

public sealed class TopProductsTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string? _originalDbFilePath;
    private readonly SqliteConnection _connection;
    private readonly ProductRepository _repo;

    public TopProductsTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"topproducts_test_{Guid.NewGuid():N}.db");
        _originalDbFilePath = Environment.GetEnvironmentVariable("DB_FILE_PATH");
        Environment.SetEnvironmentVariable("DB_FILE_PATH", _dbPath);

        var connStr = $"Data Source={_dbPath}";
        _connection = new SqliteConnection(connStr);
        _connection.Open();

        // Create tables needed by GetTopAsync
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                category TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                image TEXT,
                description TEXT
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                total REAL NOT NULL,
                order_date TEXT,
                status TEXT NOT NULL
            );

            CREATE TABLE order_items (
                id INTEGER PRIMARY KEY,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                price REAL NOT NULL
            );

            CREATE TABLE reviews (
                review_id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                rating REAL NOT NULL,
                comment TEXT,
                review_date TEXT
            );
        ";
        cmd.ExecuteNonQuery();

        var myCon = new MyCon();
        _repo = new ProductRepository(myCon);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("DB_FILE_PATH", _originalDbFilePath);
        _connection.Close();
        _connection.Dispose();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task GetTopAsync_RanksByUnitsSold_ExcludesCancelledOrders_AndFillsWithNewest()
    {
        // Arrange
        // Products:
        // P1: In stock, 5 units sold in delivered order
        // P2: In stock, 50 units in CANCELLED order (must be excluded -> 0 sold)
        // P3: In stock, 15 units sold in completed order (should rank #1)
        // P4: In stock, 0 units sold, id = 4
        // P5: In stock, 0 units sold, id = 5 (newer than P4)
        // P6: Out of stock (stock = 0), 2 units sold
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO products (id, name, category, price, stock, image, description) VALUES
                (1, 'T-Shirt', 'Shirts', 20.0, 10, 'img1.jpg', 'Shirt 1'),
                (2, 'Jeans', 'Pants', 50.0, 8, 'img2.jpg', 'Jeans 2'),
                (3, 'Hoodie', 'Hoodies', 40.0, 15, 'img3.jpg', 'Hoodie 3'),
                (4, 'Cap', 'Accessories', 15.0, 5, 'img4.jpg', 'Cap 4'),
                (5, 'Beanie', 'Accessories', 12.0, 7, 'img5.jpg', 'Beanie 5'),
                (6, 'Sold Out Jacket', 'Jackets', 90.0, 0, 'img6.jpg', 'Jacket 6');

                -- Orders
                INSERT INTO orders (id, user_id, total, status) VALUES
                (101, 1, 600.0, 'completed'),
                (102, 2, 100.0, 'Delivered'),
                (103, 3, 2500.0, 'Cancelled'),
                (104, 4, 2500.0, 'CANCELLED'),
                (105, 5, 180.0, 'pending');

                -- Order Items
                INSERT INTO order_items (id, order_id, product_id, quantity, price) VALUES
                (1, 101, 3, 15, 40.0),      -- P3: 15 units sold (completed)
                (2, 102, 1, 5, 20.0),       -- P1: 5 units sold (Delivered)
                (3, 103, 2, 30, 50.0),      -- P2 in Cancelled order (should NOT count)
                (4, 104, 2, 20, 50.0),      -- P2 in CANCELLED order (should NOT count)
                (5, 105, 6, 2, 90.0);       -- P6: 2 units sold, but stock = 0

                -- Reviews
                INSERT INTO reviews (review_id, user_id, product_id, rating, comment) VALUES
                (1, 1, 3, 5.0, 'Great!'),
                (2, 2, 3, 4.0, 'Good');     -- P3: avg rating 4.5, reviewCount 2
            ";
            cmd.ExecuteNonQuery();
        }

        // Act
        var top = (await _repo.GetTopAsync(5)).ToList();

        // Assert
        Assert.Equal(5, top.Count);

        // #1 must be P3 (15 sold, in stock)
        Assert.Equal(3, top[0].Id);
        Assert.Equal(15, top[0].SoldCount);
        Assert.Equal(4.5, top[0].AverageRating);
        Assert.Equal(2, top[0].ReviewCount);

        // #2 must be P1 (5 sold, in stock)
        Assert.Equal(1, top[1].Id);
        Assert.Equal(5, top[1].SoldCount);

        // P2 has 0 valid units sold because all its orders were Cancelled.
        // Remaining slots for in-stock products with 0 sales fill by newest id (id DESC):
        // P5 (id 5) > P4 (id 4) > P2 (id 2)
        Assert.Equal(5, top[2].Id);
        Assert.Equal(0, top[2].SoldCount);

        Assert.Equal(4, top[3].Id);
        Assert.Equal(0, top[3].SoldCount);

        Assert.Equal(2, top[4].Id);
        Assert.Equal(0, top[4].SoldCount);

        // Note: P6 (stock = 0) is placed after in-stock products, so it does not displace in-stock P5, P4, P2
    }

    [Fact]
    public async Task ProductController_GetTop_ClampsCountBetween1And20()
    {
        // Arrange
        int requestedCount = -1;
        var stubRepo = new TestProductRepository
        {
            OnGetTopAsync = c =>
            {
                requestedCount = c;
                return Task.FromResult<IEnumerable<TopProductResponse>>(new List<TopProductResponse>());
            }
        };

        var controller = new ProductController(stubRepo);

        // Act - request count <= 0
        var res1 = await controller.GetTop(0);
        Assert.IsType<OkObjectResult>(res1.Result);
        Assert.Equal(1, requestedCount);

        // Act - request count > 20
        var res2 = await controller.GetTop(50);
        Assert.IsType<OkObjectResult>(res2.Result);
        Assert.Equal(20, requestedCount);

        // Act - request count valid
        var res3 = await controller.GetTop(8);
        Assert.IsType<OkObjectResult>(res3.Result);
        Assert.Equal(8, requestedCount);
    }
}
