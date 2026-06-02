namespace Tosix.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresInSeconds,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public sealed record UpdateProfileRequest(string FullName, string Email);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record CreateAdminUserRequest(string Email, string FullName, string Password);

public sealed record AdminResetPasswordRequest(string NewPassword);

public sealed record UpdateAdminUserRequest(string FullName, string Email);

public sealed record SiteSettingDto(
    Guid Id,
    string CompanyName,
    string TaxCode,
    string Address,
    string Email,
    string PhonePrimary,
    string? PhoneSecondary,
    string? FacebookUrl,
    string? ZaloUrl,
    string? ZaloQrImagePath,
    string? SiteTitle,
    string? SiteTagline,
    string? HeroEyebrow,
    string? LogoSubtitle,
    string? Trust1Title,
    string? Trust1Text,
    string? Trust2Title,
    string? Trust2Text,
    string? Trust3Title,
    string? Trust3Text);

public sealed record SiteSettingUpdateDto(
    string CompanyName,
    string TaxCode,
    string Address,
    string Email,
    string PhonePrimary,
    string? PhoneSecondary,
    string? FacebookUrl,
    string? ZaloUrl,
    string? ZaloQrImagePath,
    string? SiteTitle,
    string? SiteTagline,
    string? HeroEyebrow,
    string? LogoSubtitle,
    string? Trust1Title,
    string? Trust1Text,
    string? Trust2Title,
    string? Trust2Text,
    string? Trust3Title,
    string? Trust3Text);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? ImagePath,
    int SortOrder,
    bool IsActive,
    int ProductCount);

public sealed record CategoryCreateDto(string Name, string Slug, string? ImagePath, int SortOrder, bool IsActive);
public sealed record CategoryUpdateDto(string Name, string Slug, string? ImagePath, int SortOrder, bool IsActive);

public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Code,
    string Name,
    decimal Price,
    decimal PriceMax,
    string? ImagePath,
    bool IsNew,
    bool IsFeatured,
    bool IsInStock,
    bool IsOrder,
    bool IsUpdating,
    int SortOrder,
    bool IsActive,
    int ImageCount = 0);

public sealed record ProductCreateDto(
    Guid CategoryId,
    string Code,
    string Name,
    decimal Price,
    decimal PriceMax,
    IReadOnlyList<string>? ImagePaths,
    bool IsNew,
    bool IsFeatured,
    bool IsInStock,
    bool IsOrder,
    bool IsUpdating,
    int SortOrder,
    bool IsActive);

public sealed record ProductUpdateDto(
    Guid CategoryId,
    string Code,
    string Name,
    decimal Price,
    decimal PriceMax,
    IReadOnlyList<string>? ImagePaths,
    bool IsNew,
    bool IsFeatured,
    bool IsInStock,
    bool IsOrder,
    bool IsUpdating,
    int SortOrder,
    bool IsActive);

public sealed record AdminProductDetailDto(
    ProductDto Product,
    IReadOnlyList<string> ImagePaths);

public sealed record ProductDetailDto(
    ProductDto Product,
    IReadOnlyList<string> ImagePaths,
    IReadOnlyList<ProductDto> RelatedProducts);

public sealed record BannerDto(Guid Id, string ImagePath, string? LinkUrl, int SortOrder, bool IsActive);
public sealed record BannerCreateDto(string ImagePath, string? LinkUrl, int SortOrder, bool IsActive);
public sealed record BannerUpdateDto(string ImagePath, string? LinkUrl, int SortOrder, bool IsActive);

public sealed record FeedbackImageDto(Guid Id, string ImagePath, string? Caption, int SortOrder, bool IsActive);
public sealed record FeedbackImageCreateDto(string ImagePath, string? Caption, int SortOrder, bool IsActive);
public sealed record FeedbackImageUpdateDto(string ImagePath, string? Caption, int SortOrder, bool IsActive);

public sealed record CustomerReviewDto(Guid Id, string ImagePath, int SortOrder, bool IsActive);
public sealed record CustomerReviewCreateDto(string ImagePath, int SortOrder, bool IsActive);
public sealed record CustomerReviewUpdateDto(string ImagePath, int SortOrder, bool IsActive);

public sealed record HomePageDto(
    SiteSettingDto Settings,
    IReadOnlyList<BannerDto> Banners,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<ProductDto> NewProducts,
    IReadOnlyList<FeedbackImageDto> FeedbackImages,
    IReadOnlyList<CustomerReviewDto> CustomerReviews);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record UploadResponse(string Path, string Url);
