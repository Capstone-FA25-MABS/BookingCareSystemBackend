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
    /// Collection chứa các tin nhắn
    /// </summary>
    public IMongoCollection<MessageEntity> Messages =>
        _database.GetCollection<MessageEntity>("Messages");

    /// <summary>
    /// Collection chứa các cuộc hội thoại
    /// </summary>
    public IMongoCollection<ConversationEntity> Conversations =>
        _database.GetCollection<ConversationEntity>("Conversations");

    /// <summary>
    /// Collection chứa lịch sử cuộc gọi
    /// </summary>
    public IMongoCollection<CallLogEntity> CallLogs =>
        _database.GetCollection<CallLogEntity>("CallLogs");
}