using System.Security.Cryptography;
using System.Text;

namespace BookingCare.Services.Auth.Utils;

/// <summary>
/// Helper class for password generation and validation
/// </summary>
public static class PasswordHelper
{
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "@$!%*?&";

    /// <summary>
    /// Generate a strong random password that meets all requirements:
    /// - At least 12 characters long
    /// - Contains at least one uppercase letter
    /// - Contains at least one lowercase letter
    /// - Contains at least one digit
    /// - Contains at least one special character
    /// </summary>
    /// <param name="length">Length of the password (minimum 12)</param>
    /// <returns>A strong random password</returns>
    public static string GenerateStrongPassword(int length = 16)
    {
        if (length < 12)
        {
            length = 12;
        }

        var password = new StringBuilder();

        // Ensure at least one character from each required category
        password.Append(GetRandomChar(LowercaseChars));
        password.Append(GetRandomChar(UppercaseChars));
        password.Append(GetRandomChar(DigitChars));
        password.Append(GetRandomChar(SpecialChars));

        // Fill the rest with random characters from all categories
        string allChars = LowercaseChars + UppercaseChars + DigitChars + SpecialChars;
        for (int i = password.Length; i < length; i++)
        {
            password.Append(GetRandomChar(allChars));
        }

        // Shuffle the password to avoid predictable patterns
        return ShuffleString(password.ToString());
    }

    /// <summary>
    /// Get a random character from the provided character set
    /// </summary>
    private static char GetRandomChar(string chars)
    {
        byte[] randomBytes = new byte[4];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        int randomValue = BitConverter.ToInt32(randomBytes, 0) & int.MaxValue;
        return chars[randomValue % chars.Length];
    }

    /// <summary>
    /// Shuffle a string to randomize character positions
    /// </summary>
    private static string ShuffleString(string input)
    {
        char[] array = input.ToCharArray();
        int n = array.Length;

        using (var rng = RandomNumberGenerator.Create())
        {
            for (int i = n - 1; i > 0; i--)
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int randomValue = BitConverter.ToInt32(randomBytes, 0) & int.MaxValue;
                int j = randomValue % (i + 1);

                // Swap
                char temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        return new string(array);
    }
}

