namespace BookingCare.Services.Communication.Constants;

/// <summary>
/// Constants for file upload and storage operations
/// Fixes SonarQube S1192 issues (magic string literals)
/// </summary>
public static class FileUploadConstants
{
    // Content Type Prefixes
    public const string ImagePrefix = "image/";
    public const string VideoPrefix = "video/";
    public const string AudioPrefix = "audio/";

    // File Type Categories
    public const string ImageType = "image";
    public const string VideoType = "video";
    public const string AudioType = "audio";
    public const string DocumentType = "document";
    public const string ArchiveType = "archive";
    public const string UnknownType = "unknown";

    // Storage Provider Names
    public const string AwsS3CloudFront = "AWS S3 + CloudFront";

    // Common Error Messages
    public const string ErrorMessage = "Error";
    public const string UserNotAuthenticated = "User not authenticated";

    // Folder Names for S3 Storage
    public static class Folders
    {
        public const string Images = "images";
        public const string Videos = "videos";
        public const string Audio = "audio";
        public const string VoiceNotes = "voicenotes";
        public const string Gifs = "gifs";
        public const string Documents = "documents";
        public const string Text = "text";
        public const string Files = "files";
    }
}

/// <summary>
/// Constants for SignalR Hub operations
/// </summary>
public static class HubConstants
{
    public const string ErrorMessage = "Error";
    public const string UserNotAuthenticated = "User not authenticated";
    public const string ConnectionIdRequired = "ConnectionId is required";
    public const string InvalidRequest = "Invalid request";
}

/// <summary>
/// Constants for default avatar URLs
/// Fixes SonarQube S1075 issues (hardcoded URIs)
/// Note: These should ideally come from configuration/appsettings.json
/// </summary>
public static class DefaultAvatarUrls
{
    // These are temporary - move to appsettings.json
    public const string MaleDefault = "https://d24em9p7s2uixh.cloudfront.net/avatars/patients/male_20251003_f9c91483.png";
    public const string FemaleDefault = "https://d24em9p7s2uixh.cloudfront.net/avatars/patients/female_20251003_f9c91483.png";
    public const string GenericDefault = "https://bookingcaree.com/user-avatar-default.png";
}
