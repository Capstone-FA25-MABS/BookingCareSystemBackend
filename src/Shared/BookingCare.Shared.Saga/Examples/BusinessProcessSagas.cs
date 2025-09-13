using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Shared.Saga.Examples;

/// <summary>
/// Saga for user registration process
/// Coordinates: User Service, Auth Service, Notification Service, Analytics Service
/// </summary>
public class UserRegistrationSaga : SagaDefinitionBase
{
    public override string SagaName => "UserRegistration";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(10);

    public UserRegistrationSaga()
    {
        AddStep<ValidateRegistrationDataStep>();
        AddStep<CreateUserAccountStep>();
        AddStep<GenerateAuthCredentialsStep>();
        AddStep<SendWelcomeEmailStep>();
        AddStep<TrackUserRegistrationStep>();
    }
}

/// <summary>
/// Saga for order processing with payment and fulfillment
/// Coordinates: Order Service, Payment Service, Inventory Service, Shipping Service, Notification Service
/// </summary>
public class OrderProcessingSaga : SagaDefinitionBase
{
    public override string SagaName => "OrderProcessing";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(20);

    public OrderProcessingSaga()
    {
        AddStep<ValidateOrderStep>();
        AddStep<ReserveInventoryStep>();
        AddStep<ProcessOrderPaymentStep>();
        AddStep<CreateShipmentStep>();
        AddStep<SendOrderConfirmationStep>();
        AddStep<UpdateInventoryStep>();
    }
}

// User Registration Saga Steps
public class ValidateRegistrationDataStep : CompensatableSagaStepBase
{
    public override string StepName => "ValidateRegistrationData";
    public override int Order => 1;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var email = context.GetData<string>("Email");
        var password = context.GetData<string>("Password");
        var fullName = context.GetData<string>("FullName");

        await Task.Delay(100, cancellationToken);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName))
        {
            return Failure("Missing required registration fields");
        }

        if (!IsValidEmail(email))
        {
            return Failure("Invalid email format");
        }

        if (password.Length < 8)
        {
            return Failure("Password must be at least 8 characters long");
        }

        context.SetData("ValidationPassed", true);
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for validation
        await Task.Delay(1, cancellationToken);
        return Success();
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains('@') && email.Contains('.');
    }
}

public class CreateUserAccountStep : CompensatableSagaStepBase
{
    public override string StepName => "CreateUserAccount";
    public override int Order => 2;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var email = context.GetData<string>("Email");
        var fullName = context.GetData<string>("FullName");
        var phoneNumber = context.GetData<string>("PhoneNumber");

        await Task.Delay(300, cancellationToken);

        // Check if user already exists
        var userExists = DateTime.UtcNow.Millisecond % 10 == 0; // Simulate 10% chance user exists

        if (userExists)
        {
            return Failure("User with this email already exists");
        }

        var userId = Guid.NewGuid();
        context.SetData("UserId", userId.ToString());
        context.SetData("UserCreated", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.GetData<string>("UserId");

        if (!string.IsNullOrEmpty(userId))
        {
            // Delete the user account
            await Task.Delay(200, cancellationToken);
            context.SetData("UserDeleted", true);
        }

        return Success();
    }
}

public class GenerateAuthCredentialsStep : CompensatableSagaStepBase
{
    public override string StepName => "GenerateAuthCredentials";
    public override int Order => 3;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.GetData<string>("UserId");
        var password = context.GetData<string>("Password");

        if (string.IsNullOrEmpty(password))
        {
            return SagaStepResult.Failure("Password is required for auth credential generation");
        }

        await Task.Delay(200, cancellationToken);

        // Generate password hash and auth tokens
        var passwordHash = HashPassword(password);
        var activationToken = Guid.NewGuid().ToString();

        context.SetData("PasswordHash", passwordHash);
        context.SetData("ActivationToken", activationToken);
        context.SetData("AuthCredentialsGenerated", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.GetData<string>("UserId");

        if (!string.IsNullOrEmpty(userId))
        {
            // Revoke auth credentials
            await Task.Delay(100, cancellationToken);
            context.SetData("AuthCredentialsRevoked", true);
        }

        return Success();
    }

    private string HashPassword(string password)
    {
        // Simulate password hashing
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));
    }
}

public class SendWelcomeEmailStep : ExecutableSagaStepBase
{
    public override string StepName => "SendWelcomeEmail";
    public override int Order => 4;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var email = context.GetData<string>("Email");
        var fullName = context.GetData<string>("FullName");
        var activationToken = context.GetData<string>("ActivationToken");

        await Task.Delay(150, cancellationToken);

        // Send welcome email with activation link
        context.SetData("WelcomeEmailSent", true);
        context.SetData("ActivationEmailSent", true);

        return Success();
    }
}

public class TrackUserRegistrationStep : ExecutableSagaStepBase
{
    public override string StepName => "TrackUserRegistration";
    public override int Order => 5;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.GetData<string>("UserId");
        var registrationSource = context.GetData<string>("RegistrationSource") ?? "web";

        await Task.Delay(50, cancellationToken);

        // Track registration analytics
        context.SetData("RegistrationTracked", true);

        return Success();
    }
}

// Order Processing Saga Steps
public class ValidateOrderStep : CompensatableSagaStepBase
{
    public override string StepName => "ValidateOrder";
    public override int Order => 1;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var customerId = context.GetData<string>("CustomerId");
        var items = context.GetData<List<OrderItem>>("Items");
        var totalAmount = context.GetData<decimal>("TotalAmount");

        await Task.Delay(100, cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            return Failure("Customer ID is required");
        }

        if (items == null || !items.Any())
        {
            return Failure("Order must contain at least one item");
        }

        if (totalAmount <= 0)
        {
            return Failure("Order total must be greater than zero");
        }

        context.SetData("OrderValidated", true);
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for validation
        await Task.Delay(1, cancellationToken);
        return Success();
    }
}

public class ReserveInventoryStep : CompensatableSagaStepBase
{
    public override string StepName => "ReserveInventory";
    public override int Order => 2;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var items = context.GetData<List<OrderItem>>("Items");

        await Task.Delay(250, cancellationToken);

        var reservationId = Guid.NewGuid();
        var reservedItems = new List<string>();

        foreach (var item in items!)
        {
            // Check inventory availability
            var isAvailable = DateTime.UtcNow.Millisecond % 5 != 0; // 80% availability chance

            if (!isAvailable)
            {
                return Failure($"Item {item.ProductId} is out of stock");
            }

            reservedItems.Add(item.ProductId);
        }

        context.SetData("ReservationId", reservationId.ToString());
        context.SetData("ReservedItems", reservedItems);
        context.SetData("InventoryReserved", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var reservationId = context.GetData<string>("ReservationId");

        if (!string.IsNullOrEmpty(reservationId))
        {
            // Release inventory reservation
            await Task.Delay(150, cancellationToken);
            context.SetData("InventoryReleased", true);
        }

        return Success();
    }
}

public class ProcessOrderPaymentStep : CompensatableSagaStepBase
{
    public override string StepName => "ProcessOrderPayment";
    public override int Order => 3;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var customerId = context.GetData<string>("CustomerId");
        var totalAmount = context.GetData<decimal>("TotalAmount");
        var paymentMethod = context.GetData<string>("PaymentMethod");

        await Task.Delay(400, cancellationToken);

        // Process payment
        var paymentSuccessful = DateTime.UtcNow.Millisecond % 4 != 0; // 75% success rate

        if (!paymentSuccessful)
        {
            return Failure("Payment processing failed", shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }

        var transactionId = Guid.NewGuid();
        context.SetData("TransactionId", transactionId.ToString());
        context.SetData("PaymentProcessed", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var transactionId = context.GetData<string>("TransactionId");

        if (!string.IsNullOrEmpty(transactionId))
        {
            // Refund payment
            await Task.Delay(300, cancellationToken);
            context.SetData("PaymentRefunded", true);
        }

        return Success();
    }
}

public class CreateShipmentStep : CompensatableSagaStepBase
{
    public override string StepName => "CreateShipment";
    public override int Order => 4;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var customerId = context.GetData<string>("CustomerId");
        var items = context.GetData<List<OrderItem>>("Items");
        var shippingAddress = context.GetData<string>("ShippingAddress");

        await Task.Delay(200, cancellationToken);

        var shipmentId = Guid.NewGuid();
        var trackingNumber = $"TRK{DateTime.UtcNow:yyyyMMddHHmmss}";

        context.SetData("ShipmentId", shipmentId.ToString());
        context.SetData("TrackingNumber", trackingNumber);
        context.SetData("ShipmentCreated", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var shipmentId = context.GetData<string>("ShipmentId");

        if (!string.IsNullOrEmpty(shipmentId))
        {
            // Cancel shipment
            await Task.Delay(100, cancellationToken);
            context.SetData("ShipmentCancelled", true);
        }

        return Success();
    }
}

public class SendOrderConfirmationStep : ExecutableSagaStepBase
{
    public override string StepName => "SendOrderConfirmation";
    public override int Order => 5;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var customerId = context.GetData<string>("CustomerId");
        var orderId = context.GetData<string>("OrderId");
        var trackingNumber = context.GetData<string>("TrackingNumber");

        await Task.Delay(100, cancellationToken);

        // Send order confirmation email
        context.SetData("OrderConfirmationSent", true);

        return Success();
    }
}

public class UpdateInventoryStep : ExecutableSagaStepBase
{
    public override string StepName => "UpdateInventory";
    public override int Order => 6;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var reservedItems = context.GetData<List<string>>("ReservedItems");

        await Task.Delay(100, cancellationToken);

        // Update actual inventory levels
        context.SetData("InventoryUpdated", true);

        return Success();
    }
}

// Supporting classes
public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// Events for the sagas
public class UserRegistrationRequestedEvent : IntegrationEvent
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RegistrationSource { get; set; } = "web";
}

public class OrderPlacedEvent : IntegrationEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
}
