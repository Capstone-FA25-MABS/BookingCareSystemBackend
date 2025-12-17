using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.FileUpload.Controllers;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;

namespace BookingCare.Services.Doctor.Controllers;

/// <summary>
/// Controller for managing doctor avatars
/// </summary>
[Authorize] // Any authenticated actor can manage own avatar (role-independent)
public class AvatarController : BaseAvatarController<IDoctorService, AvatarController>
{
    private static readonly AvatarConfig Config = new()
    {
        EntityType = "doctor-avatar",
        EntityDisplayName = "Doctor",
        UploadFolder = "avatars/doctors",
        UploadSuccessMessage = "Doctor avatar uploaded successfully",
        DeleteSuccessMessage = "Doctor avatar deleted successfully"
    };

    public AvatarController(
        FileUploadOrchestrator uploadOrchestrator,
        IDoctorService doctorService,
        ILogger<AvatarController> logger)
        : base(uploadOrchestrator, doctorService, logger, Config)
    {
    }
}
