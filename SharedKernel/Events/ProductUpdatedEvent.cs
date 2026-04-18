namespace SharedKernel.Events;

public class ProductUpdatedEvent
{
    public int MaHH { get; set; }
    public string TenHH { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; }
    public string? Hinh { get; set; }
    public DateTime UpdatedAt { get; set; }
}
