using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using BookingCare.Services.Notification.Setting;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Models.Entities;

namespace BookingCare.Services.Notification.Repositories.Implementations;

public class DeviceRepository : IDeviceRepository
{
    private readonly IMongoCollection<Device> _devices;

    public DeviceRepository(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings = settings.Value;
        var client = new MongoClient(mongoSettings.ConnectionString);
        var database = client.GetDatabase(mongoSettings.DatabaseName);
        _devices = database.GetCollection<Device>(mongoSettings.DevicesCollectionName);
    }

    public async Task<IEnumerable<Device>> GetAllAsync()
    {
        return await _devices
            .Find(d => d.IsActive)
            .SortByDescending(d => d.RegisteredAt)
            .ToListAsync();
    }

    public async Task<Device?> GetByIdAsync(string id)
    {
        return await _devices
            .Find(d => d.Id == id && d.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<Device> AddOrUpdateAsync(string name, string token)
    {
        var existingDevice = await _devices
            .Find(d => d.Token == token)
            .FirstOrDefaultAsync();

        if (existingDevice != null)
        {
            // Update existing device
            var update = Builders<Device>.Update
                .Set(d => d.Name, name)
                .Set(d => d.RegisteredAt, DateTime.UtcNow)
                .Set(d => d.IsActive, true);

            await _devices.UpdateOneAsync(d => d.Id == existingDevice.Id, update);
            existingDevice.Name = name;
            existingDevice.RegisteredAt = DateTime.UtcNow;
            existingDevice.IsActive = true;
            return existingDevice;
        }
        else
        {
            // Create new device
            var newDevice = new Device
            {
                Name = name,
                Token = token,
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            };

            await _devices.InsertOneAsync(newDevice);
            return newDevice;
        }
    }

    public async Task UpdateLastUsedAsync(string id)
    {
        var update = Builders<Device>.Update.Set(d => d.LastUsedAt, DateTime.UtcNow);
        await _devices.UpdateOneAsync(d => d.Id == id, update);
    }
}