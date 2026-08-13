using Hayt.Licensing.Services;
using System;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// پیاده‌سازی مرکزی Role Gate.
/// در فاز بعدی CurrentRole از CurrentUserService خوانده می‌شود.
/// </summary>
public sealed class RoleAccessService : IRoleAccessService
{
    private readonly object _sync = new();

    private UserRole _currentRole = UserRole.Student;

    public UserRole CurrentRole
    {
        get
        {
            lock (_sync)
            {
                return _currentRole;
            }
        }
    }

    public void SetCurrentRole(UserRole role)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
        {
            role = UserRole.Guest;
        }

        lock (_sync)
        {
            _currentRole = role;
        }
    }

    public bool CanAccess(AppFeature feature)
    {
        return AppAccessPolicy.IsRoleAllowed(CurrentRole, feature);
    }

    public void EnsureAccess(AppFeature feature)
    {
        if (CanAccess(feature))
        {
            return;
        }

        string title = AppAccessPolicy.GetTitle(feature);

        var decision = AppAccessDecision.DenyByRole(
            feature,
            CurrentRole,
            LicensePlan.Free,
            title,
            $"نقش فعلی شما اجازه دسترسی به «{title}» را ندارد.");

        throw new AppAccessDeniedException(decision);
    }
}

