using BookingCare.Services.Notification.Models;
using BookingCare.Services.Notification.Repositories;

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

    public async Task<Device?> GetByTokenAsync(string token)
    {
        return await _repository.GetByTokenAsync(token);
    }

    public async Task<bool> RemoveAsync(string id)
    {
        return await _repository.RemoveAsync(id);
    }

    public async Task<int> GetCountAsync()
    {
        return await _repository.GetCountAsync();
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

    public Device? Get(string id)
    {
        return GetAsync(id).Result;
    }

    public Device? GetByToken(string token)
    {
        return GetByTokenAsync(token).Result;
    }

    public bool Remove(string id)
    {
        return RemoveAsync(id).Result;
    }

    public int Count => GetCountAsync().Result;
}