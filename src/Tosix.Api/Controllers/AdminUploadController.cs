using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tosix.Api.Contracts;
using Tosix.Api.Security;
using Tosix.Api.Services;

namespace Tosix.Api.Controllers;

[ApiController]
[Route("api/admin/upload")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminUploadController(ILocalFileStorage storage) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadResponse>> Upload(
        IFormFile file,
        [FromQuery] string folder = "general",
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            return BadRequest("Thiếu file.");

        var path = await storage.SaveAsync(file, folder, cancellationToken);
        return Ok(new UploadResponse(path, path));
    }
}
