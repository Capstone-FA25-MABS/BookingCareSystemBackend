using BookingCare.Services.Auth.Models.DTOs;

namespace BookingCare.Services.Auth.Providers
{
    /// <summary>
    /// Interface for external authentication providers
    /// </summary>
    public interface IExternalAuthProvider
    {
        string Name { get; }
        Task<UserInfoBase?> VerifyAsync(string accessToken);
    }
}
