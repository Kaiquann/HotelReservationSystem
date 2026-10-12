namespace HotelReservationSystem.Controllers;

public class UserController : Controller
{
    private readonly DB _db;
    private readonly ILogger<UserController> _logger;

    public UserController(DB db, ILogger<UserController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> IsEmailUnique(string? email)
    {
        _logger.LogInformation($"Email: {email}");
        return Json(await _db.IsEmailUnique(email));
    }
}