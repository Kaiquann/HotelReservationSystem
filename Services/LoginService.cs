using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the login related services.
 */
public interface ILoginService
{
    Task LoginAsync(User user, bool rememberMe);
    Task<LoginAttempt?> FindLoginAttemptsAsync(User user);
    Task<LoginAttempt?> UpdateLoginAttemptAsync(User user);
    Task ClearUserLoginAttemptsAsync(User user);
    Task SyncExpiredLoginAttemptsAsync();
}

public class LoginService : ILoginService
{
    private const string ForgotPasswordUrl = "/ForgotPassword/Index?email=";
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IIpInfoService _ipInfoService;
    private readonly ILogger<LoginService> _logger;
    private readonly IMailerService _mailerService;

    public LoginService(DB db, IEmailService emailService, IIpInfoService ipInfoService, IMailerService mailerService,
        IHttpContextAccessor httpContextAccessor, ILogger<LoginService> logger)
    {
        _db = db;
        _emailService = emailService;
        _ipInfoService = ipInfoService;
        _mailerService = mailerService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LoginAsync(User user, bool rememberMe)
    {
        await _httpContextAccessor.HttpContext!.SignInAsync(user.Id.ToString(), user.Role, rememberMe);

        IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(_httpContextAccessor.HttpContext!);
        LoginLog loginLog = new LoginLog
        {
            Email = user.Email,
            Ip = ipInfo?.Ip,
            City = ipInfo?.City,
            Region = ipInfo?.Region,
            Country = ipInfo?.Country,
            Loc = ipInfo?.Loc,
            Org = ipInfo?.Org,
            Postal = ipInfo?.Postal,
            Timezone = ipInfo?.Timezone,
            UserId = user.Id
        };

        await _db.LoginLogs.AddAsync(loginLog);
        await _db.SaveChangesAsync();

        string url = ForgotPasswordUrl + user.Email;

        LoginAlertVm loginAlertVm = new LoginAlertVm
        {
            Email = user.Email,
            IpInfo = ipInfo!,
            LoginTime = loginLog.LoginTime,
            Url = url!
        };

        string body = await _emailService.LoginAlert(loginAlertVm);

        Mail mail = new Mail
        {
            To = [user.Email],
            Subject = EmailDesign.LoginAlertSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);
    }

    public async Task<LoginAttempt?> FindLoginAttemptsAsync(User user)
    {
        IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(_httpContextAccessor.HttpContext!);

        LoginAttempt? loginAttempt = await _db.LoginAttempts
            .FirstOrDefaultAsync(la => la.UserId == user.Id || la.Ip == ipInfo!.Ip);

        return loginAttempt;
    }

    public async Task<LoginAttempt?> UpdateLoginAttemptAsync(User user)
    {
        try
        {
            IpInfoResponse? ipInfo = await _ipInfoService.GetIpInfo(_httpContextAccessor.HttpContext!);

            LoginAttempt? loginAttempt = await _db.LoginAttempts
                .FirstOrDefaultAsync(la => la.UserId == user.Id || la.Ip == ipInfo!.Ip);

            if (loginAttempt == null) // If there is no login attempt, create a new one
            {
                loginAttempt = new LoginAttempt
                {
                    TryAttempt = 1,
                    Ip = ipInfo?.Ip,
                    UserId = user.Id
                };
                _db.LoginAttempts.Add(loginAttempt);
            }
            else // If there is a login attempt, increment the try attempt
            {
                loginAttempt.TryAttempt++;
                if (loginAttempt.TryAttempt >= 3)
                {
                    loginAttempt.IsBlocked = true;
                    loginAttempt.ExpiredAt = DateTime.Now.AddMinutes(15);

                    string url = ForgotPasswordUrl + user.Email;

                    IllegalLoginVm illegalLoginVm = new IllegalLoginVm
                    {
                        Email = user.Email,
                        Ip = ipInfo?.Ip!,
                        Url = url!
                    };
                    string body = await _emailService.IllegalLogin(illegalLoginVm);

                    Mail mail = new Mail
                    {
                        To = [user.Email],
                        Subject = EmailDesign.IllegalLoginSubject,
                        Body = body
                    };
                    await _mailerService.SendEmail(mail);
                }
            }

            await _db.SaveChangesAsync();
            return loginAttempt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking login attempts for {Email}", user.Email);
            throw;
        }
    }

    public async Task ClearUserLoginAttemptsAsync(User user)
    {
        await _db.LoginAttempts
            .Where(la => la.UserId == user.Id)
            .ExecuteDeleteAsync();
    }

    public async Task SyncExpiredLoginAttemptsAsync()
    {
        await _db.LoginAttempts
            .Where(la => la.ExpiredAt < DateTime.Now)
            .ExecuteDeleteAsync();
    }
}