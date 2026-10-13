using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class LoginController : Controller
{
    private const string LoginData = "LoginData";
    private readonly DB _db;
    private readonly IGoogleMfaService _googleMfaService;
    private readonly ILoginService _loginService;

    public LoginController(DB db, IGoogleMfaService googleMfaService,
        ILoginService loginService)
    {
        _db = db;
        _googleMfaService = googleMfaService;
        _loginService = loginService;
    }

    public IActionResult Index(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(LoginVm loginVm, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View(loginVm);
        }

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginVm.Email);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Email or password is incorrect.";
            return View(loginVm);
        }

        if (user.IsBlocked)
        {
            TempData[SweetAlert.Danger] = "Account has been blocked.";
            return View(loginVm);
        }

        if (user.IsDeleted >= 1)
        {
            TempData[SweetAlert.Danger] = "Account has been suspended.";
            return View(loginVm);
        }

        LoginAttempt? loginAttempt = await _loginService.FindLoginAttemptsAsync(user);
        if (loginAttempt != null && loginAttempt.IsBlocked)
        {
            TempData[SweetAlert.Danger] = "IP and account has been blocked for 15 minutes.";
            return View(loginVm);
        }

        if (!VerifyPassword(user.Password, loginVm.Password))
        {
            await _loginService.UpdateLoginAttemptAsync(user);
            TempData[SweetAlert.Danger] = "Email or password is incorrect.";
            return View(loginVm);
        }

        if (user.IsMfaEnabled)
        {
            HttpContext.Session.Set(LoginData, loginVm);
            return RedirectToAction("Mfa", new { returnUrl });
        }

        await _loginService.LoginAsync(user, loginVm.RememberMe);
        await _loginService.ClearUserLoginAttemptsAsync(user);

        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Mfa(string? returnUrl)
    {
        LoginVm? loginVm = HttpContext.Session.Get<LoginVm>(LoginData);
        if (loginVm == null)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("Index");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Mfa(LoginMfaVm loginMfaVm, string? returnUrl)
    {
        LoginVm? loginVm = HttpContext.Session.Get<LoginVm>(LoginData);
        if (loginVm == null)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View();
        }

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginVm.Email);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Email or password is incorrect.";
            return RedirectToAction("Index");
        }

        bool isValid2Fa = _googleMfaService.ValidateMfa(user.GoogleMfaSecretKey, loginMfaVm.Code);
        if (!isValid2Fa)
        {
            TempData[SweetAlert.Danger] = "Invalid Code";
            return View();
        }

        await _loginService.LoginAsync(user, loginVm.RememberMe);
        await _loginService.ClearUserLoginAttemptsAsync(user);

        HttpContext.Session.Remove(LoginData);

        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}
