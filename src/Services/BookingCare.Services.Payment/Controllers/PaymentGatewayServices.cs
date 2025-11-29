using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Groups payment gateway services
/// Reduces constructor parameter count in PaymentsController
/// </summary>
public class PaymentGatewayServices
{
    public IPayOSService PayOSService { get; }
    public IVNPayService VNPayService { get; }
    public IStripeService StripeService { get; }

    public PaymentGatewayServices(
        IPayOSService payOSService,
        IVNPayService vnPayService,
        IStripeService stripeService
    )
    {
        PayOSService = payOSService;
        VNPayService = vnPayService;
        StripeService = stripeService;
    }
}
