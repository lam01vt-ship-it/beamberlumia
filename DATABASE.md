# 📚 Tổng hợp cơ sở dữ liệu hệ thống Tosix / AmberLumia

> Tài liệu này mô tả toàn bộ cấu trúc database của hệ thống để bạn tìm hiểu, và kèm
> script SQL tạo bảng **chạy trực tiếp được trên Supabase** (vì Supabase = PostgreSQL).

---

## 1. Tổng quan

| Hạng mục | Giá trị |
|---|---|
| Hệ quản trị CSDL | **PostgreSQL** (Supabase cũng là PostgreSQL) |
| Tên database | `data-tosix` |
| Cách định nghĩa schema | **EF Core** (C# entities) — file `Entities/Entities.cs` + `Data/AppDbContext.cs` |
| Cách tạo bảng thực tế | `db.Database.EnsureCreatedAsync()` + một khối **SQL thô** trong `Program.cs` |
| ORM | Entity Framework Core 8 + Npgsql (driver PostgreSQL) |
| Kết nối | chuỗi `ConnectionStrings:DefaultConnection` trong `appsettings.json` |

### Schema được tạo ra sao? (rất quan trọng để hiểu)

Hệ thống **KHÔNG dùng EF Migrations**. Thay vào đó:

1. **`EnsureCreatedAsync()`** — nếu database trống, EF tạo **toàn bộ bảng** theo đúng model C#.
   Nhưng nó **không cập nhật** database đã tồn tại (không thêm cột/bảng mới).
2. **Khối SQL thô** (`ExecuteSqlRawAsync` trong `Program.cs`) — bù cho hạn chế trên: mỗi khi
   thêm cột/bảng mới vào code, ta thêm câu `ALTER TABLE ... ADD COLUMN IF NOT EXISTS ...`
   để **database cũ trên VPS tự nâng cấp** khi khởi động lại API.
3. **`DbInitializer.SeedAsync()`** — nạp dữ liệu mẫu (tài khoản admin, settings, sản phẩm…)
   nếu bảng còn trống.

➡️ Vì vậy khi deploy code mới có thêm cột, **chỉ cần restart API**, schema tự cập nhật.

---

## 2. Danh sách bảng (10 bảng)

| # | Bảng | Vai trò |
|---|---|---|
| 1 | `Roles` | Vai trò người dùng (Admin…) |
| 2 | `Users` | Tài khoản quản trị |
| 3 | `UserRoles` | Bảng nối User ↔ Role (nhiều-nhiều) |
| 4 | `ProductCategories` | Danh mục sản phẩm |
| 5 | `Products` | Sản phẩm |
| 6 | `ProductImages` | Ảnh phụ của sản phẩm (1 sản phẩm nhiều ảnh) |
| 7 | `Banners` | Banner trang chủ |
| 8 | `FeedbackImages` | Ảnh feedback khách hàng |
| 9 | `CustomerReviews` | Ảnh đánh giá khách hàng |
| 10 | `SiteSettings` | Cấu hình website (1 dòng duy nhất) |

### Sơ đồ quan hệ

```
Roles ──< UserRoles >── Users          (nhiều-nhiều qua UserRoles)

ProductCategories ──< Products ──< ProductImages
        (1 danh mục có nhiều sản phẩm; 1 sản phẩm có nhiều ảnh)

Banners,  FeedbackImages,  CustomerReviews,  SiteSettings   (độc lập, không khóa ngoại)
```

---

## 3. Chi tiết từng bảng

> Quy ước kiểu: `uuid` = khóa GUID; `text` = chuỗi; `integer` = số nguyên;
> `boolean` = true/false; `numeric(18,0)` = số tiền (không phần thập phân);
> `timestamptz` = ngày giờ có múi giờ.

### 3.1 `Roles`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| Name | text | NOT NULL, **UNIQUE** |

### 3.2 `Users` (entity `TosixUser`)
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| Email | text | NOT NULL, **UNIQUE** |
| PasswordHash | text | NOT NULL (mã hóa BCrypt) |
| FullName | text | NOT NULL |

### 3.3 `UserRoles` (bảng nối)
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| UserId | uuid | **PK** (ghép), FK → `Users.Id` |
| RoleId | uuid | **PK** (ghép), FK → `Roles.Id` |

> Khóa chính là **cặp (UserId, RoleId)**.

### 3.4 `ProductCategories`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| Name | text | NOT NULL |
| Slug | text | NOT NULL, **UNIQUE** (dùng cho URL) |
| ImagePath | text | NULL |
| SortOrder | integer | NOT NULL (thứ tự hiển thị) |
| IsActive | boolean | NOT NULL, default `true` |

### 3.5 `Products`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| CategoryId | uuid | NOT NULL, FK → `ProductCategories.Id` |
| Code | text | NOT NULL, **UNIQUE** (mã SP) |
| Name | text | NOT NULL |
| Price | numeric(18,0) | NOT NULL (giá / giá thấp nhất) |
| PriceMax | numeric(18,0) | NOT NULL (giá cao nhất nếu là khoảng giá) |
| ImagePath | text | NULL (ảnh đại diện) |
| IsNew | boolean | NOT NULL (hàng mới) |
| IsFeatured | boolean | NOT NULL (nổi bật) |
| IsInStock | boolean | NOT NULL, default `true` (còn hàng) |
| IsOrder | boolean | NOT NULL (hàng đặt) |
| IsUpdating | boolean | NOT NULL (đang cập nhật) |
| SortOrder | integer | NOT NULL |
| CreatedAt | timestamptz | NOT NULL, default `now()` |
| IsActive | boolean | NOT NULL, default `true` |

> ⚠️ **Quy tắc trạng thái** (3 cờ loại trừ nhau — chỉ 1 cờ = true):
> `IsInStock` (còn hàng) **HOẶC** `IsOrder` (hàng đặt) **HOẶC** `IsUpdating` (đang cập nhật).
> Logic này được ép trong `Program.cs` bằng các câu `UPDATE` chuẩn hóa dữ liệu.

### 3.6 `ProductImages`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| ProductId | uuid | NOT NULL, FK → `Products.Id` **ON DELETE CASCADE** |
| ImagePath | text | NOT NULL |
| SortOrder | integer | NOT NULL |

> Có **index** trên `(ProductId, SortOrder)`. Xóa sản phẩm → ảnh tự xóa theo (cascade).

### 3.7 `Banners`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| ImagePath | text | NOT NULL |
| LinkUrl | text | NULL |
| SortOrder | integer | NOT NULL |
| IsActive | boolean | NOT NULL, default `true` |

### 3.8 `FeedbackImages`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| ImagePath | text | NOT NULL |
| Caption | text | NULL |
| SortOrder | integer | NOT NULL |
| IsActive | boolean | NOT NULL, default `true` |

### 3.9 `CustomerReviews`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | **PK** |
| ImagePath | text | NOT NULL |
| SortOrder | integer | NOT NULL |
| IsActive | boolean | NOT NULL, default `true` |

### 3.10 `SiteSettings` (chỉ có **1 dòng**)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | uuid | **PK** |
| CompanyName | text | NOT NULL — tên công ty |
| TaxCode | text | NOT NULL — mã số thuế |
| Address | text | NOT NULL — địa chỉ |
| Email | text | NOT NULL |
| PhonePrimary | text | NOT NULL — SĐT chính |
| PhoneSecondary | text | NULL — SĐT phụ |
| FacebookUrl | text | NULL |
| ZaloUrl | text | NULL |
| ZaloQrImagePath | text | NULL — ảnh QR Zalo |
| SiteTitle | text | NULL — tên thương hiệu |
| SiteTagline | text | NULL — slogan |
| HeroEyebrow | text | NULL — dòng nhỏ trên banner |
| LogoSubtitle | text | NULL — dòng phụ logo |
| Trust1Title…Trust3Text | text | NULL — 3 cột giới thiệu trang chủ (tiêu đề + mô tả) |
| **PolicyContent** | text | NULL — **nội dung trang Chính sách** (cú pháp `##` tiêu đề, `-` gạch đầu dòng) |

---

## 4. Bảng nào phục vụ chức năng nào (schema chức năng)

| Controller / Chức năng | Đọc/Ghi bảng |
|---|---|
| `AuthController` — đăng nhập, hồ sơ | `Users`, `Roles`, `UserRoles` |
| `AdminUsersController` — quản lý tài khoản | `Users`, `UserRoles` |
| `AdminCategoriesController` — danh mục | `ProductCategories` |
| `AdminProductsController` — sản phẩm | `Products`, `ProductImages`, `ProductCategories` |
| `AdminContentController` — nội dung web | `SiteSettings`, `Banners`, `FeedbackImages`, `CustomerReviews` |
| `AdminUploadController` — tải ảnh | (không ghi DB — lưu file vào `wwwroot/uploads`) |
| `PublicController` — trang công khai | đọc `Products`, `ProductCategories`, `Banners`, `FeedbackImages`, `CustomerReviews`, `SiteSettings` |

---

## 5. Script tạo toàn bộ bảng trên Supabase

> Dán nguyên khối này vào **Supabase → SQL Editor → New query → Run**.
> Script idempotent (chạy lại nhiều lần không lỗi nhờ `IF NOT EXISTS`).

```sql
-- 1. Roles
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id"   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Roles_Name" ON "Roles" ("Name");

-- 2. Users
CREATE TABLE IF NOT EXISTS "Users" (
    "Id"           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email"        text NOT NULL,
    "PasswordHash" text NOT NULL,
    "FullName"     text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");

-- 3. UserRoles (bảng nối, khóa chính ghép)
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "UserId" uuid NOT NULL REFERENCES "Users" ("Id") ON DELETE CASCADE,
    "RoleId" uuid NOT NULL REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

-- 4. ProductCategories
CREATE TABLE IF NOT EXISTS "ProductCategories" (
    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name"      text NOT NULL,
    "Slug"      text NOT NULL,
    "ImagePath" text,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive"  boolean NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductCategories_Slug" ON "ProductCategories" ("Slug");

-- 5. Products
CREATE TABLE IF NOT EXISTS "Products" (
    "Id"         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "CategoryId" uuid NOT NULL REFERENCES "ProductCategories" ("Id"),
    "Code"       text NOT NULL,
    "Name"       text NOT NULL,
    "Price"      numeric(18,0) NOT NULL DEFAULT 0,
    "PriceMax"   numeric(18,0) NOT NULL DEFAULT 0,
    "ImagePath"  text,
    "IsNew"      boolean NOT NULL DEFAULT false,
    "IsFeatured" boolean NOT NULL DEFAULT false,
    "IsInStock"  boolean NOT NULL DEFAULT true,
    "IsOrder"    boolean NOT NULL DEFAULT false,
    "IsUpdating" boolean NOT NULL DEFAULT false,
    "SortOrder"  integer NOT NULL DEFAULT 0,
    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
    "IsActive"   boolean NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Products_Code" ON "Products" ("Code");
CREATE INDEX IF NOT EXISTS "IX_Products_CategoryId" ON "Products" ("CategoryId");

-- 6. ProductImages
CREATE TABLE IF NOT EXISTS "ProductImages" (
    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProductId" uuid NOT NULL REFERENCES "Products" ("Id") ON DELETE CASCADE,
    "ImagePath" text NOT NULL,
    "SortOrder" integer NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS "IX_ProductImages_ProductId_SortOrder"
    ON "ProductImages" ("ProductId", "SortOrder");

-- 7. Banners
CREATE TABLE IF NOT EXISTS "Banners" (
    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ImagePath" text NOT NULL,
    "LinkUrl"   text,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive"  boolean NOT NULL DEFAULT true
);

-- 8. FeedbackImages
CREATE TABLE IF NOT EXISTS "FeedbackImages" (
    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ImagePath" text NOT NULL,
    "Caption"   text,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive"  boolean NOT NULL DEFAULT true
);

-- 9. CustomerReviews
CREATE TABLE IF NOT EXISTS "CustomerReviews" (
    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ImagePath" text NOT NULL,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive"  boolean NOT NULL DEFAULT true
);

-- 10. SiteSettings (chỉ 1 dòng)
CREATE TABLE IF NOT EXISTS "SiteSettings" (
    "Id"              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "CompanyName"     text NOT NULL DEFAULT '',
    "TaxCode"         text NOT NULL DEFAULT '',
    "Address"         text NOT NULL DEFAULT '',
    "Email"           text NOT NULL DEFAULT '',
    "PhonePrimary"    text NOT NULL DEFAULT '',
    "PhoneSecondary"  text,
    "FacebookUrl"     text,
    "ZaloUrl"         text,
    "ZaloQrImagePath" text,
    "SiteTitle"       text,
    "SiteTagline"     text,
    "HeroEyebrow"     text,
    "LogoSubtitle"    text,
    "Trust1Title"     text,
    "Trust1Text"      text,
    "Trust2Title"     text,
    "Trust2Text"      text,
    "Trust3Title"     text,
    "Trust3Text"      text,
    "PolicyContent"   text
);
```

> **Khác biệt nhỏ so với DB hiện tại:** ở đây tôi thêm `DEFAULT gen_random_uuid()` cho `Id`
> để bạn có thể `INSERT` thử trực tiếp trên Supabase. Trong hệ thống .NET, GUID do **ứng dụng
> sinh ra** (không dùng default của DB) — cả hai cách đều chạy được.

---

## 6. Cách tạo bảng trên Supabase (2 cách)

### Cách A — SQL Editor (nhanh nhất, khuyên dùng)
1. Vào **app.supabase.com** → chọn project.
2. Menu trái → **SQL Editor** → **New query**.
3. Dán script mục 5 vào → bấm **Run**.
4. Sang **Table Editor** để xem 10 bảng vừa tạo.

### Cách B — Table Editor (giao diện, tạo từng bảng)
1. Menu trái → **Table Editor** → **New table**.
2. Đặt tên bảng (vd `Products`), thêm từng cột theo bảng mô tả ở mục 3.
3. Chọn kiểu cột tương ứng (`uuid`, `text`, `int8`, `bool`, `numeric`, `timestamptz`).
4. Đặt Primary Key, và thêm **Foreign Key** ở các cột `...Id`.

> 💡 Trên Supabase, kiểu hiển thị có thể khác tên: `integer` ↔ `int4`, `bigint` ↔ `int8`,
> `boolean` ↔ `bool`, `timestamptz` = "timestamp with time zone".

---

## 7. Lưu ý khi chuyển hệ thống .NET sang dùng Supabase

Nếu sau này bạn muốn API .NET trỏ thẳng vào Supabase (thay vì Postgres tự host trên VPS):

1. Vào Supabase → **Project Settings → Database → Connection string** → chọn **.NET / Npgsql**.
2. Thay `ConnectionStrings:DefaultConnection` trong `appsettings.json` bằng chuỗi đó
   (nhớ điền mật khẩu database).
3. Bật `sslmode=require` trong chuỗi kết nối (Supabase bắt buộc SSL).
4. **Tắt Row Level Security (RLS)** cho các bảng này, *hoặc* viết policy phù hợp — vì API .NET
   kết nối bằng user database trực tiếp, không qua lớp Auth của Supabase. Nếu để RLS bật mà
   không có policy, mọi truy vấn qua PostgREST/anon key sẽ bị chặn (kết nối Npgsql trực tiếp
   thì không bị ảnh hưởng).
5. Giữ nguyên cơ chế `EnsureCreated` + SQL thô — nó sẽ tự tạo bảng trên Supabase khi chạy lần đầu.

---

## 8. Tài liệu nguồn (đọc thêm trong code)

| Nội dung | File |
|---|---|
| Định nghĩa entity (cột) | `src/Tosix.Api/Entities/Entities.cs` |
| Khóa chính, index, khóa ngoại | `src/Tosix.Api/Data/AppDbContext.cs` → `OnModelCreating` |
| Tạo bảng + nâng cấp schema (SQL thô) | `src/Tosix.Api/Program.cs` |
| Dữ liệu mẫu (seed) | `src/Tosix.Api/Data/DbInitializer.cs` |
| DTO trả về cho FE | `src/Tosix.Api/Contracts/Dtos.cs` |
| Map entity → DTO | `src/Tosix.Api/Data/Mapping.cs` |
| Chuỗi kết nối | `src/Tosix.Api/appsettings.json` |
