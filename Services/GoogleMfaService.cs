using Google.Authenticator;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class handles Google Multi-Factor Authentication (MFA).
 * It provides methods to generate MFA setup information and validate MFA codes.
 */
public class GoogleMfa
{
    public string QrCodeUrl { get; set; } = string.Empty;
    public string ManualEntryCode { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public interface IGoogleMfaService
{
    GoogleMfa GenerateMfa(string email);
    bool ValidateMfa(string secretKey, string code);
}

public class GoogleMfaService : IGoogleMfaService
{
    public const string Issuer = Common.CompanyName;
    private readonly ILogger<GoogleMfaService> _logger;
    private readonly TwoFactorAuthenticator _twoFactorAuthenticator = new();

    public GoogleMfaService(ILogger<GoogleMfaService> logger)
    {
        _logger = logger;
    }

    public GoogleMfa GenerateMfa(string email)
    {
        string secretKey = Guid.NewGuid().ToString().Replace("-", "")[..10];

        SetupCode? setupInfo = _twoFactorAuthenticator.GenerateSetupCode(Issuer, email, secretKey, false);

        GoogleMfa googleMfa = new()
        {
            QrCodeUrl = setupInfo.QrCodeSetupImageUrl,
            ManualEntryCode = setupInfo.ManualEntryKey,
            SecretKey = secretKey
        };
        return googleMfa;
    }

    public bool ValidateMfa(string secretKey, string code)
    {
        return _twoFactorAuthenticator.ValidateTwoFactorPIN(secretKey, code);
    }
}