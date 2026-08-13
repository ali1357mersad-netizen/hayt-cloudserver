using Hayt.Licensing.Services;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس مرکزی تشخیص دسترسی بر اساس Role.
/// فعلاً مستقل از AppUser نگه داشته شده تا بعداً با CurrentUserService وصل شود.
/// </summary>
public interface IRoleAccessService
{
    UserRole CurrentRole { get; }

    void SetCurrentRole(UserRole role);

    bool CanAccess(AppFeature feature);

    void EnsureAccess(AppFeature feature);
}

