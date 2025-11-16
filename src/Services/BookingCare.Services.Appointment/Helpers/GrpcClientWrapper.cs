using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Payment.Protos;

namespace BookingCare.Services.Appointment.Helpers;

/// <summary>
/// Wrapper class for gRPC clients to reduce constructor parameters
/// </summary>
public class GrpcClientWrapper
{
    public DoctorService.DoctorServiceClient DoctorClient { get; }
    public HospitalService.HospitalServiceClient HospitalClient { get; }
    public SubscriptionUsageGrpc.SubscriptionUsageGrpcClient SubscriptionUsageClient { get; }
    public UserService.UserServiceClient UserClient { get; }
    public PaymentService.PaymentServiceClient PaymentClient { get; }

    public GrpcClientWrapper(
        DoctorService.DoctorServiceClient doctorClient,
        HospitalService.HospitalServiceClient hospitalClient,
        SubscriptionUsageGrpc.SubscriptionUsageGrpcClient subscriptionUsageClient,
        UserService.UserServiceClient userClient,
        PaymentService.PaymentServiceClient paymentClient)
    {
        DoctorClient = doctorClient;
        HospitalClient = hospitalClient;
        SubscriptionUsageClient = subscriptionUsageClient;
        UserClient = userClient;
        PaymentClient = paymentClient;
    }
}
