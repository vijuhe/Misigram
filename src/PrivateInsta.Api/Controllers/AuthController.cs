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
        if (!Url.IsLocalUrl(returnUrl)) returnUrl = "/";
        var props = new AuthenticationProperties { RedirectUri = returnUrl };
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
        return Ok(user.ToDto(blob));
    }
}
