using System.Text.Json;

namespace HotelReservationSystem.Controllers;

public class RegisterController : Controller
{
    private const string RegisterData = "RegisterData";
    private const string VerificationToken = "VerificationToken";
    private readonly ICfCaptchaService _cfCaptchaService;
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;

    public RegisterController(DB db, IEmailService emailService, IMailerService mailerService,
        ICfCaptchaService cfCaptchaService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _cfCaptchaService = cfCaptchaService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(RegisterVm registerVm)
    {
        if (!_cfCaptchaService.IsCaptchaResponseValid(Request))
        {
            TempData[SweetAlert.Danger] = Common.CaptchaErrorMessage;
            return View(registerVm);
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View(registerVm);
        }

        bool isEmailExists = await _db.IsEmailExists(registerVm.Email);
        if (isEmailExists)
        {
            TempData[SweetAlert.Danger] = Common.EmailExistsErrorMessage;
            return View(registerVm);
        }

        string verificationToken = GetRandomString(8);

        HttpContext.Session.SetString(RegisterData, JsonSerializer.Serialize(registerVm));
        HttpContext.Session.SetString(VerificationToken, verificationToken);

        TokenVm tokenVm = new()
        {
            Token = verificationToken
        };
        string body = await _emailService.Token(tokenVm);

        Mail mail = new()
        {
            To = [registerVm.Email],
            Subject = EmailDesign.TokenSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        return RedirectToAction("Verification");
    }

    public IActionResult Verification()
    {
        string? registerData = HttpContext.Session.GetString(RegisterData);
        string? verificationToken = HttpContext.Session.GetString(VerificationToken);

        if (registerData != null || verificationToken != null) return View();

        TempData[SweetAlert.Danger] = "Session expired. Please register again.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Verification(string token)
    {
        if (!_cfCaptchaService.IsCaptchaResponseValid(Request))
        {
            TempData[SweetAlert.Danger] = Common.CaptchaErrorMessage;
            return View();
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View();
        }

        string? registerData = HttpContext.Session.GetString(RegisterData);
        string? verificationToken = HttpContext.Session.GetString(VerificationToken);

        if (registerData == null || verificationToken == null)
        {
            TempData[SweetAlert.Danger] = "Session expired. Please register again.";
            return RedirectToAction("Index");
        }

        if (token != verificationToken)
        {
            TempData[SweetAlert.Danger] = "Invalid verification token.";
            return View();
        }

        RegisterVm? registerVm = JsonSerializer.Deserialize<RegisterVm>(registerData!);
        if (registerVm == null)
        {
            ClearData();
            TempData[SweetAlert.Danger] = "Invalid verification token.";
            return View();
        }

        User user = new()
        {
            FirstName = registerVm.FirstName,
            LastName = registerVm.LastName,
            UserName = registerVm.Email,
            Email = registerVm.Email,
            Password = HashPassword(registerVm.Password),
            PhoneNumber = registerVm.PhoneNumber
        };

        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        ClearData();
        TempData[SweetAlert.Success] = "Registered successfully.";
        return RedirectToAction("", "Login");
    }

    private void ClearData()
    {
        HttpContext.Session.Remove(RegisterData);
        HttpContext.Session.Remove(VerificationToken);
    }
}