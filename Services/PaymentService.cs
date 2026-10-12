using Stripe;
using Stripe.Checkout;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the payment related services.
 */
public class Payment
{
    // Stripe entities
    public Session Session { get; set; }
    public PaymentIntent PaymentIntent { get; set; }
    public Charge Charge { get; set; }
    public Customer Customer { get; set; }

    // Custom properties
    public string PaymentIntentId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Subtotal { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ServiceCharge { get; set; }
    public decimal AmountTax { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ReceiptUrl { get; set; } = string.Empty;
    public string ReceiptHtml { get; set; } = string.Empty;
}

public interface IPaymentService
{
    Task<Payment> InitPayment(string sessionId);
}

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly IStripeService _stripeService;

    public PaymentService(ILogger<PaymentService> logger, IStripeService stripeService)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<Payment> InitPayment(string sessionId)
    {
        Session session = await _stripeService.GetCheckoutSessionAsync(sessionId);
        string paymentIntentId = session.PaymentIntentId;
        decimal amountTax = session.TotalDetails.AmountTax;

        PaymentIntent? paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);
        string latestChargeId = paymentIntent.LatestChargeId;
        string customerId = paymentIntent.CustomerId;
        string paymentMethod = string.Join(",", paymentIntent.PaymentMethodTypes);

        Charge? charge = await _stripeService.GetChargeAsync(latestChargeId);
        string receiptUrl = charge.ReceiptUrl;

        Customer? customer = await _stripeService.GetCustomerAsync(customerId);

        string receiptHtml = await HttpHelper.GetHtmlWithUrl(receiptUrl);

        string orderNumber = "O" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);

        Payment payment = new()
        {
            Session = session,
            PaymentIntent = paymentIntent,
            Charge = charge,
            Customer = customer,
            PaymentIntentId = paymentIntentId,
            OrderNumber = orderNumber,
            Amount = session.AmountTotal.GetValueOrDefault() / 100m,
            Subtotal = session.AmountSubtotal.GetValueOrDefault() / 100m,
            PaymentMethod = paymentMethod,
            Status = paymentIntent.Status,
            AmountTax = amountTax / 100m,
            ServiceCharge = amountTax / 100m,
            CreatedAt = paymentIntent.Created,
            ReceiptUrl = receiptUrl,
            ReceiptHtml = receiptHtml
        };
        return payment;
    }
}