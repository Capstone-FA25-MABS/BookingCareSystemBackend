using BookingCare.Services.User.Data;
using BookingCare.Services.User.Models.Entities;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.User.Services;

// Temporarily commented out until gRPC issues are resolved
/*
public class UserGrpcService : global::BookingCare.Services.User.UserService.UserServiceBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return new UserResponse
            {
                Id = user.Id.ToString(),
                AccountId = user.AccountId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Gender = user.Gender?.ToString() ?? "",
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt.ToString("O")
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with ID: {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UserResponse> GetUserByAccountId(GetUserByAccountIdRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return new UserResponse
            {
                Id = user.Id.ToString(),
                AccountId = user.AccountId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Gender = user.Gender?.ToString() ?? "",
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt.ToString("O")
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by account ID: {AccountId}", request.AccountId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            if (await _context.Users.AnyAsync(u => u.AccountId == accountId))
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "User already exists for this account"));
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Email already exists"));
            }

            var user = new User
            {
                AccountId = accountId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = !string.IsNullOrEmpty(request.Gender) ? Enum.Parse<Gender>(request.Gender) : null,
                Address = request.Address,
                Phone = request.Phone,
                AvatarUrl = request.AvatarUrl ?? "https://bookingcaree.com/user-avatar-default.png"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserResponse
            {
                Id = user.Id.ToString(),
                AccountId = user.AccountId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Gender = user.Gender?.ToString() ?? "",
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt.ToString("O")
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user for account ID: {AccountId}", request.AccountId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            if (!string.IsNullOrEmpty(request.Gender))
                user.Gender = Enum.Parse<Gender>(request.Gender);
            if (request.Address != null)
                user.Address = request.Address;
            if (request.Phone != null)
                user.Phone = request.Phone;
            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();

            return new UserResponse
            {
                Id = user.Id.ToString(),
                AccountId = user.AccountId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Gender = user.Gender?.ToString() ?? "",
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt.ToString("O")
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return new DeleteUserResponse
            {
                Success = true,
                Message = "User deleted successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID: {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetAllUsersResponse> GetAllUsers(GetAllUsersRequest request, ServerCallContext context)
    {
        try
        {
            var page = request.Page > 0 ? request.Page : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            var skip = (page - 1) * pageSize;

            var query = _context.Users.AsQueryable();
            var totalCount = await query.CountAsync();

            var users = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = users.Select(user => new UserResponse
            {
                Id = user.Id.ToString(),
                AccountId = user.AccountId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Gender = user.Gender?.ToString() ?? "",
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt.ToString("O")
            }).ToList();

            return new GetAllUsersResponse
            {
                Users = { userResponses },
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
*/
