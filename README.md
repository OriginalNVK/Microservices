# HKShop Microservices - ASP.NET Core 8

## Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                          Client                                  │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                    ┌───────────▼──────────┐
                    │    API Gateway        │
                    │    Port: 5000         │
                    │  (YARP Reverse Proxy) │
                    └──┬──┬──┬──┬──┬───────┘
                       │  │  │  │  │
         ┌─────────────┘  │  │  │  └──────────────┐
         │         ┌──────┘  │  └──────┐           │
         ▼         ▼         ▼         ▼           ▼
   ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
   │  User    │ │ Product  │ │  Cart    │ │  Order   │ │ Invoice  │
   │ Service  │ │ Service  │ │ Service  │ │ Service  │ │ Service  │
   │ :5001    │ │ :5002    │ │ :5003    │ │ :5004    │ │ :5005    │
   └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
        │            │            │            │            │
   ┌────▼─────┐ ┌────▼─────┐ ┌────▼─────┐ ┌────▼─────┐ ┌────▼─────┐
   │ UserDB   │ │ProductDB │ │  CartDB  │ │ OrderDB  │ │InvoiceDB │
   │ :5433    │ │ :5434    │ │ :5435    │ │ :5436    │ │ :5437    │
   └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘
                                    │
              ┌─────────────────────▼──────────────────────┐
              │                  Apache Kafka               │
              │                   Port: 9092                │
              └────────────────────────────────────────────┘
```

## Cấu trúc dự án

```
hkshop-microservices/
├── UserService/          # Quản lý người dùng, xác thực
├── ProductService/       # Quản lý hàng hóa, loại hàng hóa
├── CartService/          # Quản lý giỏ hàng
├── OrderService/         # Quản lý đặt hàng
├── InvoiceService/       # Quản lý hóa đơn, báo cáo
├── ApiGateway/           # YARP Reverse Proxy
├── SharedKernel/         # Kafka topics, shared events
└── docker-compose.yml    # Docker Compose toàn bộ hệ thống
```

## Database phân chia

| Service         | Database          | Tables                              |
|-----------------|-------------------|-------------------------------------|
| UserService     | UserServiceDb     | NguoiDung, KhachHang, NhanVien     |
| ProductService  | ProductServiceDb  | Loai, HangHoa                      |
| CartService     | CartServiceDb     | Cart, HangHoaCache                 |
| OrderService    | OrderServiceDb    | HoaDon, ChiTietHD                  |
| InvoiceService  | InvoiceServiceDb  | HoaDon, ChiTietHD (read replica)   |

## Kafka Topics (Giao tiếp giữa các services)

| Topic               | Producer        | Consumers                      |
|---------------------|-----------------|--------------------------------|
| `user.registered`   | UserService     | -                              |
| `customer.created`  | UserService     | CartService, OrderService      |
| `product.created`   | ProductService  | CartService                    |
| `product.updated`   | ProductService  | CartService                    |
| `product.deleted`   | ProductService  | CartService                    |
| `cart.updated`      | CartService     | -                              |
| `cart.cleared`      | CartService     | -                              |
| `order.created`     | OrderService    | InvoiceService                 |
| `order.updated`     | OrderService    | InvoiceService                 |
| `order.cancelled`   | OrderService    | -                              |

## API Endpoints

### UserService (Port 5001)
- `POST /api/auth/register` - Đăng ký khách hàng
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/forgot-password` - Quên mật khẩu
- `POST /api/auth/reset-password` - Đặt lại mật khẩu
- `POST /api/auth/change-password` - Đổi mật khẩu
- `GET /api/khachhang/profile` - Thông tin cá nhân
- `PUT /api/khachhang/profile` - Cập nhật thông tin
- `GET /api/khachhang` - Danh sách KH (Admin)
- `GET /api/nhanvien` - Danh sách NV (Admin)
- `POST /api/nhanvien` - Thêm nhân viên (Admin)

### ProductService (Port 5002)
- `GET /api/loai` - Danh sách loại hàng hóa
- `POST /api/loai` - Thêm loại (Admin)
- `PUT /api/loai/{id}` - Sửa loại (Admin)
- `DELETE /api/loai/{id}` - Xóa loại (Admin)
- `GET /api/hanghoa` - Danh sách hàng hóa (filter, sort, page)
- `GET /api/hanghoa/{id}` - Chi tiết sản phẩm
- `POST /api/hanghoa` - Thêm sản phẩm (Admin)
- `PUT /api/hanghoa/{id}` - Sửa sản phẩm (Admin)
- `DELETE /api/hanghoa/{id}` - Xóa sản phẩm (Admin)
- `GET /api/hanghoa/best-sellers` - Top bán chạy
- `GET /api/hanghoa/on-sale` - Đang giảm giá

### CartService (Port 5003)
- `GET /api/cart` - Lấy giỏ hàng
- `POST /api/cart/add` - Thêm vào giỏ
- `PUT /api/cart/{id}` - Cập nhật số lượng
- `DELETE /api/cart/{id}` - Xóa 1 sản phẩm
- `DELETE /api/cart/clear` - Xóa toàn bộ giỏ
- `GET /api/cart/count` - Số lượng sản phẩm

### OrderService (Port 5004)
- `POST /api/order` - Tạo đơn hàng
- `GET /api/order/my-orders` - Đơn hàng của tôi
- `GET /api/order/{id}` - Chi tiết đơn hàng
- `GET /api/order` - Tất cả đơn hàng (Admin)
- `PUT /api/order/{id}/status` - Cập nhật trạng thái (Admin)
- `PUT /api/order/{id}/cancel` - Hủy đơn hàng
- `GET /api/order/statistics` - Thống kê (Admin)

### InvoiceService (Port 5005)
- `GET /api/invoice/my-invoices` - Hóa đơn của tôi
- `GET /api/invoice/{id}` - Chi tiết hóa đơn
- `GET /api/invoice/{id}/export/pdf` - Xuất PDF
- `GET /api/invoice` - Tất cả hóa đơn (Admin)
- `GET /api/invoice/report/excel` - Báo cáo Excel
- `GET /api/invoice/report/revenue` - Báo cáo doanh thu
- `GET /api/invoice/report/top-products` - Top sản phẩm bán chạy
- `GET /api/invoice/report/revenue/excel` - Xuất báo cáo Excel

## Cài đặt và chạy

### Yêu cầu
- Docker & Docker Compose
- .NET 8 SDK (để chạy locally)

### Chạy toàn bộ hệ thống với Docker
```bash
cd hkshop-microservices
docker-compose up -d
```

### Chạy từng service riêng lẻ (Development)
```bash
# Terminal 1 - UserService
cd UserService
dotnet run

# Terminal 2 - ProductService  
cd ProductService
dotnet run

# Terminal 3 - CartService
cd CartService
dotnet run

# Terminal 4 - OrderService
cd OrderService
dotnet run

# Terminal 5 - InvoiceService
cd InvoiceService
dotnet run

# Terminal 6 - ApiGateway
cd ApiGateway
dotnet run
```

### Migration database
```bash
# UserService
cd UserService
dotnet ef migrations add InitialCreate
dotnet ef database update

# ProductService
cd ProductService
dotnet ef migrations add InitialCreate
dotnet ef database update

# CartService
cd CartService
dotnet ef migrations add InitialCreate
dotnet ef database update

# OrderService
cd OrderService
dotnet ef migrations add InitialCreate
dotnet ef database update

# InvoiceService
cd InvoiceService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Swagger UI

Sau khi chạy:
- UserService: http://localhost:5001/swagger
- ProductService: http://localhost:5002/swagger
- CartService: http://localhost:5003/swagger
- OrderService: http://localhost:5004/swagger
- InvoiceService: http://localhost:5005/swagger
- Kafka UI: http://localhost:8080

## Phân quyền

| VaiTro | Vai trò  | Mô tả                          |
|--------|----------|--------------------------------|
| 0      | Khách hàng| Mua hàng, quản lý giỏ hàng    |
| 1      | Admin/NV | Quản lý toàn bộ hệ thống       |

## Công nghệ sử dụng

- **Framework**: ASP.NET Core 8
- **ORM**: Entity Framework Core 8 + PostgreSQL
- **Authentication**: JWT Bearer Token
- **Message Broker**: Apache Kafka (Confluent.Kafka)
- **API Gateway**: YARP Reverse Proxy
- **PDF Export**: QuestPDF
- **Excel Export**: ClosedXML
- **Email**: MailKit
- **Password Hashing**: BCrypt.Net
- **API Docs**: Swagger/OpenAPI
