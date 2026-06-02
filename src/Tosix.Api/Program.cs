using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tosix.Api.Data;
using Tosix.Api.Options;
using Tosix.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");
    options.UseNpgsql(cs);
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ILocalFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<SeedImageResolver>();
builder.Services.AddScoped<LiveSiteImporter>();
builder.Services.AddHttpClient(nameof(LiveSiteImporter));

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(o =>
{
    o.AddPolicy("fe", p =>
    {
        p.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Tosix Decor", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Ví dụ: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (args.Contains("--import-live", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<LiveSiteImporter>();
    await importer.ImportAsync();
    return;
}

if (args.Contains("--import-galleries", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<LiveSiteImporter>();
    await importer.ImportGalleriesAsync();
    return;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ProductImages" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "ProductId" uuid NOT NULL,
            "ImagePath" text NOT NULL,
            "SortOrder" integer NOT NULL,
            CONSTRAINT "FK_ProductImages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_ProductImages_ProductId" ON "ProductImages" ("ProductId");
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "ZaloUrl" text;
        UPDATE "SiteSettings" SET "ZaloUrl" = 'https://zalo.me/84965975366' WHERE "ZaloUrl" IS NULL OR "ZaloUrl" = '';
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "ZaloQrImagePath" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "HeroEyebrow" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "LogoSubtitle" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust1Title" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust1Text" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust2Title" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust2Text" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust3Title" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "Trust3Text" text;
        ALTER TABLE "SiteSettings" ADD COLUMN IF NOT EXISTS "PolicyContent" text;
        UPDATE "SiteSettings" SET "SiteTitle" = 'AmberLumia' WHERE "SiteTitle" IS NULL OR "SiteTitle" = '' OR "SiteTitle" ILIKE 'tosix decor';
        UPDATE "SiteSettings" SET "HeroEyebrow" = 'AmberLumia — Đèn & nội thất' WHERE "HeroEyebrow" IS NULL OR "HeroEyebrow" = '';
        UPDATE "SiteSettings" SET "LogoSubtitle" = 'ĐÈN & NỘI THẤT CAO CẤP' WHERE "LogoSubtitle" IS NULL OR "LogoSubtitle" = '';
        UPDATE "SiteSettings" SET "Trust1Title" = 'Đa dạng mẫu mã' WHERE "Trust1Title" IS NULL OR "Trust1Title" = '';
        UPDATE "SiteSettings" SET "Trust1Text" = 'Đèn trang trí & nội thất thời thượng' WHERE "Trust1Text" IS NULL OR "Trust1Text" = '';
        UPDATE "SiteSettings" SET "Trust2Title" = 'Tư vấn tận tâm' WHERE "Trust2Title" IS NULL OR "Trust2Title" = '';
        UPDATE "SiteSettings" SET "Trust2Text" = 'Hỗ trợ chọn sản phẩm phù hợp' WHERE "Trust2Text" IS NULL OR "Trust2Text" = '';
        UPDATE "SiteSettings" SET "Trust3Title" = 'Khách hàng tin tưởng' WHERE "Trust3Title" IS NULL OR "Trust3Title" = '';
        UPDATE "SiteSettings" SET "Trust3Text" = 'Feedback & đánh giá thực tế' WHERE "Trust3Text" IS NULL OR "Trust3Text" = '';
        ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "IsInStock" boolean NOT NULL DEFAULT true;
        ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "IsOrder" boolean NOT NULL DEFAULT false;
        UPDATE "Products" SET "IsInStock" = true WHERE "IsInStock" IS NULL;
        UPDATE "Products" SET "IsOrder" = false WHERE "IsOrder" IS NULL;
        UPDATE "Products" SET "IsOrder" = false WHERE "IsInStock" = true AND "IsOrder" = true;
        UPDATE "Products" SET "IsInStock" = true WHERE "IsInStock" = false AND "IsOrder" = false;
        ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "IsUpdating" boolean NOT NULL DEFAULT false;
        UPDATE "Products" SET "IsUpdating" = false WHERE "IsUpdating" IS NULL;
        UPDATE "Products" SET "IsOrder" = false, "IsUpdating" = false WHERE "IsInStock" = true AND ("IsOrder" = true OR "IsUpdating" = true);
        UPDATE "Products" SET "IsInStock" = false, "IsUpdating" = false WHERE "IsOrder" = true;
        UPDATE "Products" SET "IsInStock" = false, "IsOrder" = false WHERE "IsUpdating" = true;
        UPDATE "Products" SET "IsInStock" = true, "IsOrder" = false, "IsUpdating" = false
        WHERE "IsInStock" = false AND "IsOrder" = false AND "IsUpdating" = false;
        ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "PriceMax" numeric(18,0) NOT NULL DEFAULT 0;
        ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT now();
        """);
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        UPDATE "SiteSettings" SET "PolicyContent" = {DbInitializer.DefaultPolicyContent}
        WHERE "PolicyContent" IS NULL OR "PolicyContent" = '';
        """);
    await DbInitializer.SeedAsync(db, env);
}


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseStaticFiles();
app.UseCors("fe");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
