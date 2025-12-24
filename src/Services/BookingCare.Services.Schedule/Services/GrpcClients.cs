using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.ServiceMedical.Protos;
using BookingCare.Services.Appointment.Protos;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Wrapper class for gRPC clients to reduce constructor parameter count
/// </summary>
public class GrpcClients
{
    public DoctorService.DoctorServiceClient DoctorClient { get; }
    public ServiceMedicalService.ServiceMedicalServiceClient ServiceMedicalClient { get; }
    public AppointmentService.AppointmentServiceClient AppointmentClient { get; }

    public GrpcClients(
        DoctorService.DoctorServiceClient doctorClient,
        ServiceMedicalService.ServiceMedicalServiceClient serviceMedicalClient,
        AppointmentService.AppointmentServiceClient appointmentClient)
    {
        DoctorClient = doctorClient;
        ServiceMedicalClient = serviceMedicalClient;
        AppointmentClient = appointmentClient;
    }
}
