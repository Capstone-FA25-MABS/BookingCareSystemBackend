using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Auth.Hubs;

/// <summary>
/// SignalR Hub for real-time account notifications (ban, lock, etc.)
/// Note: [Authorize] is removed to allow long-lived connections without JWT token expiration issues.
/// Authentication is still checked manually in OnConnectedAsync.
/// </summary>
public class AccountNotificationHub : Hub
{
    // Store connection mappings: userId -> List of connectionIds
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
    private readonly ILogger<AccountNotificationHub> _logger;

    public AccountNotificationHub(ILogger<AccountNotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Manual authentication check (since [Authorize] attribute is removed for long-lived connections)
        if (!Context.User?.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("[AccountNotificationHub] Connection rejected: User not authenticated");
            Context.Abort();
            return;
        }

        var userId = GetUserId(Context.User);
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("[AccountNotificationHub] Connection rejected: Invalid user ID");
            Context.Abort();
            return;
        }

        var connectionId = Context.ConnectionId;

        // Add connection to dictionary
        _connections.AddOrUpdate(
            userId,
            new HashSet<string> { connectionId },
            (key, existingSet) =>
            {
                existingSet.Add(connectionId);
                return existingSet;
            });

        _logger.LogInformation("[AccountNotificationHub] User {UserId} connected with connectionId {ConnectionId}", userId, connectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId(Context.User);
        if (userId != Guid.Empty)
        {
            var connectionId = Context.ConnectionId;

            // Remove connection from dictionary
            if (_connections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                }
            }

            _logger.LogInformation("[AccountNotificationHub] User {UserId} disconnected with connectionId {ConnectionId}", userId, connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get all connection IDs for a specific user
    /// </summary>
    public static IEnumerable<string> GetConnectionIds(Guid userId)
    {
        return _connections.TryGetValue(userId, out var connections)
            ? connections.ToList()
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Check if user is currently connected
    /// </summary>
    public static bool IsUserConnected(Guid userId)
    {
        return _connections.ContainsKey(userId);
    }

    private static Guid GetUserId(ClaimsPrincipal? user)
    {
        if (user == null) return Guid.Empty;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}

