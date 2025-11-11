namespace BookingCare.Services.Auth.Constants
{
    public static class AuthConstants
    {
        public const string InvalidRequestData = "Invalid request data";
        public const string ValidationError = "Validation error";

        // Channel types for registration and authentication
        public const string CHANNEL_EMAIL = "email";
        public const string CHANNEL_PHONE = "phone";

        // Constants for repeated string literals (SagaContext keys and sort field names)
        public const string SAGA_KEY_EMAIL = "Email";
        public const string SAGA_KEY_FULLNAME = "FullName";
        public const string SORT_FIELD_CREATED_AT = "CreatedAt";

        // Constants for repeated string literals
        public const string UNKNOWN_STATUS = "UNKNOWN";
        public const string ACCOUNT_NOT_FOUND = "Account not found";
    }
}
