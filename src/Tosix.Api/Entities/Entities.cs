namespace Tosix.Api.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class TosixUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public Guid UserId { get; set; }
    public TosixUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class ProductCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImagePath { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsInStock { get; set; } = true;
    public bool IsOrder { get; set; }
    public bool IsUpdating { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class Banner
{
    public Guid Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FeedbackImage
{
    public Guid Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CustomerReview
{
    public Guid Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SiteSetting
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhonePrimary { get; set; } = string.Empty;
    public string? PhoneSecondary { get; set; }
    public string? FacebookUrl { get; set; }
    public string? ZaloUrl { get; set; }
    public string? SiteTitle { get; set; }
    public string? SiteTagline { get; set; }
    public string? HeroEyebrow { get; set; }
    public string? LogoSubtitle { get; set; }
    public string? Trust1Title { get; set; }
    public string? Trust1Text { get; set; }
    public string? Trust2Title { get; set; }
    public string? Trust2Text { get; set; }
    public string? Trust3Title { get; set; }
    public string? Trust3Text { get; set; }
}
