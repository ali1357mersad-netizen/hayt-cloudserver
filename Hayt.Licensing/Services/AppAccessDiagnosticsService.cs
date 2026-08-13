using Hayt.Licensing.Services;
using System;
using System.Collections.Generic;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// گزارش‌گیر مرکزی لایه دسترسی.
/// این سرویس Self-Test و Snapshot را ترکیب می‌کند.
/// </summary>
public sealed class AppAccessDiagnosticsService : IAppAccessDiagnosticsService
{
    private readonly ILicenseService _licenseService;
    private readonly IRoleAccessService _roleAccessService;
    private readonly IAppAccessSelfTestService _selfTestService;
    private readonly IAppAccessSnapshotService _snapshotService;

    public AppAccessDiagnosticsService()
        : this(
            AppAccessRuntime.LicenseService,
            AppAccessRuntime.RoleAccessService,
            AppAccessRuntime.SelfTestService,
            AppAccessRuntime.SnapshotService)
    {
    }

    public AppAccessDiagnosticsService(
        ILicenseService licenseService,
        IRoleAccessService roleAccessService,
        IAppAccessSelfTestService selfTestService,
        IAppAccessSnapshotService snapshotService)
    {
        _licenseService = licenseService ??
            throw new ArgumentNullException(nameof(licenseService));

        _roleAccessService = roleAccessService ??
            throw new ArgumentNullException(nameof(roleAccessService));

        _selfTestService = selfTestService ??
            throw new ArgumentNullException(nameof(selfTestService));

        _snapshotService = snapshotService ??
            throw new ArgumentNullException(nameof(snapshotService));
    }

    public AppAccessDiagnosticsReport CreateReport()
    {
        UserRole originalRole = _roleAccessService.CurrentRole;

        try
        {
            LicensePlan plan = SafeGetEffectivePlan();

            IReadOnlyList<AppAccessSelfTestResult> roleResults =
                _selfTestService.RunAll();

            _roleAccessService.SetCurrentRole(originalRole);

            IReadOnlyList<AppAccessStatusItem> allSnapshot =
                SafeSnapshot(() => _snapshotService.GetAll());

            IReadOnlyList<AppAccessStatusItem> teacherSnapshot =
                SafeSnapshot(() => _snapshotService.GetTeacherPanelItems());

            IReadOnlyList<AppAccessStatusItem> studentSnapshot =
                SafeSnapshot(() => _snapshotService.GetStudentPanelItems());

            IReadOnlyList<AppAccessStatusItem> adminSnapshot =
                SafeSnapshot(() => _snapshotService.GetAdminPanelItems());

            return AppAccessDiagnosticsReport.Create(
                plan,
                originalRole,
                roleResults,
                allSnapshot,
                teacherSnapshot,
                studentSnapshot,
                adminSnapshot);
        }
        finally
        {
            _roleAccessService.SetCurrentRole(originalRole);
        }
    }

    public string CreateTextReport()
    {
        return CreateReport().ToText();
    }

    private LicensePlan SafeGetEffectivePlan()
    {
        try
        {
            return _licenseService.GetEffectivePlan();
        }
        catch
        {
            return LicensePlan.Free;
        }
    }

    private static IReadOnlyList<AppAccessStatusItem> SafeSnapshot(
        Func<IReadOnlyList<AppAccessStatusItem>> factory)
    {
        try
        {
            return factory();
        }
        catch
        {
            return new List<AppAccessStatusItem>();
        }
    }
}

