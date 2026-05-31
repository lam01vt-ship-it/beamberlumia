# Tosix Decor — Backend API

ASP.NET Core 8 API cho website [Tosix Decor](https://tosixdecor.com.vn/), theo kiến trúc dự án Krik (`bekrik`).

## Yêu cầu

- .NET 8 SDK
- PostgreSQL (mặc định DB: `data-tosix`)

## Chạy local

```powershell
cd D:\Projects1\betosix\src\Tosix.Api
dotnet run
```

API: http://localhost:5208  
Swagger: http://localhost:5208/swagger

## Tài khoản admin mặc định

- Email: `admin@tosix.local`
- Mật khẩu: `Admin123!`

## Đồng bộ dữ liệu & ảnh từ website gốc

Website gốc dùng WooCommerce (`tosixdecor.com.vn`). Để import **945 sản phẩm** kèm **ảnh đúng từng mã**, chạy:

```powershell
cd D:\Projects1\betosix\src\Tosix.Api
dotnet run -- --import-live
```

Lệnh này sẽ:
- Lấy danh mục + sản phẩm từ API WooCommerce
- Ghép ảnh với file đã tải trong `wwwroot/uploads/seed/` (ưu tiên ảnh gốc, không dùng thumbnail)
- Tải thêm ảnh thiếu vào `wwwroot/uploads/imported/`
- Cập nhật banner, feedback, đánh giá

**Lưu ý:** Dừng API (Visual Studio / `dotnet run`) trước khi build nếu bị lỗi file bị khóa.

## Ảnh local

Ảnh tải từ website gốc nằm tại:

```
src/Tosix.Api/wwwroot/uploads/seed/
```

Ảnh admin upload mới sẽ lưu tại `wwwroot/uploads/{folder}/`.

Truy cập ảnh qua URL: `http://localhost:5208/uploads/...`

## API chính

| Nhóm | Endpoint |
|------|----------|
| Public | `GET /api/public/home`, `/categories`, `/products`, `/settings` |
| Auth | `POST /api/auth/login`, `GET /api/auth/me` |
| Admin | `GET/POST/PUT/DELETE /api/admin/categories`, `/products`, `/banners`, `/feedback`, `/reviews` |
| Upload | `POST /api/admin/upload?folder=products` |

## Cấu hình DB

Copy `src/Tosix.Api/appsettings.example.json` thành `appsettings.json` (nếu chưa có) và sửa connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=data-tosix;Username=postgres;Password=YOUR_PASSWORD"
}
```

**Lưu ý:** Ảnh upload/seed không nằm trong repo Git — chạy `dotnet run -- --import-live` để tải dữ liệu & ảnh từ website gốc.
