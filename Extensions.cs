using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem;

public static class Extensions
{
    // ===================================================================================
    // Library For All Project
    // ===================================================================================

    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers.XRequestedWith == "XMLHttpRequest";
    }

    public static bool IsValid(this ModelStateDictionary ms, string key)
    {
        return ms.GetFieldValidationState(key) == ModelValidationState.Valid;
    }

    public static DateOnly ToDateOnly(this DateTime dt)
    {
        return DateOnly.FromDateTime(dt);
    }

    public static TimeOnly ToTimeOnly(this DateTime dt)
    {
        return TimeOnly.FromDateTime(dt);
    }

    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        string? value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public static async Task SignInAsync(this HttpContext httpContext, string name, string role, bool rememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        ];

        ClaimsIdentity identity = new(claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties authProperties = new()
        {
            IsPersistent = rememberMe,
            AllowRefresh = true
        };

        await httpContext.SignInAsync(principal, authProperties);
    }

    public static async Task SignOutAsync(this HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Redirect to referer
     */
    public static RedirectResult RedirectToReferer(this HttpRequest request)
    {
        string referer = request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer)
            ? new RedirectResult(referer)
            : new RedirectResult("/");
    }

    // ===================================================================================
    // Internal Project Use Only
    // ===================================================================================

    public static string IsActive(this IHtmlHelper html, string controller, string? action = null)
    {
        RouteData routeData = html.ViewContext.RouteData;

        string? routeController = routeData.Values["controller"]?.ToString();
        string? routeAction = routeData.Values["action"]?.ToString();

        // Check if the controller matches and optionally the action
        if (string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase) &&
            (action == null || string.Equals(action, routeAction, StringComparison.OrdinalIgnoreCase)))
            return "active";

        // NEW: Check if only the controller matches for all child actions
        if (action == null && string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase))
            return "active";

        return string.Empty;
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if user is logged in
     */
    public static bool IsLoggedIn(this ClaimsPrincipal user)
    {
        return user.Identity is { IsAuthenticated: true };
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if user has admin access
     */
    public static bool HasAdminAccess(this ClaimsPrincipal user)
    {
        return user.IsLoggedIn() && (user.IsInRole(UserRoles.Admin) || user.IsInRole(UserRoles.SuperAdmin));
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Get current logged in user
     */
    public static async Task<User?> GetCurrentUser(this ClaimsPrincipal user, DB db)
    {
        if (!user.IsLoggedIn()) return null;
        return await db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == user.Identity!.Name);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Get all roles except SuperAdmin
     */
    public static async Task<List<Role>> GetRolesAsync(this DB db, User user)
    {
        return await db.Roles
            .Where(r => r.Name != UserRoles.SuperAdmin &&
                        (user.Role != UserRoles.Admin || r.Name != UserRoles.Admin))
            .ToListAsync();
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Get role by name
     */
    public static async Task<Role?> GetRoleAsync(this DB db, string roleName)
    {
        return await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if role is valid
     */
    public static async Task<bool> IsValidRole(this DB db, string roleName)
    {
        return await db.Roles.AnyAsync(r => r.Name == roleName);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if the email exists in the database.
     */
    public static async Task<bool> IsEmailExists(this DB db, string? email)
    {
        if (email == null) return false;
        return await db.Users.AnyAsync(u => u.Email == email);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if the email is unique (does not exist in the database).
     */
    public static async Task<bool> IsEmailUnique(this DB db, string? email)
    {
        if (email == null) return false;
        return !await db.Users.AnyAsync(u => u.Email == email);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check user is admin or not
     */
    public static bool IsAdmin(this User user)
    {
        return user.Role == UserRoles.SuperAdmin || user.Role == UserRoles.Admin;
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Adjust the check in time
     */
    public static DateTime AdjustCheckInTime(this DateTime checkInDate)
    {
        return checkInDate.Date.AddHours(Common.CheckInTime);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Adjust the check out time
     */
    public static DateTime AdjustCheckOutTime(this DateTime checkOutDate)
    {
        return checkOutDate.Date.AddHours(Common.CheckOutTime);
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if the order is allowed to cancel
     */
    public static bool IsOrderAllowToCancel(this Order order)
    {
        if (order.Status != OrderStatus.Confirmed) return false;

        List<Booking> bookings = order.Bookings.ToList();

        if (bookings.Count == 0) return false;

        Booking booking = bookings.First();

        int totalDays = booking.CheckInDate.Date.Subtract(DateTime.Today.Date).Days;

        return totalDays > Common.AllowDaysToCancel;
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if the order is allowed to edit
     */
    public static bool IsOrderAllowToEdit(this Order order)
    {
        if (!EnumHelper.IsAllowValue(order.Status,
                [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Canceled]))
            return false;

        List<Booking> bookings = order.Bookings.ToList();

        if (bookings.Count == 0) return false;

        Booking booking = bookings.First();

        return EnumHelper.IsAllowValue(booking.Status,
                   [BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.Canceled]) &&
               DateTime.Today.Date <= booking.CheckOutDate.Date;
    }

    /**
     * @author: Chong Jun Xiang
     * @description: Check if the booking is allowed to edit
     */
    public static bool IsBookingAllowToEdit(this Booking booking)
    {
        return EnumHelper.IsAllowValue(booking.Status, [BookingStatus.Confirmed]);
    }

    /**
    * @author: Liew Kai Quan
    * @description: Get all Room category
    */
    public static async Task<List<RoomCategory>> GetRoomCategoryAsync(this DB db)
    {
        return await db.RoomCategories
            .Where(r => r.IsDeleted == 0)
            .ToListAsync();
    }

    public static string GetRoomPreference(this RoomSetting roomSetting)
    {
        return roomSetting.IsLandscapeArea ? Preference.LandscapeView : Preference.SmokingArea;
    }
}