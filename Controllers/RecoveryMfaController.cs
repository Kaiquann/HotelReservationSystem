using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class RecoveryMfaController : Controller
{
    private readonly ICfCaptchaService _cfCaptchaService;
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;
    private readonly IResetMfaService _resetMfaService;

    public RecoveryMfaController(DB db, IEmailService emailService, IMailerService mailerService,
        ICfCaptchaService cfCaptchaService, IResetMfaService resetMfaService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _cfCaptchaService = cfCaptchaService;
        _resetMfaService = resetMfaService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(RecoveryMfaVm recoveryMfaVm)
    {
        if (!_cfCaptchaService.IsCaptchaResponseValid(Request))
        {
            TempData[SweetAlert.Danger] = Common.CaptchaErrorMessage;
            return View(recoveryMfaVm);
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View(recoveryMfaVm);
        }

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == recoveryMfaVm.Email);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Email does not exist.";
            return View(recoveryMfaVm);
        }

        if (!user.IsMfaEnabled)
        {
            TempData[SweetAlert.Danger] = "MFA is not enabled on this account.";
            return RedirectToAction();
        }

        ResetMfa? resetMfaDb = await _resetMfaService.GetResetMfaByUserIdAsync(user.Id.ToString());
        if (resetMfaDb != null) _db.ResetMfas.Remove(resetMfaDb);

        string token = Guid.NewGuid().ToString();
        ResetMfa resetMfa = new()
        {
            UserId = user.Id,
            Token = token
        };

        await _db.ResetMfas.AddAsync(resetMfa);
        await _db.SaveChangesAsync();

        string resetMfaUrl = Url.Action("", "ResetMfa", null, Request.Scheme) +
                             $"?token={token}";

        ResetMfaVm resetMfaVm = new()
        {
            Url = resetMfaUrl
        };

        string body = await _emailService.ResetMfa(resetMfaVm);
        Mail mail = new()
        {
            To = [recoveryMfaVm.Email],
            Subject = EmailDesign.ResetMfaSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "Check your email to reset your MFA.";
        return RedirectToAction("Index", "Login");
    }
}