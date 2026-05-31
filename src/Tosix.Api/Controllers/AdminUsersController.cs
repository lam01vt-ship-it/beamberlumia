using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tosix.Api.Contracts;
using Tosix.Api.Data;
using Tosix.Api.Entities;
using Tosix.Api.Security;

namespace Tosix.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminUsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(ToSummary).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> Create([FromBody] CreateAdminUserRequest body, CancellationToken cancellationToken)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var fullName = body.FullName.Trim();
        var password = body.Password;

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { detail = "Email không được để trống." });
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { detail = "Họ tên không được để trống." });
        if (!IsValidPassword(password))
            return BadRequest(new { detail = "Mật khẩu phải có ít nhất 8 ký tự." });

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
            return BadRequest(new { detail = "Email đã được sử dụng." });

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == TosixRoles.Admin, cancellationToken);
        if (adminRole is null)
            return BadRequest(new { detail = "Không tìm thấy vai trò Admin." });

        var user = new TosixUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };
        user.UserRoles.Add(new UserRole { User = user, Role = adminRole });
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToSummary(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, [FromBody] UpdateAdminUserRequest body, CancellationToken cancellationToken)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var fullName = body.FullName.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { detail = "Email không được để trống." });
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { detail = "Họ tên không được để trống." });

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        if (await db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email, cancellationToken))
            return BadRequest(new { detail = "Email đã được sử dụng." });

        user.Email = email;
        user.FullName = fullName;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToSummary(user));
    }

    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest body, CancellationToken cancellationToken)
    {
        if (!IsValidPassword(body.NewPassword))
            return BadRequest(new { detail = "Mật khẩu phải có ít nhất 8 ký tự." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();
        if (currentUserId == id)
            return BadRequest(new { detail = "Không thể xóa tài khoản đang đăng nhập." });

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == TosixRoles.Admin);
        if (isAdmin)
        {
            var adminCount = await db.UserRoles
                .Where(ur => ur.Role.Name == TosixRoles.Admin)
                .Select(ur => ur.UserId)
                .Distinct()
                .CountAsync(cancellationToken);
            if (adminCount <= 1)
                return BadRequest(new { detail = "Không thể xóa admin cuối cùng." });
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static UserSummaryDto ToSummary(TosixUser user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        return new UserSummaryDto(user.Id, user.Email, user.FullName, roles);
    }

    private static bool IsValidPassword(string password) => password.Length >= 8;
}
