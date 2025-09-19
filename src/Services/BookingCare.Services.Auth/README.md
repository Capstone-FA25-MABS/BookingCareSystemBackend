# BookingCare Authentication & Authorization Service

## 🚀 Quick Setup Guide

### 1. Inject Authentication & Authorization vào Program.cs

```csharp
// Services configuration
builder.Services.AddJwtAuthAndAuthorization();

// Middleware pipeline
app.UseStandardAuthPipeline();
```

### 2. ⚠️ Important Note

`app.UseStandardAuthPipeline()` đã bao gồm:

```csharp
app.UseAutoToken();        // Custom middleware to attach token from cookies
app.UseRouting();          // Must be before UseAuthentication and UseAuthorization
app.UseAuthentication();
app.UseAuthorization();
```

**Lưu ý:** Hãy xóa các dòng sau trong Program.cs nếu chúng tồn tại:

- `app.UseRouting();`
- `app.UseAuthentication();`
- `app.UseAuthorization();`

## 🔐 Authorization Rules

### Role-based Authorization

```csharp
[Authorize(Policy = "Role:Admin")]
[Authorize(Policy = "Role:Doctor")]
[Authorize(Policy = "Role:Patient")]
[Authorize(Policy = "Role:Hospital")]
```

### Permission-based Authorization

```csharp
[Authorize(Policy = "Perm:Patient.Read")]
[Authorize(Policy = "Perm:Doctor.Create")]
[Authorize(Policy = "Perm:Appointment.Update")]
```

### 📝 Permission Naming Convention

Khi tạo permission, sử dụng format: `"Resource.Action"`

**Ví dụ:**

- `Patient.Read`
- `Doctor.Create`
- `Appointment.Update`
- `Schedule.Delete`
