namespace SharedKernel.Events;

public class UserRegisteredEvent
{
    public int UserId { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public int VaiTro { get; set; }
    public DateTime NgayTao { get; set; }
}
