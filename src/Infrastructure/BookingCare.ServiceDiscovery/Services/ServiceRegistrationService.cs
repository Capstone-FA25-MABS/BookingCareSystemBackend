using Consul;

namespace BookingCare.ServiceDiscovery.Services;

public interface IServiceRegistrationService
{
    Task<bool> RegisterServiceAsync(string serviceName, string serviceId, string address, int port, string[] tags);
    Task<bool> DeregisterServiceAsync(string serviceId);
    Task<IEnumerable<ServiceEntry>> GetHealthyServicesAsync(string serviceName);
    Task<IEnumerable<ServiceEntry>> GetAllServicesAsync();
}

public class ServiceRegistrationService : IServiceRegistrationService
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ServiceRegistrationService> _logger;

    public ServiceRegistrationService(IConsulClient consulClient, ILogger<ServiceRegistrationService> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<bool> RegisterServiceAsync(string serviceName, string serviceId, string address, int port, string[] tags)
    {
        try
        {
            var registration = new AgentServiceRegistration
            {
                ID = serviceId,
                Name = serviceName,
                Address = address,
                Port = port,
                Tags = tags,
                Check = new AgentServiceCheck
                {
                    HTTP = $"https://{address}:{port}/health",
                    Interval = TimeSpan.FromSeconds(30),
                    Timeout = TimeSpan.FromSeconds(10),
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(5)
                }
            };

            var result = await _consulClient.Agent.ServiceRegister(registration);
            _logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered successfully", serviceName, serviceId);
            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName} with ID {ServiceId}", serviceName, serviceId);
            return false;
        }
    }

    public async Task<bool> DeregisterServiceAsync(string serviceId)
    {
        try
        {
            var result = await _consulClient.Agent.ServiceDeregister(serviceId);
            _logger.LogInformation("Service with ID {ServiceId} deregistered successfully", serviceId);
            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service with ID {ServiceId}", serviceId);
            return false;
        }
    }

    public async Task<IEnumerable<ServiceEntry>> GetHealthyServicesAsync(string serviceName)
    {
        try
        {
            var services = await _consulClient.Health.Service(serviceName, string.Empty, true);
            return services.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get healthy services for {ServiceName}", serviceName);
            return Enumerable.Empty<ServiceEntry>();
        }
    }

    public async Task<IEnumerable<ServiceEntry>> GetAllServicesAsync()
    {
        try
        {
            var services = await _consulClient.Agent.Services();
            return services.Response.Values.Select(s => new ServiceEntry
            {
                Service = new AgentService
                {
                    ID = s.ID,
                    Service = s.Service,
                    Address = s.Address,
                    Port = s.Port,
                    Tags = s.Tags
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all services");
            return Enumerable.Empty<ServiceEntry>();
        }
    }
}
