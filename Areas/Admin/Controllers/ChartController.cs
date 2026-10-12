using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class ChartController : Controller
{
    private readonly DB _db;

    public ChartController(DB db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        List<Order> orderList = await _db.Orders
            .Include(o => o.Bookings)
            .ThenInclude(o => o.Room)
            .ThenInclude(o => o.RoomCategory)
            .Where(o => o.Status == OrderStatus.Confirmed)
            .ToListAsync();

        if (!orderList.Any())
        {
            TempData[SweetAlert.Danger] = "No booking";
            return RedirectToAction("Index", "Room");
        }

        var roomCategoryTotals = orderList
            .SelectMany(o => o.Bookings)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn ||
                        b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.Canceled)
            .Where(b => b.Room != null && b.Room.RoomCategory != null)
            .GroupBy(b => b.Room.RoomCategory.Type)
            .Select(group => new
            {
                RoomCategoryName = group.Key,
                TotalAmount = group.Sum(b => b.TotalAmount)
            })
            .ToList();

        RoomReportVm roomReportVm = new()
        {
            RoomCategory = roomCategoryTotals.Select(rc => new RoomCategory
            {
                Type = rc.RoomCategoryName
            }).ToList(),
            TotalAmount = roomCategoryTotals.Select(rc => rc.TotalAmount).ToList()
        };

        return View(roomReportVm);
    }

    [HttpPost]
    public async Task<IActionResult> Filter(int selectedYear, int selectedMonth)
    {
        List<Order> filterOrder = await _db.Orders
            .Include(o => o.Bookings)
            .ThenInclude(o => o.Room)
            .ThenInclude(o => o.RoomCategory)
            .Where(o => o.Status == OrderStatus.Confirmed &&
                        (selectedYear == 0 || o.CreatedAt.Year == selectedYear) &&
                        (selectedMonth == 0 || o.CreatedAt.Month == selectedMonth))
            .ToListAsync();

        var groupedData = filterOrder
            .SelectMany(o => o.Bookings)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn ||
                        b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.Canceled)
            .GroupBy(b => b.Room.RoomCategory.Type)
            .Select(group => new
            {
                CategoryName = group.Key,
                TotalAmount = group.Sum(b => b.TotalAmount)
            })
            .ToList();

        RoomReportVm viewModel = new()
        {
            RoomCategory = groupedData.Select(g => new RoomCategory { Type = g.CategoryName }).ToList(),
            TotalAmount = groupedData.Select(g => g.TotalAmount).ToList(),
            SelectedYear = selectedYear > 0 ? selectedYear : DateTime.Now.Year,
            SelectedMonth = selectedMonth > 0 ? selectedMonth : 0
        };
        return View("Index", viewModel);
    }
}