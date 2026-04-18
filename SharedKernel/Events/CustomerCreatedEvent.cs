namespace SharedKernel.Events;

public class CustomerCreatedEvent
{
    public string MaKH { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public string? DiaChi { get; set; }
    public DateTime CreatedAt { get; set; }
}
