using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tosix.Api.Contracts;
using Tosix.Api.Data;
using Tosix.Api.Options;
using Tosix.Api.Services;

namespace Tosix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(AppDbContext db, IJwtTokenService jwt, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken cancellationToken)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Unauthorized();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        var token = jwt.CreateAccessToken(user, roles);
        var expiresMinutes = jwtOptions.Value.ExpiresMinutes;

        return Ok(new LoginResponse(
            token,
            expiresMinutes * 60,
            new UserSummaryDto(user.Id, user.Email, user.FullName, roles)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Unauthorized();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        return Ok(new UserSummaryDto(user.Id, user.Email, user.FullName, roles));
    }

    [HttpPut("me/profile")]
    [Authorize]
    public async Task<ActionResult<UserSummaryDto>> UpdateProfile([FromBody] UpdateProfileRequest body, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var email = body.Email.Trim().ToLowerInvariant();
        var fullName = body.FullName.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { detail = "Email không được để trống." });
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { detail = "Họ tên không được để trống." });

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();

        if (await db.Users.AnyAsync(u => u.Id != userId && u.Email.ToLower() == email, cancellationToken))
            return BadRequest(new { detail = "Email đã được sử dụng." });

        user.Email = email;
        user.FullName = fullName;
        await db.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        return Ok(new UserSummaryDto(user.Id, user.Email, user.FullName, roles));
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest body, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        if (body.NewPassword.Length < 8)
            return BadRequest(new { detail = "Mật khẩu mới phải có ít nhất 8 ký tự." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(body.CurrentPassword, user.PasswordHash))
            return BadRequest(new { detail = "Mật khẩu hiện tại không đúng." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
