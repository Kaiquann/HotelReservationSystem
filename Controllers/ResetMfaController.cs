namespace HotelReservationSystem.Controllers;

public class ResetMfaController : Controller
{
    private const string GoogleMfaData = "GoogleMfaData";
    private readonly DB _db;
    private readonly IGoogleMfaService _googleMfaService;
    private readonly IResetMfaService _resetMfaService;

    public ResetMfaController(DB db, IGoogleMfaService googleMfaService,
        IResetMfaService resetMfaService)
    {
        _db = db;
        _googleMfaService = googleMfaService;
        _resetMfaService = resetMfaService;
    }

    public async Task<IActionResult> Index(string token)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return RedirectToAction("", "RecoveryMfa");
        }

        ResetMfa? resetMfa = await _resetMfaService.GetResetMfaByTokenAsync(token);
        if (resetMfa == null)
        {
            TempData[SweetAlert.Danger] = "Invalid or expired token";
            return RedirectToAction("", "RecoveryMfa");
        }

        GoogleMfa googleMfa = _googleMfaService.GenerateMfa(resetMfa.User.Email);

        HttpContext.Session.Set(GoogleMfaData, googleMfa);

        ResetMfaVm resetMfaVm = new()
        {
            Email = resetMfa.User.Email,
            QrCodeUrl = googleMfa.QrCodeUrl,
            ManualEntryCode = googleMfa.ManualEntryCode
        };

        return View(resetMfaVm);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ResetMfaVm resetMfaVm)
    {
        GoogleMfa? googleMfa = HttpContext.Session.Get<GoogleMfa>(GoogleMfaData);
        if (googleMfa == null)
        {
            TempData[SweetAlert.Danger] = "Invalid Session";
            return RedirectToAction("", "RecoveryMfa");
        }

        ResetMfa? resetMfa = await _resetMfaService.GetResetMfaByTokenAsync(resetMfaVm.Token);
        if (resetMfa == null)
        {
            TempData[SweetAlert.Danger] = "Invalid or expired token";
            return RedirectToAction("", "RecoveryMfa");
        }

        resetMfaVm.Email = resetMfa.User.Email;
        resetMfaVm.QrCodeUrl = googleMfa.QrCodeUrl;
        resetMfaVm.ManualEntryCode = googleMfa.ManualEntryCode;

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return View();
        }

        bool isValid2Fa = _googleMfaService.ValidateMfa(googleMfa.SecretKey, resetMfaVm.Code);
        if (!isValid2Fa)
        {
            TempData[SweetAlert.Danger] = "Invalid Code";
            return View(resetMfaVm);
        }

        resetMfa.User.GoogleMfaSecretKey = googleMfa.SecretKey;
        resetMfa.User.UpdatedAt = DateTime.Now;
        _db.ResetMfas.Remove(resetMfa);

        await _db.SaveChangesAsync();

        ClearData();
        TempData[SweetAlert.Success] = "MFA reset successfully";
        return RedirectToAction("", "Login");
    }

    private void ClearData()
    {
        HttpContext.Session.Remove(GoogleMfaData);
    }
}
