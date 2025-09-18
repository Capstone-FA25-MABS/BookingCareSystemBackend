using BookingCare.Services.Notification.Models.Entities;
using BookingCare.Services.Notification.Repositories.Interfaces;

namespace BookingCare.Services.Notification.Utils.SMS;

public class DeviceStore
{
    private readonly IDeviceRepository _repository;

    public DeviceStore(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Device>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Device> AddOrUpdateAsync(string name, string token)
    {
        return await _repository.AddOrUpdateAsync(name, token);
    }

    public async Task<Device?> GetAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task UpdateLastUsedAsync(string id)
    {
        await _repository.UpdateLastUsedAsync(id);
    }

    // Legacy sync methods for backward compatibility
    public IReadOnlyList<Device> All => GetAllAsync().Result.ToList().AsReadOnly();

    public Device AddOrUpdate(string name, string token)
    {
        return AddOrUpdateAsync(name, token).Result;
    }

}