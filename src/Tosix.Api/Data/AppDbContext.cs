using Microsoft.EntityFrameworkCore;
using Tosix.Api.Entities;

namespace Tosix.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<TosixUser> Users => Set<TosixUser>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<FeedbackImage> FeedbackImages => Set<FeedbackImage>();
    public DbSet<CustomerReview> CustomerReviews => Set<CustomerReview>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TosixUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<ProductCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Price).HasPrecision(18, 0);
            e.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProductId, x.SortOrder });
            e.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Banner>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<FeedbackImage>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<CustomerReview>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<SiteSetting>(e => e.HasKey(x => x.Id));
    }
}
