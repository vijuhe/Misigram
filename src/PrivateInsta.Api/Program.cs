using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PrivateInsta.Api.Data;
using PrivateInsta.Api.Hubs;
using PrivateInsta.Api.Middleware;
using PrivateInsta.Api.Models;
using PrivateInsta.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
})
.AddGoogle(o =>
{
    o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

    // Must match the redirect URI registered in Google Cloud Console
    o.CallbackPath = "/api/auth/callback";

    o.Scope.Add("email");
    o.Scope.Add("profile");
    o.ClaimActions.MapJsonKey("picture", "picture");

    o.Events.OnTicketReceived = async ctx =>
    {
        var principal = ctx.Principal;
        var email = principal?.FindFirstValue(ClaimTypes.Email);
        var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var allowed = config.GetSection("AllowedGoogleAccounts").Get<string[]>() ?? [];

        if (principal is null || string.IsNullOrEmpty(email) || !allowed.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            ctx.Fail("Your Google account is not authorised.");
            return;
        }

        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
        var picture = principal.FindFirstValue("picture");

        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user is null)
        {
            user = new User { GoogleId = googleId, Email = email, DisplayName = name, AvatarUrl = picture };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Replace the Google principal with our own app principal
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));

        ctx.Principal = new ClaimsPrincipal(identity);
        ctx.Properties!.IsPersistent = true;
        ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
    };

    o.Events.OnRemoteFailure = ctx =>
    {
        ctx.Response.Redirect("/login?error=access_denied");
        ctx.HandleResponse();
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddControllers();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddHostedService<StoryExpiryService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

var app = builder.Build();

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<GoogleAllowlistMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapFallbackToFile("index.html");

app.Run();
