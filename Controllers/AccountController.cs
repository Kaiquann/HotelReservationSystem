using System.Text.Json;

namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.AuthenticatedUsersOnly)]
public class AccountController : Controller
{
    private const string ChangeEmailData = "ChangeEmailData";
    private const string VerificationToken = "VerificationToken";
    private const string GoogleMfaData = "GoogleMfaData";
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IGoogleMfaService _googleMfaService;
    private readonly IImageService _imageService;
    private readonly IIpInfoService _ipInfoService;
    private readonly IMailerService _mailerService;

    public AccountController(DB db, IEmailService emailService, IMailerService mailerService,
        IIpInfoService ipInfoService, IGoogleMfaService googleMfaService,
        IImageService imageService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _ipInfoService = ipInfoService;
        _googleMfaService = googleMfaService;
        _imageService = imageService;
    }

    public async Task<IActionResult> Profile()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser != null) return View(currentUser);

        TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> EditProfile()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ProfileVm profileVm = new ProfileVm
        {
            FirstName = currentUser.FirstName,
            LastName = currentUser.LastName,
            UserName = currentUser.UserName,
            PhoneNumber = currentUser.PhoneNumber,
            ProfileImageUrl = currentUser.ProfileImageUrl
        };

        if (Request.IsAjax()) return PartialView("_EditProfile", profileVm);

        return View(profileVm);
    }

    [HttpPost]
    public async Task<IActionResult> EditProfile(ProfileVm profileVm)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return Request.RedirectToReferer();
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        if (profileVm.ProfileImage != null)
        {
            string error = _imageService.ValidatePhoto(profileVm.ProfileImage);
            if (error != string.Empty) ModelState.AddModelError("ProfileImage", error);
        }

        if (profileVm.ProfileImage != null)
        {
            if (!string.IsNullOrEmpty(currentUser.ProfileImageUrl))
                try
                {
                    _imageService.DeletePhoto(currentUser.ProfileImageUrl, Common.ImageUploadPath);
                }
                catch
                {
                    // ignored
                }

            try
            {
                currentUser.ProfileImageUrl = _imageService.SavePhoto(profileVm.ProfileImage, Common.ImageUploadPath);
            }
            catch
            {
                TempData[SweetAlert.Danger] = "Failed to upload image";
                return Request.RedirectToReferer();
            }
        }

        currentUser.FirstName = profileVm.FirstName;
        currentUser.LastName = profileVm.LastName;
        currentUser.UserName = profileVm.UserName;
        currentUser.PhoneNumber = profileVm.PhoneNumber;
        currentUser.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = "Profile updated successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProfileImage()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return Request.RedirectToReferer();
        }

        if (string.IsNullOrEmpty(currentUser.ProfileImageUrl))
        {
            TempData[SweetAlert.Danger] = "No image to remove";
            return Request.RedirectToReferer();
        }

        if (!string.IsNullOrEmpty(currentUser.ProfileImageUrl))
            try
            {
                _imageService.DeletePhoto(currentUser.ProfileImageUrl, Common.ImageUploadPath);
            }
            catch
            {
                // ignored
            }

        currentUser.ProfileImageUrl = null;
        currentUser.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = "Profile updated successfully";
        return Request.RedirectToReferer();
    }

    public async Task<IActionResult> ChangeEmail()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ChangeEmailVm changeEmailVm = new ChangeEmailVm
        {
            OldEmail = currentUser.Email
        };

        return View(changeEmailVm);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeEmail(ChangeEmailVm changeEmailVm)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("ChangeEmail");
        }

        if (!VerifyPassword(currentUser.Password, changeEmailVm.Password))
        {
            TempData[SweetAlert.Danger] = "Invalid password";
            return RedirectToAction("ChangeEmail");
        }

        if (currentUser.Email == changeEmailVm.Email)
        {
            TempData[SweetAlert.Danger] = "New email is the same as the old email";
            return RedirectToAction("ChangeEmail");
        }

        bool isEmailExists = await _db.IsEmailExists(changeEmailVm.Email);
        if (isEmailExists)
        {
            TempData[SweetAlert.Danger] = Common.EmailExistsErrorMessage;
            return RedirectToAction("ChangeEmail");
        }

        string verificationToken = GetRandomString(8);

        HttpContext.Session.SetString(ChangeEmailData, JsonSerializer.Serialize(changeEmailVm));
        HttpContext.Session.SetString(VerificationToken, verificationToken);

        TokenVm tokenVm = new TokenVm
        {
            Token = verificationToken
        };
        string body = await _emailService.Token(tokenVm);

        Mail mail = new Mail
        {
            To = [changeEmailVm.Email],
            Subject = EmailDesign.TokenSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        return RedirectToAction("Verification");
    }

    public async Task<IActionResult> Verification()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        string? changeEmailData = HttpContext.Session.GetString(ChangeEmailData);
        string? verificationToken = HttpContext.Session.GetString(VerificationToken);

        if (changeEmailData == null || verificationToken == null)
        {
            TempData[SweetAlert.Danger] = "Session expired. Please register again.";
            return RedirectToAction("ChangeEmail");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Verification(string token)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        string? changeEmailData = HttpContext.Session.GetString(ChangeEmailData);
        string? verificationToken = HttpContext.Session.GetString(VerificationToken);

        if (changeEmailData == null || verificationToken == null)
        {
            TempData[SweetAlert.Danger] = "Session expired. Please register again.";
            return RedirectToAction("ChangeEmail");
        }

        if (token != verificationToken)
        {
            TempData[SweetAlert.Danger] = "Invalid verification token.";
            return View("Verification");
        }

        ChangeEmailVm? changeEmailVm = JsonSerializer.Deserialize<ChangeEmailVm>(changeEmailData);

        if (changeEmailVm == null)
        {
            ClearData();
            TempData[SweetAlert.Danger] = "Invalid verification token.";
            return RedirectToAction("ChangeEmail");
        }

        currentUser.Email = changeEmailVm.Email;
        currentUser.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(HttpContext);

        string subject = "Email Changed Successfully";
        string message = "You have changed your email from " + changeEmailVm.OldEmail + " to " + changeEmailVm.Email +
                         ". IP: " + ipInfo?.Ip! + " on " + DateTime.Now;

        SimpleTitleMessageVm simpleTitleMessageVm = new SimpleTitleMessageVm
        {
            Title = subject,
            Message = message
        };
        string simpleTitleMessage = await _emailService.SimpleTitleMessage(simpleTitleMessageVm);

        Mail mail = new Mail
        {
            To = [currentUser.Email],
            Cc = [changeEmailVm.OldEmail],
            Subject = subject,
            Body = simpleTitleMessage
        };
        await _mailerService.SendEmail(mail);

        ClearData();
        TempData[SweetAlert.Success] = subject;
        return RedirectToAction("Profile");
    }

    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm changePasswordVm)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View(changePasswordVm);
        }

        bool isValidPassword = VerifyPassword(currentUser.Password, changePasswordVm.OldPassword);

        if (!isValidPassword)
        {
            TempData[SweetAlert.Danger] = "Old password is incorrect";
            return View(changePasswordVm);
        }

        currentUser.Password = HashPassword(changePasswordVm.NewPassword);
        currentUser.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(HttpContext);

        string? url = Url.Action("Index", "ForgotPassword", new { email = currentUser.Email }, Request.Scheme);

        PasswordChangedVm passwordChangedVm = new PasswordChangedVm
        {
            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Email = currentUser.Email,
            Ip = ipInfo?.Ip!,
            Url = url!
        };
        string body = await _emailService.PasswordChanged(passwordChangedVm);

        Mail mail = new Mail
        {
            To = [currentUser.Email],
            Subject = EmailDesign.PasswordChangedSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "Password changed successfully";
        return RedirectToAction("Profile");
    }

    public async Task<IActionResult> EnableMfa()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (currentUser.IsMfaEnabled)
        {
            TempData[SweetAlert.Danger] = "MFA is already enabled";
            return RedirectToAction("Profile");
        }

        GoogleMfa googleMfa = _googleMfaService.GenerateMfa(currentUser.Email);
        HttpContext.Session.Set(GoogleMfaData, googleMfa);

        EnableMfaVm multiFactorAuthVm = new EnableMfaVm
        {
            Email = currentUser.Email,
            QrCodeUrl = googleMfa.QrCodeUrl,
            ManualEntryCode = googleMfa.ManualEntryCode
        };

        return View(multiFactorAuthVm);
    }

    [HttpPost]
    public async Task<IActionResult> EnableMfa(EnableMfaVm enableMfaVm)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (currentUser.IsMfaEnabled)
        {
            TempData[SweetAlert.Danger] = "MFA is already enabled";
            return RedirectToAction("Profile");
        }

        GoogleMfa? googleMfa = HttpContext.Session.Get<GoogleMfa>(GoogleMfaData);
        if (googleMfa == null)
        {
            TempData[SweetAlert.Danger] = "Session expired. Please enable MFA again.";
            return RedirectToAction("EnableMfa");
        }

        enableMfaVm.Email = currentUser.Email;
        enableMfaVm.QrCodeUrl = googleMfa.QrCodeUrl;
        enableMfaVm.ManualEntryCode = googleMfa.ManualEntryCode;

        bool isValid2Fa = _googleMfaService.ValidateMfa(googleMfa.SecretKey, enableMfaVm.Code);
        if (!isValid2Fa)
        {
            TempData[SweetAlert.Danger] = "Invalid Code";
            ModelState.AddModelError("Code", "Invalid Code");
            return View(enableMfaVm);
        }

        currentUser.IsMfaEnabled = true;
        currentUser.GoogleMfaSecretKey = googleMfa.SecretKey;
        currentUser.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        string message = "MFA enabled successfully on " + DateTime.Now;
        await SendMfaAlert(currentUser, message);

        TempData[SweetAlert.Success] = "MFA Enabled Successfully";
        return RedirectToAction("Profile");
    }

    public async Task<IActionResult> DisableMfa()
    {
        User? currentUser = await User.GetCurrentUser(_db);

        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (currentUser.IsMfaEnabled) return View();

        TempData[SweetAlert.Danger] = "MFA is not enabled";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    public async Task<IActionResult> DisableMfa(DisableMfaVm disableMfaVm)
    {
        User? currentUser = await User.GetCurrentUser(_db);

        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (!currentUser.IsMfaEnabled)
        {
            TempData[SweetAlert.Danger] = "MFA is not enabled";
            return RedirectToAction("Profile");
        }

        bool isValidPassword = VerifyPassword(currentUser.Password, disableMfaVm.Password);
        if (!isValidPassword)
        {
            TempData[SweetAlert.Danger] = "Invalid Password";
            return View();
        }

        bool isValidCode = _googleMfaService.ValidateMfa(currentUser.GoogleMfaSecretKey, disableMfaVm.Code);
        if (!isValidCode)
        {
            TempData[SweetAlert.Danger] = "Invalid Code";
            return View();
        }

        currentUser.IsMfaEnabled = false;
        currentUser.GoogleMfaSecretKey = string.Empty;
        currentUser.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        string message = "MFA disabled successfully on " + DateTime.Now;
        await SendMfaAlert(currentUser, message);

        TempData[SweetAlert.Success] = "MFA Disabled Successfully";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        TempData[SweetAlert.Success] = "Log out successful";
        return RedirectToAction("Index", "Home");
    }

    private async Task SendMfaAlert(User user, string message)
    {
        SimpleTitleMessageVm simpleTitleMessageVm = new SimpleTitleMessageVm
        {
            Title = EmailDesign.MfaAlertSubject,
            Message = message
        };
        string body = await _emailService.SimpleTitleMessage(simpleTitleMessageVm);

        Mail mail = new Mail
        {
            To = [user.Email],
            Subject = EmailDesign.MfaAlertSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);
    }

    private void ClearData()
    {
        HttpContext.Session.Remove(ChangeEmailData);
        HttpContext.Session.Remove(VerificationToken);
    }
}
