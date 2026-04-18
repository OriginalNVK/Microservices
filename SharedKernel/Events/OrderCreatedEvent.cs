namespace SharedKernel.Events;

public class OrderCreatedEvent
{
    public int MaHD { get; set; }
    public string MaKH { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public string CachThanhToan { get; set; } = string.Empty;
    public string CachVanChuyen { get; set; } = string.Empty;
    public decimal PhiVanChuyen { get; set; }
    public decimal TongTien { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public DateTime NgayDat { get; set; }
}

public class OrderItemEvent
{
    public int MaHH { get; set; }
    public string TenHH { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; }
}
