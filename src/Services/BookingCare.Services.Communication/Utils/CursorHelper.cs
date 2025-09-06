namespace BookingCare.Services.Communication.Utils;

/// <summary>
/// Utility class cho cursor-based pagination operations
/// </summary>
public static class CursorHelper
{
    /// <summary>
    /// Generate cursor từ timestamp và ID
    /// </summary>
    /// <param name="id">Entity ID (message ID, conversation ID, etc.)</param>
    /// <param name="timestamp">Timestamp for ordering</param>
    /// <returns>Base64 encoded cursor string</returns>
    public static string GenerateCursor(string id, DateTime timestamp)
    {
        var combined = $"{timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}|{id}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Parse cursor để lấy timestamp và ID
    /// </summary>
    /// <param name="cursor">Base64 encoded cursor string</param>
    /// <returns>Tuple containing timestamp and ID</returns>
    /// <exception cref="ArgumentException">Thrown when cursor format is invalid</exception>
    public static (DateTime timestamp, string id) ParseCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var combined = System.Text.Encoding.UTF8.GetString(bytes);
            var parts = combined.Split('|', 2);
            
            if (parts.Length != 2)
                throw new ArgumentException("Invalid cursor format");

            var timestamp = DateTime.Parse(parts[0]);
            var id = parts[1];

            return (timestamp, id);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new ArgumentException("Invalid cursor format", ex);
        }
    }

    /// <summary>
    /// Validate cursor format without parsing
    /// </summary>
    /// <param name="cursor">Cursor string to validate</param>
    /// <returns>True if cursor format is valid</returns>
    public static bool IsValidCursor(string cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return false;

        try
        {
            ParseCursor(cursor);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create cursor for current timestamp
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Cursor for current UTC time</returns>
    public static string CreateCurrentCursor(string id)
    {
        return GenerateCursor(id, DateTime.UtcNow);
    }
}