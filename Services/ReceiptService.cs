namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the receipt related services.
 */
public interface IReceiptService
{
    // Get receipt details
    Task<ReceiptVm> GetReceiptAsync(Transaction transaction, Order order);

    // Send receipt to user
    Task<bool> SendReceiptAsync(Transaction transaction, Order order);
}

public class ReceiptService : IReceiptService
{
    private readonly IBookingService _bookingService;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;
    private readonly IPaymentService _paymentService;

    public ReceiptService(IBookingService bookingService, IEmailService emailService,
        IMailerService mailerService, IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _emailService = emailService;
        _mailerService = mailerService;
        _paymentService = paymentService;
    }

    public async Task<ReceiptVm> GetReceiptAsync(Transaction transaction, Order order)
    {
        try
        {
            if (transaction == null) throw new Exception("Invalid transaction");
            if (order == null) throw new Exception("Invalid order");

            // Get all bookings
            List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);

            ReceiptVm receiptVm = new()
            {
                Order = order,
                Transaction = transaction,
                Bookings = bookings
            };

            return receiptVm;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<bool> SendReceiptAsync(Transaction transaction, Order order)
    {
        try
        {
            if (transaction == null) throw new Exception("Invalid transaction");
            if (order == null) throw new Exception("Invalid order");

            ReceiptVm receiptVm = await GetReceiptAsync(transaction, order);

            string body = await _emailService.PaymentReceipt(receiptVm);

            // Send email
            Mail mail = new()
            {
                To = [transaction.User.Email],
                Subject = EmailDesign.PaymentReceiptSubject,
                Body = body
            };
            await _mailerService.SendEmail(mail);

            // Stripe Receipt
            Payment payment = await _paymentService.InitPayment(receiptVm.Transaction.SessionId);
            Mail stripeMail = new Mail
            {
                To = [transaction.User.Email],
                Subject = EmailDesign.StripePaymentReceiptSubject,
                Body = payment.ReceiptHtml
            };
            await _mailerService.SendEmail(stripeMail);

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}