using BookingCare.Services.Notification.Models.Entities;

namespace BookingCare.Services.Notification.Repositories.Interfaces;

public interface IDeviceRepository
{
	Task<IEnumerable<Device>> GetAllAsync();
	Task<Device?> GetByIdAsync(string id);
	Task<Device> AddOrUpdateAsync(string name, string token);
	Task UpdateLastUsedAsync(string id);
}