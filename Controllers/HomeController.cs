using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class HomeController : Controller
{
    private readonly DB _db;
    private readonly IMailerService _mailerService;

    public HomeController(DB db, IMailerService mailerService)
    {
        _db = db;
        _mailerService = mailerService;
    }

    public async Task<IActionResult> Index()
    {
        var topSales = await _db.Bookings
            .Include(bk => bk.Room)
            .ThenInclude(r => r.RoomCategory)
            .ThenInclude(r => r.RoomImages)
            .GroupBy(bk => bk.Room.RoomCategoryId)
            .Select(group => new
            {
                RoomCategoryId = group.Key,
                group.First().Room.RoomCategory,
                RoomCount = group.Count()
            })
            .OrderByDescending(result => result.RoomCount)
            .Take(3)
            .ToListAsync();

        RoomVm roomVm = new RoomVm
        {
            RoomCategories = topSales.Select(ts => ts.RoomCategory).ToList()
        };

        return View(roomVm);
    }

    public IActionResult AboutUs()
    {
        return View();
    }

    public IActionResult ContactUs()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitContactUs(SendContactUsVm sendEmailVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = "Please fill in all fields correctly.";
            return View("ContactUs", sendEmailVm);
        }

        // modified: Chong Jun Xiang
        User[] admins = await _db.Users
            .Where(u => u.Role == UserRoles.SuperAdmin ||
                        u.Role == UserRoles.Admin)
            .ToArrayAsync();

        string subject = "New Contact Us Message";
        string message = $@"
        <p><strong>Name:</strong> {sendEmailVm.Name}</p>
        <p><strong>Email:</strong> {sendEmailVm.Email}</p>
        <p><strong>Message:</strong></p>
        <p>{sendEmailVm.Message}</p>
        <p><em>Sent on {DateTime.Now}</em></p>";

        Mail mail = new()
        {
            Subject = subject,
            Body = message,
            // modified: Chong Jun Xiang
            Bcc = [..admins.Select(a => a.Email).ToArray()]
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "Your message has been sent successfully!";
        return RedirectToAction("ContactUs");
    }
}