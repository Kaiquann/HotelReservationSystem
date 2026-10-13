using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class ForgotPasswordController : Controller
{
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;
    private readonly IResetPasswordService _resetPasswordService;

    public ForgotPasswordController(DB db, IEmailService emailService, IMailerService mailerService,
        IResetPasswordService resetPasswordService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _resetPasswordService = resetPasswordService;
    }

    public async Task<IActionResult> Index(string? email)
    {
        await _db.ResetPasswords
            .Where(rp => rp.ExpiresAt < DateTime.Now)
            .ExecuteDeleteAsync();

        ForgotPasswordVm forgotPasswordVm = new()
        {
            Email = email ?? string.Empty
        };

        return View(forgotPasswordVm);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ForgotPasswordVm forgotPasswordVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View(forgotPasswordVm);
        }

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordVm.Email);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Email does not exist.";
            return View(forgotPasswordVm);
        }

        string token = Guid.NewGuid().ToString();

        ResetPassword? resetPassword = await _resetPasswordService.GetResetPasswordByUserIdAsync(user.Id.ToString());
        if (resetPassword != null)
        {
            resetPassword.UserId = user.Id;
            resetPassword.Token = token;
        }
        else
        {
            resetPassword = new ResetPassword
            {
                UserId = user.Id,
                Token = token
            };
            await _db.ResetPasswords.AddAsync(resetPassword);
        }

        await _db.SaveChangesAsync();

        string? url = Url.Action("", "ResetPassword", new { token }, Request.Scheme);

        ResetPasswordVm resetPasswordVm = new()
        {
            Url = url!
        };

        string body = await _emailService.ResetPassword(resetPasswordVm);
        Mail mail = new()
        {
            To = [forgotPasswordVm.Email],
            Subject = EmailDesign.ResetPasswordSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "Check your email to reset your password.";
        return RedirectToAction("Index", "Login");
    }
}
