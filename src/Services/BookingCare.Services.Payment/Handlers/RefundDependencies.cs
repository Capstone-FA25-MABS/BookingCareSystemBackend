using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Handlers;

/// <summary>
/// Groups repository dependencies for refund operations
/// Reduces constructor parameter count in AppointmentCancelledEventHandler
/// </summary>
public class RefundDependencies
{
    public IPaymentRepository PaymentRepository { get; }
    public IPaymentMethodRepository PaymentMethodRepository { get; }
    public IBankAccountRepository BankAccountRepository { get; }

    public RefundDependencies(
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IBankAccountRepository bankAccountRepository
    )
    {
        PaymentRepository = paymentRepository;
        PaymentMethodRepository = paymentMethodRepository;
        BankAccountRepository = bankAccountRepository;
    }
}
