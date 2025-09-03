using MongoDB.Driver;
using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Data;

/// <summary>
/// MongoDB context cho Communication service
/// </summary>
public class CommunicationDbContext
{
    private readonly IMongoDatabase _database;

    public CommunicationDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Collection ch?a các tin nh?n
    /// </summary>
    public IMongoCollection<MessageEntity> Messages => 
        _database.GetCollection<MessageEntity>("Messages");

    /// <summary>
    /// Collection ch?a các cu?c h?i tho?i
    /// </summary>
    public IMongoCollection<ConversationEntity> Conversations => 
        _database.GetCollection<ConversationEntity>("Conversations");

    /// <summary>
    /// Collection ch?a l?ch s? cu?c g?i
    /// </summary>
    public IMongoCollection<CallLogEntity> CallLogs => 
        _database.GetCollection<CallLogEntity>("CallLogs");
}