using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoiceService.Models;

namespace InvoiceService.Services;

public interface IPdfExportService
{
    byte[] GenerateInvoicePdf(HoaDon hoaDon);
}

public class PdfExportService : IPdfExportService
{
    public PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(HoaDon hoaDon)
    {
        var tongTienHang = hoaDon.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong);
        var tongTien = tongTienHang + hoaDon.PhiVanChuyen;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Item().Element(x => ComposeInvoiceInfo(x, hoaDon));
                        col.Item().Height(15);
                        col.Item().Element(x => ComposeCustomerInfo(x, hoaDon));
                        col.Item().Height(15);
                        col.Item().Element(x => ComposeItemsTable(x, hoaDon));
                        col.Item().Height(10);
                        col.Item().Element(x => ComposeTotals(x, tongTienHang, hoaDon.PhiVanChuyen, tongTien));
                        col.Item().Height(20);
                        col.Item().Element(ComposeSignature);
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(9);
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("HKShop").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text("Hệ thống bán hàng trực tuyến").FontSize(10).FontColor(Colors.Grey.Medium);
                col.Item().Text("Địa chỉ: 123 Đường ABC, TP. Hồ Chí Minh").FontSize(9);
                col.Item().Text("Điện thoại: 0123-456-789 | Email: info@hkshop.com").FontSize(9);
            });

            row.ConstantItem(150).AlignRight().Column(col =>
            {
                col.Item().Text("HÓA ĐƠN BÁN HÀNG").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Ngày: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
            });
        });
    }

    private void ComposeInvoiceInfo(IContainer container, HoaDon hoaDon)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text($"Mã hóa đơn: #{hoaDon.MaHD}").Bold();
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignRight().Text($"Ngày đặt: {hoaDon.NgayDat:dd/MM/yyyy HH:mm}");

            table.Cell().Padding(5).Text($"Mã khách hàng: {hoaDon.MaKH}");
            table.Cell().Padding(5).AlignRight().Text($"Trạng thái: {GetTrangThaiText(hoaDon.TrangThai)}");
        });
    }

    private void ComposeCustomerInfo(IContainer container, HoaDon hoaDon)
    {
        container.Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
        {
            col.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold().FontSize(12);
            col.Item().Height(5);
            col.Item().Text($"Họ tên: {hoaDon.HoTen ?? hoaDon.MaKH}");
            if (!string.IsNullOrEmpty(hoaDon.EmailKH))
                col.Item().Text($"Email: {hoaDon.EmailKH}");
            if (!string.IsNullOrEmpty(hoaDon.DienThoaiKH))
                col.Item().Text($"Điện thoại: {hoaDon.DienThoaiKH}");
            col.Item().Text($"Địa chỉ giao hàng: {hoaDon.DiaChi}");
            col.Item().Text($"Phương thức thanh toán: {hoaDon.CachThanhToan}");
            col.Item().Text($"Phương thức vận chuyển: {hoaDon.CachVanChuyen}");
        });
    }

    private void ComposeItemsTable(IContainer container, HoaDon hoaDon)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tên sản phẩm").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Đơn giá").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Giảm giá").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("SL").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Thành tiền").Bold().FontColor(Colors.White);
            });

            var stt = 1;
            foreach (var ct in hoaDon.ChiTietHDs)
            {
                var giaSauGiam = ct.DonGia * (1 - ct.GiamGia / 100);
                var thanhTien = giaSauGiam * ct.SoLuong;
                var bg = stt % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                table.Cell().Background(bg).Padding(5).Text(stt.ToString());
                table.Cell().Background(bg).Padding(5).Text(ct.TenHH);
                table.Cell().Background(bg).Padding(5).AlignRight().Text(FormatCurrency(ct.DonGia));
                table.Cell().Background(bg).Padding(5).AlignCenter().Text($"{ct.GiamGia}%");
                table.Cell().Background(bg).Padding(5).AlignCenter().Text(ct.SoLuong.ToString());
                table.Cell().Background(bg).Padding(5).AlignRight().Text(FormatCurrency(thanhTien));

                stt++;
            }
        });
    }

    private void ComposeTotals(IContainer container, decimal tongTienHang, decimal phiVanChuyen, decimal tongTien)
    {
        container.AlignRight().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(150).Text("Tổng tiền hàng:");
                row.ConstantItem(120).AlignRight().Text(FormatCurrency(tongTienHang));
            });
            col.Item().Row(row =>
            {
                row.ConstantItem(150).Text("Phí vận chuyển:");
                row.ConstantItem(120).AlignRight().Text(FormatCurrency(phiVanChuyen));
            });
            col.Item().BorderTop(1).BorderColor(Colors.Blue.Darken2).Padding(3).Row(row =>
            {
                row.ConstantItem(150).Text("TỔNG CỘNG:").Bold().FontSize(13);
                row.ConstantItem(120).AlignRight().Text(FormatCurrency(tongTien)).Bold().FontSize(13).FontColor(Colors.Red.Medium);
            });
            col.Item().Text($"(Bằng chữ: {SoTienBangChu(tongTien)})").FontSize(9).Italic();
        });
    }

    private void ComposeSignature(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("Khách hàng").Bold();
                col.Item().Height(50);
                col.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("Nhân viên bán hàng").Bold();
                col.Item().Height(50);
                col.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static string GetTrangThaiText(int trangThai) => trangThai switch
    {
        0 => "Chờ xác nhận",
        1 => "Đã xác nhận",
        2 => "Đang giao hàng",
        3 => "Đã giao hàng",
        4 => "Đã hủy",
        _ => "Không xác định"
    };

    private static string FormatCurrency(decimal amount) => $"{amount:N0} đ";

    private static string SoTienBangChu(decimal amount)
    {
        if (amount == 0) return "Không đồng";
        return $"{amount:N0} đồng";
    }
}
