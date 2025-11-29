using BookingCare.Services.Appointment.Configuration;
using BookingCare.Services.Appointment.Helpers;
using BookingCare.Shared.Common.Helpers;
using Microsoft.Extensions.Options;
using RedisClient = StackExchange.Redis;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Encapsulates infrastructure dependencies for AppointmentService
/// to reduce constructor parameter count
/// </summary>
public class AppointmentServiceDependencies
{
    public GrpcClientWrapper GrpcClients { get; }
    public RedisClient.IConnectionMultiplexer RedisConnection { get; }
    public IHttpContextAccessor HttpContextAccessor { get; }
    public FrontendConfiguration FrontendConfig { get; }

    public AppointmentServiceDependencies(
        GrpcClientWrapper grpcClients,
        RedisClient.IConnectionMultiplexer redisConnection,
        IHttpContextAccessor httpContextAccessor,
        IOptions<FrontendConfiguration> frontendConfig
    )
    {
        GrpcClients = grpcClients;
        RedisConnection = redisConnection;
        HttpContextAccessor = httpContextAccessor;
        FrontendConfig = frontendConfig.Value;
    }
}
