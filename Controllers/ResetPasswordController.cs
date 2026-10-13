namespace HotelReservationSystem.Controllers;

public class ResetPasswordController : Controller
{
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IIpInfoService _ipInfoService;
    private readonly ILoginService _loginService;
    private readonly IMailerService _mailerService;
    private readonly IResetPasswordService _resetPasswordService;

    public ResetPasswordController(DB db, ILoginService loginService,
        IResetPasswordService resetPasswordService, IIpInfoService ipInfoService, IMailerService mailerService,
        IEmailService emailService)
    {
        _db = db;
        _loginService = loginService;
        _resetPasswordService = resetPasswordService;
        _ipInfoService = ipInfoService;
        _mailerService = mailerService;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index(string token)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("", "ForgotPassword");
        }

        ResetPassword? resetPassword = await _resetPasswordService.GetResetPasswordByTokenAsync(token);
        if (resetPassword == null)
        {
            TempData[SweetAlert.Danger] = "Invalid or expired token";
            return RedirectToAction("", "ForgotPassword");
        }

        ResetPasswordVm resetPasswordVm = new()
        {
            Token = token,
            Email = resetPassword.User.Email
        };

        return View(resetPasswordVm);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ResetPasswordVm resetPasswordVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("", "ResetPassword", new { token = resetPasswordVm.Token });
        }

        ResetPassword? resetPassword = await _resetPasswordService.GetResetPasswordByTokenAsync(resetPasswordVm.Token);
        if (resetPassword == null)
        {
            TempData[SweetAlert.Danger] = "Invalid or expired token";
            return RedirectToAction("", "ForgotPassword");
        }

        User? user = await _db.Users.FindAsync(resetPassword.UserId);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Invalid User Id";
            return View(resetPasswordVm);
        }

        await _loginService.ClearUserLoginAttemptsAsync(user);

        user.Password = HashPassword(resetPasswordVm.NewPassword);
        user.UpdatedAt = DateTime.Now;

        _db.ResetPasswords.Remove(resetPassword);

        await _db.SaveChangesAsync();

        IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(HttpContext);

        string? url = Url.Action("Index", "ForgotPassword", new { email = user.Email }, Request.Scheme);

        PasswordChangedVm passwordChangedVm = new PasswordChangedVm
        {
            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Email = user.Email,
            Ip = ipInfo?.Ip!,
            Url = url!
        };
        string body = await _emailService.PasswordChanged(passwordChangedVm);

        Mail mail = new Mail
        {
            To = [user.Email],
            Subject = EmailDesign.PasswordChangedSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "Password reset successfully.";
        return RedirectToAction("", "Login");
    }
}
