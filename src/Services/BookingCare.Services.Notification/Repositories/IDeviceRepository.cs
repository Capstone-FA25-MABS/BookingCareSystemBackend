using BookingCare.Services.Notification.Models;

namespace BookingCare.Services.Notification.Repositories;

public interface IDeviceRepository
{
    Task<IEnumerable<Device>> GetAllAsync();
    Task<Device?> GetByIdAsync(string id);
    Task<Device?> GetByTokenAsync(string token);
    Task<Device> AddOrUpdateAsync(string name, string token);
    Task<bool> RemoveAsync(string id);
    Task<int> GetCountAsync();
    Task UpdateLastUsedAsync(string id);
}