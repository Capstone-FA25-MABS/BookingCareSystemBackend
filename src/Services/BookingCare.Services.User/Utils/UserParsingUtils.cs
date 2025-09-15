using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Utils;

public static class UserParsingUtils
{
    public static Gender? ParseGender(string genderString)
    {
        if (string.IsNullOrWhiteSpace(genderString))
            return null;

        if (Enum.TryParse<Gender>(genderString, true, out var gender))
            return gender;

        return null;
    }

    public static DateTime? ParseDateOfBirth(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
            return date;

        return null;
    }

}
