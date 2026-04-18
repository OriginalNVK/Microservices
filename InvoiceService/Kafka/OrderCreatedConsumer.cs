using Confluent.Kafka;
using System.Text.Json;
using InvoiceService.Data;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Kafka;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrderCreatedConsumer> logger)
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
            GroupId = "invoice-service-order-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { "order.created", "order.updated" });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    if (result.Topic == "order.created")
                        await HandleOrderCreated(result.Message.Value);
                    else if (result.Topic == "order.updated")
                        await HandleOrderUpdated(result.Message.Value);
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

    private async Task HandleOrderCreated(string json)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var maHD = data.GetProperty("MaHD").GetInt32();

            if (await db.HoaDons.AnyAsync(h => h.MaHD == maHD))
                return;

            var hoaDon = new HoaDon
            {
                MaHD = maHD,
                MaKH = data.GetProperty("MaKH").GetString() ?? "",
                HoTen = data.TryGetProperty("HoTen", out var hoTen) ? hoTen.GetString() : null,
                DiaChi = data.GetProperty("DiaChi").GetString() ?? "",
                CachThanhToan = data.GetProperty("CachThanhToan").GetString() ?? "",
                CachVanChuyen = data.GetProperty("CachVanChuyen").GetString() ?? "",
                PhiVanChuyen = data.GetProperty("PhiVanChuyen").GetDecimal(),
                NgayDat = data.GetProperty("NgayDat").GetDateTime(),
                TrangThai = 0
            };

            db.HoaDons.Add(hoaDon);
            await db.SaveChangesAsync();

            if (data.TryGetProperty("Items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    db.ChiTietHDs.Add(new ChiTietHD
                    {
                        MaHD = maHD,
                        MaHH = item.GetProperty("MaHH").GetInt32(),
                        TenHH = item.GetProperty("TenHH").GetString() ?? "",
                        DonGia = item.GetProperty("DonGia").GetDecimal(),
                        SoLuong = item.GetProperty("SoLuong").GetInt32(),
                        GiamGia = item.GetProperty("GiamGia").GetDecimal()
                    });
                }
                await db.SaveChangesAsync();
            }

            _logger.LogInformation("Invoice created for order {MaHD}", maHD);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order.created event");
        }
    }

    private async Task HandleOrderUpdated(string json)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var maHD = data.GetProperty("MaHD").GetInt32();

            var hd = await db.HoaDons.FindAsync(maHD);
            if (hd == null) return;

            hd.TrangThai = data.GetProperty("TrangThai").GetInt32();

            if (data.TryGetProperty("MaNV", out var maNV) && maNV.ValueKind != JsonValueKind.Null)
                hd.MaNV = maNV.GetString();

            if (data.TryGetProperty("NgayGiao", out var ngayGiao) && ngayGiao.ValueKind != JsonValueKind.Null)
                hd.NgayGiao = DateOnly.Parse(ngayGiao.GetString() ?? "");

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order.updated event");
        }
    }
}
