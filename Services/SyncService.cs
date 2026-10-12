namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * The SyncMiddleware synchronizes expired data to ensure the system's information is up-to-date
 * before processing the request.
 */
public class SyncMiddleware
{
    private readonly ILogger<SyncMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public SyncMiddleware(RequestDelegate next, ILogger<SyncMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IResetPasswordService resetPasswordService =
            scope.ServiceProvider.GetRequiredService<IResetPasswordService>();
        IResetMfaService resetMfaService = scope.ServiceProvider.GetRequiredService<IResetMfaService>();
        ILoginService loginService = scope.ServiceProvider.GetRequiredService<ILoginService>();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        _logger.LogInformation("Syncing status");
        await resetPasswordService.SyncExpiredResetPasswordAsync();
        await resetMfaService.SyncExpiredResetMfaAsync();
        await loginService.SyncExpiredLoginAttemptsAsync();
        await bookingService.SyncExpiredBookingAsync();

        await _next(context);
    }
}