using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * Ensures user roles and claims are synchronized with the database for
 * accurate authorization.
 */
public interface IUserClaimsSyncService
{
    Task<User?> GetUserAsync(string userId);
}

public class UserClaimsSyncService : IUserClaimsSyncService
{
    private readonly DB _db;

    public UserClaimsSyncService(DB db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
    }
}

public class UserClaimsSyncMiddleware
{
    private readonly ILogger<UserClaimsSyncMiddleware> _logger;
    private readonly RequestDelegate _next;

    public UserClaimsSyncMiddleware(RequestDelegate next, ILogger<UserClaimsSyncMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IUserClaimsSyncService userSyncService)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
            try
            {
                string? userId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    User? user = await userSyncService.GetUserAsync(userId);

                    if (user == null)
                    {
                        _logger.LogWarning("User {Id} not found in the database. Logging out.", userId);
                        await LogoutUserAsync(context);
                        context.Response.Redirect("/Login");
                        return;
                    }

                    if (user.IsBlocked || user.IsDeleted >= 1)
                    {
                        _logger.LogWarning("User {Id} is blocked or deleted. Logging out.", userId);
                        await LogoutUserAsync(context);
                        context.Response.Redirect("/Login");
                        return;
                    }

                    if (context.User.Identity is ClaimsIdentity identity)
                    {
                        await UpdateUserRoleIfNeededAsync(context, identity, user, userId);
                    }
                    else
                    {
                        _logger.LogWarning("User {Id} has an invalid claims identity. Logging out.", userId);
                        await LogoutUserAsync(context);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during role synchronization.");
            }

        await _next(context);
    }

    private async Task UpdateUserRoleIfNeededAsync(HttpContext context, ClaimsIdentity identity, User user,
        string userId)
    {
        string latestRole = user.Role;
        string? currentRole = identity.FindFirst(ClaimTypes.Role)?.Value;

        if (currentRole != latestRole)
        {
            _logger.LogInformation(
                "Updating role for user {Id} from {CurrentRole} to {NewRole}.",
                userId, currentRole, latestRole);

            UpdateUserRoleClaim(identity, latestRole);

            // Re-authenticate the user with updated claims
            ClaimsPrincipal newPrincipal = new(identity);
            await context.SignInAsync(newPrincipal);
        }
    }

    private static void UpdateUserRoleClaim(ClaimsIdentity identity, string latestRole)
    {
        Claim? roleClaim = identity.FindFirst(ClaimTypes.Role);
        if (roleClaim != null) identity.RemoveClaim(roleClaim);
        identity.AddClaim(new Claim(ClaimTypes.Role, latestRole));
    }

    private static async Task LogoutUserAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}