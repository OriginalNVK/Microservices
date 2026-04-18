using Confluent.Kafka;
using System.Text.Json;
using CartService.Data;
using CartService.Models;
using Microsoft.EntityFrameworkCore;

namespace CartService.Kafka;

public class ProductUpdatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductUpdatedConsumer> _logger;

    public ProductUpdatedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ProductUpdatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "cart-service-product-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { "product.created", "product.updated", "product.deleted" });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    await HandleProductEvent(result.Topic, result.Message.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleProductEvent(string topic, string json)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data == null) return;

            var maHH = data["MaHH"].GetInt32();

            if (topic == "product.deleted")
            {
                var cache = await db.HangHoaCaches.FindAsync(maHH);
                if (cache != null) db.HangHoaCaches.Remove(cache);

                var cartItems = await db.Carts.Where(c => c.MaHH == maHH).ToListAsync();
                db.Carts.RemoveRange(cartItems);

                await db.SaveChangesAsync();
                _logger.LogInformation("Product {MaHH} removed from all carts", maHH);
                return;
            }

            var tenHH = data.ContainsKey("TenHH") ? data["TenHH"].GetString() ?? "" : "";
            var donGia = data.ContainsKey("DonGia") && data["DonGia"].ValueKind != JsonValueKind.Null
                ? data["DonGia"].GetDecimal() : 0;
            var giamGia = data.ContainsKey("GiamGia") ? data["GiamGia"].GetDecimal() : 0;
            var hinh = data.ContainsKey("Hinh") && data["Hinh"].ValueKind != JsonValueKind.Null
                ? data["Hinh"].GetString() : null;

            var existing = await db.HangHoaCaches.FindAsync(maHH);
            if (existing == null)
            {
                db.HangHoaCaches.Add(new HangHoaCache
                {
                    MaHH = maHH,
                    TenHH = tenHH,
                    DonGia = donGia,
                    GiamGia = giamGia,
                    Hinh = hinh,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.TenHH = tenHH;
                existing.DonGia = donGia;
                existing.GiamGia = giamGia;
                existing.Hinh = hinh;
                existing.UpdatedAt = DateTime.UtcNow;

                var cartItems = await db.Carts.Where(c => c.MaHH == maHH).ToListAsync();
                foreach (var item in cartItems)
                    item.DonGia = donGia;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling product event from topic {Topic}", topic);
        }
    }
}
