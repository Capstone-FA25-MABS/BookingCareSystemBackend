using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Data.Seeding;

/// <summary>
/// Class để seed dữ liệu mẫu cho Communication database
/// </summary>
public static class CommunicationDataSeeder
{
    /// <summary>
    /// Seed dữ liệu mẫu vào database
    /// </summary>
    public static async Task SeedAsync(CommunicationDbContext context)
    {
        await SeedConversationsAsync(context);
        await SeedMessagesAsync(context);
        await SeedCallLogsAsync(context);
    }

    /// <summary>
    /// Seed dữ liệu Conversations mẫu
    /// </summary>
    private static async Task SeedConversationsAsync(CommunicationDbContext context)
    {
        // Kiểm tra xem đã có dữ liệu chưa
        var existingCount = await context.Conversations.CountDocumentsAsync(FilterDefinition<ConversationEntity>.Empty);
        if (existingCount > 0)
        {
            return; // Đã có dữ liệu, không cần seed
        }

        var conversations = new List<ConversationEntity>
        {
            new ConversationEntity
            {
                Participants = new List<string> { "user1", "user2" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new ConversationEntity
            {
                Participants = new List<string> { "user1", "user3" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new ConversationEntity
            {
                Participants = new List<string> { "user2", "user3" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        await context.Conversations.InsertManyAsync(conversations);
    }

    /// <summary>
    /// Seed dữ liệu Messages mẫu
    /// </summary>
    private static async Task SeedMessagesAsync(CommunicationDbContext context)
    {
        // Kiểm tra xem đã có dữ liệu chưa
        var existingCount = await context.Messages.CountDocumentsAsync(FilterDefinition<MessageEntity>.Empty);
        if (existingCount > 0)
        {
            return; // Đã có dữ liệu, không cần seed
        }

        // Lấy conversations để tạo messages
        var conversations = await context.Conversations.Find(FilterDefinition<ConversationEntity>.Empty).ToListAsync();
        if (!conversations.Any())
        {
            return;
        }

        var messages = new List<MessageEntity>();

        foreach (var conversation in conversations.Take(2)) // Chỉ tạo messages cho 2 conversation đầu
        {
            var participants = conversation.Participants;
            if (participants.Count >= 2)
            {
                messages.AddRange(new[]
                {
                    new MessageEntity
                    {
                        ConversationId = conversation.Id,
                        SenderId = participants[0],
                        ReceiverId = participants[1],
                        Content = "Xin chào! Bạn khỏe không?",
                        Type = MessageType.Text,
                        Status = MessageStatus.READ,
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        UpdatedAt = DateTime.UtcNow.AddHours(-2),
                        ReadAt = DateTime.UtcNow.AddHours(-1)
                    },
                    new MessageEntity
                    {
                        ConversationId = conversation.Id,
                        SenderId = participants[1],
                        ReceiverId = participants[0],
                        Content = "Chào bạn! Mình khỏe, cảm ơn bạn.",
                        Type = MessageType.Text,
                        Status = MessageStatus.READ,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        UpdatedAt = DateTime.UtcNow.AddHours(-1),
                        ReadAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new MessageEntity
                    {
                        ConversationId = conversation.Id,
                        SenderId = participants[0],
                        ReceiverId = participants[1],
                        Content = "Hôm nay bạn có rảnh không? Mình muốn hẹn bạn.",
                        Type = MessageType.Text,
                        Status = MessageStatus.UNREAD,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
                    }
                });
            }
        }

        if (messages.Any())
        {
            await context.Messages.InsertManyAsync(messages);

            // Cập nhật lastMessage cho conversations
            foreach (var conversation in conversations.Take(2))
            {
                var lastMessage = messages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                if (lastMessage != null)
                {
                    var lastMessageInfo = new LastMessage
                    {
                        MessageId = lastMessage.Id,
                        Content = lastMessage.Content,
                        SenderId = lastMessage.SenderId,
                        CreatedAt = lastMessage.CreatedAt
                    };

                    await context.Conversations.UpdateOneAsync(
                        c => c.Id == conversation.Id,
                        Builders<ConversationEntity>.Update
                            .Set(c => c.LastMessage, lastMessageInfo)
                            .Set(c => c.UpdatedAt, DateTime.UtcNow)
                    );
                }
            }
        }
    }

    /// <summary>
    /// Seed dữ liệu CallLogs mẫu
    /// </summary>
    private static async Task SeedCallLogsAsync(CommunicationDbContext context)
    {
        // Kiểm tra xem đã có dữ liệu chưa
        var existingCount = await context.CallLogs.CountDocumentsAsync(FilterDefinition<CallLogEntity>.Empty);
        if (existingCount > 0)
        {
            return; // Đã có dữ liệu, không cần seed
        }

        // Lấy conversations để tạo call logs
        var conversations = await context.Conversations.Find(FilterDefinition<ConversationEntity>.Empty).ToListAsync();
        if (!conversations.Any())
        {
            return;
        }

        var callLogs = new List<CallLogEntity>();

        foreach (var conversation in conversations.Take(2))
        {
            var participants = conversation.Participants;
            if (participants.Count >= 2)
            {
                callLogs.AddRange(new[]
                {
                    new CallLogEntity
                    {
                        ConversationId = conversation.Id,
                        CallerId = participants[0],
                        ReceiverId = participants[1],
                        Duration = 15,
                        Type = CallType.Video,
                        Status = CallStatus.Accepted,
                        StartedAt = DateTime.UtcNow.AddDays(-2),
                        EndedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(15)
                    },
                    new CallLogEntity
                    {
                        ConversationId = conversation.Id,
                        CallerId = participants[1],
                        ReceiverId = participants[0],
                        Duration = 0,
                        Type = CallType.Audio,
                        Status = CallStatus.Missed,
                        StartedAt = DateTime.UtcNow.AddHours(-3),
                        EndedAt = null
                    },
                    new CallLogEntity
                    {
                        ConversationId = conversation.Id,
                        CallerId = participants[0],
                        ReceiverId = participants[1],
                        Duration = 8,
                        Type = CallType.Audio,
                        Status = CallStatus.Accepted,
                        StartedAt = DateTime.UtcNow.AddHours(-1),
                        EndedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(8)
                    }
                });
            }
        }

        if (callLogs.Any())
        {
            await context.CallLogs.InsertManyAsync(callLogs);
        }
    }

    /// <summary>
    /// Xóa tất cả dữ liệu (chỉ dùng trong development)
    /// </summary>
    public static async Task ClearAllDataAsync(CommunicationDbContext context)
    {
        await context.Messages.DeleteManyAsync(_ => true);
        await context.Conversations.DeleteManyAsync(_ => true);
        await context.CallLogs.DeleteManyAsync(_ => true);
    }
}