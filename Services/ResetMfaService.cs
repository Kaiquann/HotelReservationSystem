using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the reset MFA related services.
 */
public interface IResetMfaService
{
    // Single ResetMfa
    Task<ResetMfa?> GetResetMfaByTokenAsync(string token);
    Task<ResetMfa?> GetResetMfaByUserIdAsync(string userId);

    // Custom Methods
    Task SyncExpiredResetMfaAsync();
}

public class ResetMfaService : IResetMfaService
{
    private readonly DB _db;

    public ResetMfaService(DB db)
    {
        _db = db;
    }

    public async Task<ResetMfa?> GetResetMfaByTokenAsync(string token)
    {
        return await _db.ResetMfas
            .Include(rp => rp.User)
            .SingleOrDefaultAsync(rp => rp.Token == token && rp.ExpiresAt > DateTime.Now);
    }

    public async Task<ResetMfa?> GetResetMfaByUserIdAsync(string userId)
    {
        return await _db.ResetMfas
            .Include(rp => rp.User)
            .FirstOrDefaultAsync(rp => rp.UserId.ToString() == userId);
    }

    public async Task SyncExpiredResetMfaAsync()
    {
        await _db.ResetMfas
            .Where(rp => rp.ExpiresAt < DateTime.Now)
            .ExecuteDeleteAsync();
    }
}