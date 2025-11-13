using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using MongoDB.Driver;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation của Tag repository sử dụng MongoDB
/// </summary>
public class TagRepository : ITagRepository
{
    private readonly IMongoCollection<TagEntity> _tags;

    public TagRepository(CommunicationDbContext context)
    {
        _tags = context.Tags;
    }

    /// <summary>
    /// Lấy tag theo ID
    /// </summary>
    public async Task<TagEntity?> GetByIdAsync(string id)
    {
        return await _tags.Find(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Lấy danh sách tag của user
    /// </summary>
    public async Task<IEnumerable<TagEntity>> GetByUserIdAsync(string userId)
    {
        return await _tags
            .Find(t => t.UserId == userId && t.IsActive)
            .SortByDescending(t => t.IsPinned)
            .ThenBy(t => t.Order)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tất cả system tags
    /// </summary>
    public async Task<IEnumerable<TagEntity>> GetSystemTagsAsync()
    {
        return await _tags
            .Find(t => t.UserId == null && t.IsActive)
            .SortByDescending(t => t.IsPinned)
            .ThenBy(t => t.Order)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tag theo tên (của một user cụ thể)
    /// </summary>
    public async Task<TagEntity?> GetByNameAsync(string userId, string name)
    {
        return await _tags
            .Find(t => t.UserId == userId && t.Name == name && t.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Lấy nhiều tag theo danh sách IDs
    /// </summary>
    public async Task<IEnumerable<TagEntity>> GetByIdsAsync(List<string> ids)
    {
        return await _tags.Find(t => ids.Contains(t.Id) && t.IsActive).ToListAsync();
    }

    /// <summary>
    /// Tạo tag mới
    /// </summary>
    public async Task<TagEntity> CreateAsync(TagEntity tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;
        await _tags.InsertOneAsync(tag);
        return tag;
    }

    /// <summary>
    /// Cập nhật tag
    /// </summary>
    public async Task<TagEntity?> UpdateAsync(string id, TagEntity tag)
    {
        tag.UpdatedAt = DateTime.UtcNow;
        var options = new FindOneAndReplaceOptions<TagEntity, TagEntity>
        {
            ReturnDocument = ReturnDocument.After,
        };
        var result = await _tags.FindOneAndReplaceAsync<TagEntity, TagEntity>(
            t => t.Id == id,
            tag,
            options
        );
        return result;
    }

    /// <summary>
    /// Xóa tag (soft delete)
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<TagEntity>
            .Update.Set(t => t.IsActive, false)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _tags.UpdateOneAsync(t => t.Id == id, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Tăng số lượng cuộc hội thoại sử dụng tag
    /// </summary>
    public async Task<bool> IncrementConversationCountAsync(string tagId, int increment = 1)
    {
        var update = Builders<TagEntity>
            .Update.Inc(t => t.ConversationCount, increment)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _tags.UpdateOneAsync(t => t.Id == tagId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Giảm số lượng cuộc hội thoại sử dụng tag
    /// </summary>
    public async Task<bool> DecrementConversationCountAsync(string tagId, int decrement = 1)
    {
        var update = Builders<TagEntity>
            .Update.Inc(t => t.ConversationCount, -decrement)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _tags.UpdateOneAsync(
            t => t.Id == tagId && t.ConversationCount >= decrement,
            update
        );
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Kiểm tra tag có tồn tại không
    /// </summary>
    public async Task<bool> ExistsAsync(string id)
    {
        return await _tags.Find(t => t.Id == id && t.IsActive).AnyAsync();
    }

    /// <summary>
    /// Kiểm tra tag có thuộc về user không
    /// </summary>
    public async Task<bool> IsTagOwnedByUserAsync(string tagId, string userId)
    {
        return await _tags.Find(t => t.Id == tagId && t.UserId == userId && t.IsActive).AnyAsync();
    }

    /// <summary>
    /// Lấy danh sách tag được sắp xếp theo thứ tự và pinned
    /// </summary>
    public async Task<IEnumerable<TagEntity>> GetSortedTagsAsync(
        string userId,
        bool includeSystem = true
    )
    {
        var filterBuilder = Builders<TagEntity>.Filter;
        var filters = new List<FilterDefinition<TagEntity>>();

        // Only get tags owned by this user OR system tags (userId is null)
        if (includeSystem)
        {
            filters.Add(
                filterBuilder.Or(
                    filterBuilder.Eq(t => t.UserId, userId),
                    filterBuilder.Eq(t => t.UserId, null)
                )
            );
        }
        else
        {
            filters.Add(filterBuilder.Eq(t => t.UserId, userId));
        }

        // Only active tags
        filters.Add(filterBuilder.Eq(t => t.IsActive, true));

        var filter = filterBuilder.And(filters);

        return await _tags
            .Find(filter)
            .SortByDescending(t => t.IsPinned)
            .ThenBy(t => t.Order)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}
