namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Purpose namespaces used for OTP flows across services.
/// </summary>
public enum OtpPurpose
{
    REGISTER,
    FORGOT_PASSWORD
}

public static class OtpPurposeExtensions
{
    /// <summary>
    /// Converts enum to canonical lowercase key used in caches, HMAC, URLs.
    /// </summary>
    public static string ToKey(this OtpPurpose purpose)
    {
        return purpose switch
        {
            OtpPurpose.REGISTER => "register",
            OtpPurpose.FORGOT_PASSWORD => "forgot-password",
            _ => "register"
        };
    }
}


