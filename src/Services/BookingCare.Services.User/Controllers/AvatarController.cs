using BookingCare.Services.User.Services;
using BookingCare.Shared.FileUpload.Controllers;
using BookingCare.Shared.FileUpload.Services;

namespace BookingCare.Services.User.Controllers;

/// <summary>
/// Controller for managing user avatars
/// </summary>
public class AvatarController : BaseAvatarController<IUserService, AvatarController>
{
    private static readonly AvatarConfig Config = new()
    {
        EntityType = "avatar",
        EntityDisplayName = "User",
        UploadFolder = "avatars/patients",
        UploadSuccessMessage = "Avatar uploaded successfully",
        DeleteSuccessMessage = "Avatar deleted successfully"
    };

    public AvatarController(
        FileUploadOrchestrator uploadOrchestrator,
        IUserService userService,
        ILogger<AvatarController> logger)
        : base(uploadOrchestrator, userService, logger, Config)
    {
    }
}

