using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivateInsta.Api.Data;
using PrivateInsta.Api.DTOs;
using PrivateInsta.Api.Services;

namespace PrivateInsta.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, BlobStorageService blob) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        // RedirectUri is where the user lands after successful OAuth — the frontend root.
        // The actual OAuth callback (/api/auth/callback) is handled by the Google middleware.
        var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();
        return Ok(new UserDto(user.Id, user.Email, user.DisplayName, blob.ResolveSasUrl(user.AvatarUrl), user.Bio, user.CreatedAt));
    }
}
