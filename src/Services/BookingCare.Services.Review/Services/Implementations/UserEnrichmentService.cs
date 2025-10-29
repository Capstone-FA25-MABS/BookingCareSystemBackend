using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Services;
using Grpc.Core;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service for enriching user information from User service
/// </summary>
public class UserEnrichmentService : BaseService, IUserEnrichmentService
{
    private readonly UserService.UserServiceClient _userClient;

    public UserEnrichmentService(UserService.UserServiceClient userClient, ILogger<UserEnrichmentService> logger)
        : base(logger)
    {
        _userClient = userClient;
    }

    /// <summary>
    /// Gets user information for multiple user IDs (for patients who write reviews)
    /// </summary>
    /// <param name="userIds">List of user IDs to fetch</param>
    /// <returns>Dictionary mapping user ID to UserInfo</returns>
    public async Task<Dictionary<string, UserInfo>> GetUsersInfoAsync(List<string> userIds)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (!userIds.Any())
            {
                LogInfo("No user IDs provided for enrichment", null);
                return new Dictionary<string, UserInfo>();
            }

            var uniqueUserIds = GetUniqueUserIds(userIds);
            LogInfo("Fetching user display info for {Count} users from UserService (consistent format)", null, uniqueUserIds.Count);

            var request = new GetUsersDisplayInfoRequest();
            request.Ids.AddRange(uniqueUserIds);

            try
            {
                var response = await _userClient.GetUsersDisplayInfoAsync(request);
                return ProcessUserServiceResponse(response, uniqueUserIds);
            }
            catch (RpcException ex)
            {
                return HandleGrpcException(ex, uniqueUserIds);
            }

        }, "GetUsersInfo");
    }

    /// <summary>
    /// Filters and removes duplicates from user IDs
    /// </summary>
    private static List<string> GetUniqueUserIds(List<string> userIds)
    {
        return userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
    }

    /// <summary>
    /// Processes the response from User service and creates user info dictionary
    /// </summary>
    private Dictionary<string, UserInfo> ProcessUserServiceResponse(
        UsersDisplayInfoResponse response,
        List<string> uniqueUserIds)
    {
        var userInfoDict = MapFoundUsers(response.Users);
        AddMissingUsers(userInfoDict, uniqueUserIds);

        LogInfo("Successfully enriched {Found}/{Total} user details from UserService (consistent format)",
            null, userInfoDict.Values.Count(u => u.Found), uniqueUserIds.Count);

        return userInfoDict;
    }

    /// <summary>
    /// Maps found user details to UserInfo objects
    /// </summary>
    private static Dictionary<string, UserInfo> MapFoundUsers(
        IEnumerable<UserDisplayInfoResponse> userDetails)
    {
        var userInfoDict = new Dictionary<string, UserInfo>();

        foreach (var userDetail in userDetails)
        {
            userInfoDict[userDetail.Id] = new UserInfo
            {
                UserId = userDetail.Id,
                Email = userDetail.Email ?? string.Empty,
                FullName = userDetail.FullName ?? string.Empty,
                AvatarUrl = userDetail.AvatarUrl ?? string.Empty,
                Found = userDetail.Found
            };
        }

        return userInfoDict;
    }

    /// <summary>
    /// Adds missing user IDs as not found entries
    /// </summary>
    private static void AddMissingUsers(
        Dictionary<string, UserInfo> userInfoDict,
        List<string> uniqueUserIds)
    {
        foreach (var userId in uniqueUserIds.Where(userId => !userInfoDict.ContainsKey(userId)))
        {
            userInfoDict[userId] = new UserInfo
            {
                UserId = userId,
                Found = false
            };
        }
    }

    /// <summary>
    /// Handles gRPC exceptions and returns appropriate fallback response
    /// </summary>
    private Dictionary<string, UserInfo> HandleGrpcException(RpcException ex, List<string> uniqueUserIds)
    {
        var errorMessage = ex.StatusCode switch
        {
            StatusCode.DeadlineExceeded => "User service call timed out: {Status}",
            StatusCode.Unavailable => "User service is unavailable: {Status}",
            _ => "gRPC call to User service failed: {Status} - {Detail}"
        };

        if (ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode == StatusCode.Unavailable)
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString());
        }
        else
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString(), ex.Status.Detail);
        }

        return CreateEmptyUserInfos(uniqueUserIds);
    }

    /// <summary>
    /// Creates empty user info objects for when User service is unavailable
    /// </summary>
    private static Dictionary<string, UserInfo> CreateEmptyUserInfos(List<string> userIds)
    {
        return userIds.ToDictionary(
            userId => userId,
            userId => new UserInfo
            {
                UserId = userId,
                Found = false
            });
    }
}