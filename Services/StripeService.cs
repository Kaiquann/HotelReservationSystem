using Stripe;
using Stripe.Checkout;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class integrates with Stripe for payment processing.
 */
public class StripeApp
{
    public string ApiKey { get; set; } = string.Empty;
}

public interface IStripeService
{
    Task<TaxRate> CreateTaxRateAsync(TaxRateCreateOptions taxRateCreateOptions);
    Task<Customer> CreateCustomerAsync(CustomerCreateOptions customerCreateOptions);
    Task<Session> CreateCheckoutSessionAsync(SessionCreateOptions sessionCreateOptions);
    Task<Session> GetCheckoutSessionAsync(string sessionId);
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    Task<Charge> GetChargeAsync(string chargeId);
    Task<Customer> GetCustomerAsync(string customerId);
    Task<Refund> RefundPaymentIntentAsync(string paymentIntentId, decimal amount);
}

public class StripeService : IStripeService
{
    public readonly StripeClient StripeClient;

    public StripeService(StripeApp stripeApp)
    {
        if (string.IsNullOrEmpty(stripeApp.ApiKey))
            throw new ArgumentException("Stripe API key is not configured. Please set a valid API key.");

        StripeClient = new StripeClient(stripeApp.ApiKey);
    }

    public async Task<TaxRate> CreateTaxRateAsync(TaxRateCreateOptions taxRateCreateOptions)
    {
        TaxRateService taxRateService = new(StripeClient);
        TaxRate? taxRate = await taxRateService.CreateAsync(taxRateCreateOptions);
        return taxRate;
    }

    public async Task<Customer> CreateCustomerAsync(CustomerCreateOptions customerCreateOptions)
    {
        CustomerService customerService = new(StripeClient);
        Customer? customer = await customerService.CreateAsync(customerCreateOptions);
        return customer;
    }

    public async Task<Session> CreateCheckoutSessionAsync(SessionCreateOptions sessionCreateOptions)
    {
        SessionService sessionService = new(StripeClient);
        Session? session = await sessionService.CreateAsync(sessionCreateOptions);
        return session;
    }

    public async Task<Session> GetCheckoutSessionAsync(string sessionId)
    {
        SessionService sessionService = new(StripeClient);
        Session? session = await sessionService.GetAsync(sessionId);
        return session;
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        PaymentIntentService paymentIntentService = new(StripeClient);
        PaymentIntent? paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
        return paymentIntent;
    }

    public async Task<Charge> GetChargeAsync(string chargeId)
    {
        ChargeService chargeService = new(StripeClient);
        Charge? charge = await chargeService.GetAsync(chargeId);
        return charge;
    }

    public async Task<Customer> GetCustomerAsync(string customerId)
    {
        CustomerService customerService = new(StripeClient);
        Customer? customer = await customerService.GetAsync(customerId);
        return customer;
    }

    public async Task<Refund> RefundPaymentIntentAsync(string paymentIntentId, decimal amount)
    {
        RefundService refundService = new RefundService(StripeClient);
        RefundCreateOptions refundCreateOptions = new()
        {
            PaymentIntent = paymentIntentId,
            Amount = (long)(amount * 100)
        };
        Refund? refund = await refundService.CreateAsync(refundCreateOptions);
        return refund;
    }
}