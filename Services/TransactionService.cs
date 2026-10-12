using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the transaction related services.
 */
public interface ITransactionService
{
    // Single Transaction
    Task<Transaction?> GetTransactionAsync(string transactionNumber);
    Task<Transaction?> GetTransactionWithUserIdAsync(string transactionNumber, string userId);
}

public class TransactionService : ITransactionService
{
    private readonly DB _db;

    public TransactionService(DB db)
    {
        _db = db;
    }

    public async Task<Transaction?> GetTransactionAsync(string transactionNumber)
    {
        return await _db.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);
    }

    public async Task<Transaction?> GetTransactionWithUserIdAsync(string transactionNumber, string userId)
    {
        return await _db.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber &&
                                      t.UserId.ToString() == userId);
    }
}