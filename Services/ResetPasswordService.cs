using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the reset password related services.
 */
public interface IResetPasswordService
{
    // Single Reset Password
    Task<ResetPassword?> GetResetPasswordByTokenAsync(string token);
    Task<ResetPassword?> GetResetPasswordByUserIdAsync(string userId);

    // Custom Methods
    Task SyncExpiredResetPasswordAsync();
}

public class ResetPasswordService : IResetPasswordService
{
    private readonly DB _db;

    public ResetPasswordService(DB db)
    {
        _db = db;
    }

    public async Task<ResetPassword?> GetResetPasswordByTokenAsync(string token)
    {
        return await _db.ResetPasswords
            .Include(rp => rp.User)
            .SingleOrDefaultAsync(rp => rp.Token == token && rp.ExpiresAt > DateTime.Now);
    }

    public async Task<ResetPassword?> GetResetPasswordByUserIdAsync(string userId)
    {
        return await _db.ResetPasswords
            .Include(rp => rp.User)
            .FirstOrDefaultAsync(rp => rp.UserId.ToString() == userId);
    }

    public async Task SyncExpiredResetPasswordAsync()
    {
        await _db.ResetPasswords
            .Where(rp => rp.ExpiresAt < DateTime.Now)
            .ExecuteDeleteAsync();
    }
}