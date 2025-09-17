using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BookingCare.Services.Notification.Setting;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Models.Entities;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Repositories.Implementations;

public class DeviceRepository : IDeviceRepository
{
    private readonly IMongoCollection<Device> _devices;

    public DeviceRepository(IOptions<MongoDbSettings> settings)
    {
        try
        {
            var mongoSettings = settings.Value;
            if (string.IsNullOrEmpty(mongoSettings.ConnectionString))
            {
                throw new DeviceException("MongoDB connection string is not configured");
            }
            if (string.IsNullOrEmpty(mongoSettings.DatabaseName))
            {
                throw new DeviceException("MongoDB database name is not configured");
            }
            if (string.IsNullOrEmpty(mongoSettings.DevicesCollectionName))
            {
                throw new DeviceException("MongoDB devices collection name is not configured");
            }

            var client = new MongoClient(mongoSettings.ConnectionString);
            var database = client.GetDatabase(mongoSettings.DatabaseName);
            _devices = database.GetCollection<Device>(mongoSettings.DevicesCollectionName);
        }
        catch (Exception ex)
        {
            throw new DeviceException("Failed to initialize DeviceRepository", "DEVICE_INITIALIZATION_ERROR", System.Net.HttpStatusCode.InternalServerError, ex);
        }
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
        // Validate input
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DeviceValidationException("DeviceName", "Device name cannot be null or empty", name);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DeviceValidationException("Token", "Device token cannot be null or empty", token);
        }

        try
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
        catch (Exception ex)
        {
            throw new DeviceRegistrationException($"Failed to add or update device '{name}' with token '{token}'", ex);
        }
    }

    public async Task UpdateLastUsedAsync(string id)
    {
        var update = Builders<Device>.Update.Set(d => d.LastUsedAt, DateTime.UtcNow);
        await _devices.UpdateOneAsync(d => d.Id == id, update);
    }
}