using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the order related services.
 */
public interface IOrderService
{
    // Single Order
    Task<Order?> GetOrderAsync(string orderNumber);
    Task<Order?> GetOrderWithUserIdAsync(string orderNumber, string userId);
    Task<Order?> GetOrderWithTransactionNumberAsync(string transactionNumber);
    Task<Order?> GetOrderWithTransactionNumberAndUserIdAsync(string transactionNumber, string userId);

    // Multiple Orders
    Task<List<Order>> GetOrdersAsync();
}

public class OrderService : IOrderService
{
    private readonly DB _db;

    public OrderService(DB db)
    {
        _db = db;
    }

    public async Task<Order?> GetOrderAsync(string orderNumber)
    {
        return await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Bookings)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<Order?> GetOrderWithUserIdAsync(string orderNumber, string userId)
    {
        return await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Bookings)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber &&
                                      o.UserId.ToString() == userId);
    }

    public async Task<Order?> GetOrderWithTransactionNumberAsync(string transactionNumber)
    {
        return await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Bookings)
            .FirstOrDefaultAsync(o => o.TransactionNumber == transactionNumber);
    }

    public async Task<Order?> GetOrderWithTransactionNumberAndUserIdAsync(string transactionNumber, string userId)
    {
        return await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Bookings)
            .FirstOrDefaultAsync(o => o.TransactionNumber == transactionNumber &&
                                      o.UserId.ToString() == userId);
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Bookings)
            .ToListAsync();
    }
}