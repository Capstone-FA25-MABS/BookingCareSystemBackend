using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Handlers;

/// <summary>
/// Groups service dependencies for refund processing
/// Reduces constructor parameter count in AppointmentCancelledEventHandler
/// </summary>
public class RefundProcessors
{
    public IRefundHistoryService RefundHistoryService { get; }
    public IStripeService StripeService { get; }
    public IPaymentService PaymentService { get; }

    public RefundProcessors(
        IRefundHistoryService refundHistoryService,
        IStripeService stripeService,
        IPaymentService paymentService
    )
    {
        RefundHistoryService = refundHistoryService;
        StripeService = stripeService;
        PaymentService = paymentService;
    }
}
