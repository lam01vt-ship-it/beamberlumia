using Tosix.Api.Entities;
using Tosix.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Tosix.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IWebHostEnvironment env, CancellationToken cancellationToken = default)
    {
        if (!await db.Roles.AnyAsync(cancellationToken))
        {
            var roleAdmin = new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Name = TosixRoles.Admin
            };
            db.Roles.Add(roleAdmin);

            const string devPassword = "Admin123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(devPassword);
            var admin = new TosixUser
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                Email = "admin@tosix.local",
                PasswordHash = hash,
                FullName = "Tosix Admin"
            };
            admin.UserRoles.Add(new UserRole { User = admin, Role = roleAdmin });
            db.Users.Add(admin);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.SiteSettings.AnyAsync(cancellationToken))
            return;

        var seedDir = Path.Combine(env.WebRootPath, "uploads", "seed");
        var seedFiles = Directory.Exists(seedDir)
            ? Directory.GetFiles(seedDir).Where(f => IsImage(f)).OrderBy(f => f).ToList()
            : [];

        string Pick(int index) => seedFiles.Count == 0
            ? "/uploads/seed/placeholder.jpg"
            : $"/uploads/seed/{Path.GetFileName(seedFiles[index % seedFiles.Count])}";

        db.SiteSettings.Add(new SiteSetting
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CompanyName = "CÔNG TY TNHH THIẾT KẾ VÀ TRANG TRÍ NỘI THẤT HÙNG LÂM",
            TaxCode = "0110778120",
            Address = "Số 10 ngõ 87 Phố Thiên Hiền, Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
            Email = "tosixdecor@gmail.com",
            PhonePrimary = "0965975366",
            PhoneSecondary = "0973840296",
            FacebookUrl = "https://www.facebook.com/tosixdecor",
            ZaloUrl = "https://zalo.me/84965975366",
            SiteTitle = "AmberLumia",
            SiteTagline = "Tổng phân phối đèn trang trí, đồ nội thất",
            HeroEyebrow = "AmberLumia — Đèn & nội thất",
            LogoSubtitle = "ĐÈN & NỘI THẤT CAO CẤP",
            Trust1Title = "Đa dạng mẫu mã",
            Trust1Text = "Đèn trang trí & nội thất thời thượng",
            Trust2Title = "Tư vấn tận tâm",
            Trust2Text = "Hỗ trợ chọn sản phẩm phù hợp",
            Trust3Title = "Khách hàng tin tưởng",
            Trust3Text = "Feedback & đánh giá thực tế",
        });

        var categories = new[]
        {
            new ProductCategory { Id = Guid.NewGuid(), Name = "Đèn chùm", Slug = "den-chum", ImagePath = Pick(0), SortOrder = 1 },
            new ProductCategory { Id = Guid.NewGuid(), Name = "Đèn thả", Slug = "den-tha", ImagePath = Pick(1), SortOrder = 2 },
            new ProductCategory { Id = Guid.NewGuid(), Name = "Đèn tường", Slug = "den-tuong", ImagePath = Pick(2), SortOrder = 3 },
            new ProductCategory { Id = Guid.NewGuid(), Name = "Đèn bàn", Slug = "den-ban", ImagePath = Pick(3), SortOrder = 4 },
            new ProductCategory { Id = Guid.NewGuid(), Name = "Đèn sàn", Slug = "den-san", ImagePath = Pick(4), SortOrder = 5 },
            new ProductCategory { Id = Guid.NewGuid(), Name = "Decor trang trí", Slug = "decor", ImagePath = Pick(5), SortOrder = 6 },
        };
        db.ProductCategories.AddRange(categories);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), CategoryId = categories[0].Id, Code = "DT-22", Name = "DT-22", Price = 1_700_000, ImagePath = Pick(10), IsNew = true, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[1].Id, Code = "DT-21", Name = "DT-21", Price = 3_500_000, ImagePath = Pick(11), IsNew = true, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[1].Id, Code = "DT-20", Name = "DT-20", Price = 3_000_000, ImagePath = Pick(12), IsNew = true, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[2].Id, Code = "DTU-30", Name = "DTU-30", Price = 1_200_000, ImagePath = Pick(13), SortOrder = 4 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[2].Id, Code = "DTU-29", Name = "DTU-29", Price = 1_100_000, ImagePath = Pick(14), IsNew = true, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[3].Id, Code = "BB02", Name = "BB02", Price = 350_000, ImagePath = Pick(15), IsNew = true, SortOrder = 6 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[3].Id, Code = "YQBD01", Name = "YQBD01", Price = 1_000_000, ImagePath = Pick(16), SortOrder = 7 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[4].Id, Code = "PH3", Name = "PH3", Price = 800_000, ImagePath = Pick(17), SortOrder = 8 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[0].Id, Code = "BD001", Name = "BD001", Price = 950_000, ImagePath = Pick(18), IsNew = true, SortOrder = 9 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[0].Id, Code = "LDT001", Name = "LDT001", Price = 1_950_000, ImagePath = Pick(19), IsNew = true, SortOrder = 10 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[1].Id, Code = "TDB001", Name = "TDB001", Price = 1_850_000, ImagePath = Pick(20), IsNew = true, SortOrder = 11 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[1].Id, Code = "DT-12", Name = "DT-12", Price = 2_950_000, ImagePath = Pick(21), IsNew = true, SortOrder = 12 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[2].Id, Code = "DT-08", Name = "DT-08", Price = 2_000_000, ImagePath = Pick(22), IsNew = true, SortOrder = 13 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[3].Id, Code = "TD001", Name = "TD001", Price = 350_000, ImagePath = Pick(23), IsNew = true, SortOrder = 14 },
            new() { Id = Guid.NewGuid(), CategoryId = categories[4].Id, Code = "TDX001", Name = "TDX001", Price = 370_000, ImagePath = Pick(24), IsNew = true, SortOrder = 15 },
        };
        db.Products.AddRange(products);

        db.Banners.AddRange(
            new Banner { Id = Guid.NewGuid(), ImagePath = Pick(30), SortOrder = 1, IsActive = true },
            new Banner { Id = Guid.NewGuid(), ImagePath = Pick(31), SortOrder = 2, IsActive = true },
            new Banner { Id = Guid.NewGuid(), ImagePath = Pick(32), SortOrder = 3, IsActive = true }
        );

        for (var i = 0; i < 8; i++)
        {
            db.FeedbackImages.Add(new FeedbackImage
            {
                Id = Guid.NewGuid(),
                ImagePath = Pick(40 + i),
                SortOrder = i + 1,
                IsActive = true
            });
        }

        for (var i = 0; i < 8; i++)
        {
            db.CustomerReviews.Add(new CustomerReview
            {
                Id = Guid.NewGuid(),
                ImagePath = Pick(50 + i),
                SortOrder = i + 1,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }
}
