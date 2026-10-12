using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.AuthenticatedUsersOnly)]
public class LoginLogController : Controller
{
    private readonly DB _db;

    public LoginLogController(DB db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Search = search = search?.Trim() ?? "";

        List<LoginLog> searched = await _db.LoginLogs
            .Where(ll => (ll.Email.Contains(search) ||
                          ll.Ip!.Contains(search) ||
                          ll.City!.Contains(search) ||
                          ll.Region!.Contains(search) ||
                          ll.Country!.Contains(search) ||
                          ll.Loc!.Contains(search) ||
                          ll.Org!.Contains(search) ||
                          ll.Postal!.Contains(search) ||
                          ll.Timezone!.Contains(search)) &&
                         ll.UserId == currentUser.Id)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, page = 1 });

        IPagedList<LoginLog> model = SortAndPagination(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("LoginLog/_Index", model);

        return View(model);
    }

    /**
     * @description: Sort and Pagination
     */
    private IPagedList<LoginLog> SortAndPagination(List<LoginLog> searched, string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<LoginLog, object> fn = sort switch
        {
            "Email" => s => s.Email,
            "Ip Address" => s => s.Ip!,
            "City" => s => s.City!,
            "Region" => s => s.Region!,
            "Country" => s => s.Country!,
            "Loc" => s => s.Loc!,
            "Org" => s => s.Org!,
            "Postal" => s => s.Postal!,
            "Timezone" => s => s.Timezone!,
            "LoginTime" => s => s.LoginTime,
            _ => s => s.LoginTime
        };

        IOrderedEnumerable<LoginLog> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<LoginLog> pagedList = sorted.ToPagedList(page, 10);

        return pagedList;
    }
}