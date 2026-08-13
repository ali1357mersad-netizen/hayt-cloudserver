using Hayt.Licensing.Services;
using System;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// Runtime مرکزی برای WPF.
/// چون پروژه دسکتاپ است، این کلاس بدون وابستگی به ASP.NET/DI کار می‌کند.
/// بعداً اگر DI رسمی اضافه شود، می‌توان همین نقطه را به ServiceProvider وصل کرد.
/// </summary>
public static class AppAccessRuntime
{
    private static readonly object Sync = new();

    private static ILicenseService? _licenseService;
    private static IPremiumAccessService? _premiumAccessService;
    private static IRoleAccessService? _roleAccessService;
    private static IAppAccessService? _appAccessService;
    private static IAppAccessGuard? _guard;
    private static IAppAccessSnapshotService? _snapshotService;
    private static IAppAccessSelfTestService? _selfTestService;
    private static IAppAccessDiagnosticsService? _diagnosticsService;
    private static IAppAccessDiagnosticsFileService? _diagnosticsFileService;
    private static IAppAccessDiagnosticsRunnerService? _diagnosticsRunnerService;

    public static ILicenseService LicenseService
    {
        get
        {
            EnsureInitialized();
            return _licenseService!;
        }
    }

    public static IPremiumAccessService PremiumAccessService
    {
        get
        {
            EnsureInitialized();
            return _premiumAccessService!;
        }
    }

    public static IRoleAccessService RoleAccessService
    {
        get
        {
            EnsureInitialized();
            return _roleAccessService!;
        }
    }

    public static IAppAccessService AppAccessService
    {
        get
        {
            EnsureInitialized();
            return _appAccessService!;
        }
    }

    public static IAppAccessGuard Guard
    {
        get
        {
            EnsureInitialized();
            return _guard!;
        }
    }

    public static IAppAccessSnapshotService SnapshotService
    {
        get
        {
            EnsureInitialized();
            return _snapshotService!;
        }
    }

    public static IAppAccessSelfTestService SelfTestService
    {
        get
        {
            EnsureInitialized();
            return _selfTestService!;
        }
    }

    public static IAppAccessDiagnosticsService DiagnosticsService
    {
        get
        {
            EnsureInitialized();
            return _diagnosticsService!;
        }
    }

    public static IAppAccessDiagnosticsFileService DiagnosticsFileService
    {
        get
        {
            EnsureInitialized();
            return _diagnosticsFileService!;
        }
    }

    public static IAppAccessDiagnosticsRunnerService DiagnosticsRunnerService
    {
        get
        {
            EnsureInitialized();
            return _diagnosticsRunnerService!;
        }
    }

    public static UserRole CurrentRole
    {
        get
        {
            EnsureInitialized();
            return _roleAccessService!.CurrentRole;
        }
    }

    public static void EnsureInitialized()
    {
        if (_appAccessService is not null)
        {
            return;
        }

        lock (Sync)
        {
            if (_appAccessService is not null)
            {
                return;
            }

            BuildDefaultServices(UserRole.Student);
        }
    }

    public static void Configure(
        ILicenseService licenseService,
        IPremiumAccessService premiumAccessService,
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        IAppAccessGuard guard,
        IAppAccessSnapshotService snapshotService)
    {
        if (licenseService is null)
        {
            throw new ArgumentNullException(nameof(licenseService));
        }

        if (premiumAccessService is null)
        {
            throw new ArgumentNullException(nameof(premiumAccessService));
        }

        if (roleAccessService is null)
        {
            throw new ArgumentNullException(nameof(roleAccessService));
        }

        if (appAccessService is null)
        {
            throw new ArgumentNullException(nameof(appAccessService));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        if (snapshotService is null)
        {
            throw new ArgumentNullException(nameof(snapshotService));
        }

        lock (Sync)
        {
            _licenseService = licenseService;
            _premiumAccessService = premiumAccessService;
            _roleAccessService = roleAccessService;
            _appAccessService = appAccessService;
            _guard = guard;
            _snapshotService = snapshotService;

            RebuildDependentServices();
        }
    }

    public static void Configure(
        ILicenseService licenseService,
        IPremiumAccessService premiumAccessService,
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        IAppAccessGuard guard,
        IAppAccessSnapshotService snapshotService,
        IAppAccessSelfTestService selfTestService)
    {
        if (licenseService is null)
        {
            throw new ArgumentNullException(nameof(licenseService));
        }

        if (premiumAccessService is null)
        {
            throw new ArgumentNullException(nameof(premiumAccessService));
        }

        if (roleAccessService is null)
        {
            throw new ArgumentNullException(nameof(roleAccessService));
        }

        if (appAccessService is null)
        {
            throw new ArgumentNullException(nameof(appAccessService));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        if (snapshotService is null)
        {
            throw new ArgumentNullException(nameof(snapshotService));
        }

        if (selfTestService is null)
        {
            throw new ArgumentNullException(nameof(selfTestService));
        }

        lock (Sync)
        {
            _licenseService = licenseService;
            _premiumAccessService = premiumAccessService;
            _roleAccessService = roleAccessService;
            _appAccessService = appAccessService;
            _guard = guard;
            _snapshotService = snapshotService;
            _selfTestService = selfTestService;

            _diagnosticsService = new AppAccessDiagnosticsService(
                _licenseService,
                _roleAccessService,
                _selfTestService,
                _snapshotService);

            _diagnosticsFileService =
                new AppAccessDiagnosticsFileService(_diagnosticsService);

            _diagnosticsRunnerService =
                new AppAccessDiagnosticsRunnerService(_diagnosticsFileService);
        }
    }

    public static void Configure(
        ILicenseService licenseService,
        IPremiumAccessService premiumAccessService,
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        IAppAccessGuard guard,
        IAppAccessSnapshotService snapshotService,
        IAppAccessSelfTestService selfTestService,
        IAppAccessDiagnosticsService diagnosticsService)
    {
        if (licenseService is null)
        {
            throw new ArgumentNullException(nameof(licenseService));
        }

        if (premiumAccessService is null)
        {
            throw new ArgumentNullException(nameof(premiumAccessService));
        }

        if (roleAccessService is null)
        {
            throw new ArgumentNullException(nameof(roleAccessService));
        }

        if (appAccessService is null)
        {
            throw new ArgumentNullException(nameof(appAccessService));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        if (snapshotService is null)
        {
            throw new ArgumentNullException(nameof(snapshotService));
        }

        if (selfTestService is null)
        {
            throw new ArgumentNullException(nameof(selfTestService));
        }

        if (diagnosticsService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsService));
        }

        lock (Sync)
        {
            _licenseService = licenseService;
            _premiumAccessService = premiumAccessService;
            _roleAccessService = roleAccessService;
            _appAccessService = appAccessService;
            _guard = guard;
            _snapshotService = snapshotService;
            _selfTestService = selfTestService;
            _diagnosticsService = diagnosticsService;

            _diagnosticsFileService =
                new AppAccessDiagnosticsFileService(_diagnosticsService);

            _diagnosticsRunnerService =
                new AppAccessDiagnosticsRunnerService(_diagnosticsFileService);
        }
    }

    public static void Configure(
        ILicenseService licenseService,
        IPremiumAccessService premiumAccessService,
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        IAppAccessGuard guard,
        IAppAccessSnapshotService snapshotService,
        IAppAccessSelfTestService selfTestService,
        IAppAccessDiagnosticsService diagnosticsService,
        IAppAccessDiagnosticsFileService diagnosticsFileService)
    {
        if (licenseService is null)
        {
            throw new ArgumentNullException(nameof(licenseService));
        }

        if (premiumAccessService is null)
        {
            throw new ArgumentNullException(nameof(premiumAccessService));
        }

        if (roleAccessService is null)
        {
            throw new ArgumentNullException(nameof(roleAccessService));
        }

        if (appAccessService is null)
        {
            throw new ArgumentNullException(nameof(appAccessService));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        if (snapshotService is null)
        {
            throw new ArgumentNullException(nameof(snapshotService));
        }

        if (selfTestService is null)
        {
            throw new ArgumentNullException(nameof(selfTestService));
        }

        if (diagnosticsService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsService));
        }

        if (diagnosticsFileService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsFileService));
        }

        lock (Sync)
        {
            _licenseService = licenseService;
            _premiumAccessService = premiumAccessService;
            _roleAccessService = roleAccessService;
            _appAccessService = appAccessService;
            _guard = guard;
            _snapshotService = snapshotService;
            _selfTestService = selfTestService;
            _diagnosticsService = diagnosticsService;
            _diagnosticsFileService = diagnosticsFileService;

            _diagnosticsRunnerService =
                new AppAccessDiagnosticsRunnerService(_diagnosticsFileService);
        }
    }

    public static void Configure(
        ILicenseService licenseService,
        IPremiumAccessService premiumAccessService,
        IRoleAccessService roleAccessService,
        IAppAccessService appAccessService,
        IAppAccessGuard guard,
        IAppAccessSnapshotService snapshotService,
        IAppAccessSelfTestService selfTestService,
        IAppAccessDiagnosticsService diagnosticsService,
        IAppAccessDiagnosticsFileService diagnosticsFileService,
        IAppAccessDiagnosticsRunnerService diagnosticsRunnerService)
    {
        if (licenseService is null)
        {
            throw new ArgumentNullException(nameof(licenseService));
        }

        if (premiumAccessService is null)
        {
            throw new ArgumentNullException(nameof(premiumAccessService));
        }

        if (roleAccessService is null)
        {
            throw new ArgumentNullException(nameof(roleAccessService));
        }

        if (appAccessService is null)
        {
            throw new ArgumentNullException(nameof(appAccessService));
        }

        if (guard is null)
        {
            throw new ArgumentNullException(nameof(guard));
        }

        if (snapshotService is null)
        {
            throw new ArgumentNullException(nameof(snapshotService));
        }

        if (selfTestService is null)
        {
            throw new ArgumentNullException(nameof(selfTestService));
        }

        if (diagnosticsService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsService));
        }

        if (diagnosticsFileService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsFileService));
        }

        if (diagnosticsRunnerService is null)
        {
            throw new ArgumentNullException(nameof(diagnosticsRunnerService));
        }

        lock (Sync)
        {
            _licenseService = licenseService;
            _premiumAccessService = premiumAccessService;
            _roleAccessService = roleAccessService;
            _appAccessService = appAccessService;
            _guard = guard;
            _snapshotService = snapshotService;
            _selfTestService = selfTestService;
            _diagnosticsService = diagnosticsService;
            _diagnosticsFileService = diagnosticsFileService;
            _diagnosticsRunnerService = diagnosticsRunnerService;
        }
    }

    public static void SetCurrentRole(UserRole role)
    {
        EnsureInitialized();
        _roleAccessService!.SetCurrentRole(role);
    }

    public static AppAccessDecision Check(AppFeature feature)
    {
        EnsureInitialized();
        return _appAccessService!.CheckAccess(feature);
    }

    public static bool Can(AppFeature feature)
    {
        EnsureInitialized();
        return _appAccessService!.CanAccess(feature);
    }

    public static void Ensure(AppFeature feature)
    {
        EnsureInitialized();
        _appAccessService!.EnsureAccess(feature);
    }

    public static string CreateDiagnosticsTextReport()
    {
        EnsureInitialized();
        return _diagnosticsService!.CreateTextReport();
    }

    public static AppAccessDiagnosticsReport CreateDiagnosticsReport()
    {
        EnsureInitialized();
        return _diagnosticsService!.CreateReport();
    }

    public static AppAccessDiagnosticsSaveResult SaveDiagnosticsReport()
    {
        EnsureInitialized();
        return _diagnosticsFileService!.SaveReport();
    }

    public static AppAccessDiagnosticsSaveResult SaveDiagnosticsReport(
        string rootDirectory)
    {
        EnsureInitialized();
        return _diagnosticsFileService!.SaveReport(rootDirectory);
    }

    public static AppAccessDiagnosticsRunResult RunDiagnosticsAndSave()
    {
        EnsureInitialized();
        return _diagnosticsRunnerService!.RunAndSave();
    }

    public static AppAccessDiagnosticsRunResult RunDiagnosticsAndSave(
        string rootDirectory)
    {
        EnsureInitialized();
        return _diagnosticsRunnerService!.RunAndSave(rootDirectory);
    }

    public static string RunDiagnosticsAndSaveTextSummary()
    {
        EnsureInitialized();
        return _diagnosticsRunnerService!.RunAndSaveTextSummary();
    }

    public static string RunDiagnosticsAndSaveTextSummary(
        string rootDirectory)
    {
        EnsureInitialized();
        return _diagnosticsRunnerService!.RunAndSaveTextSummary(rootDirectory);
    }

    public static void ResetForTests(UserRole role = UserRole.Student)
    {
        lock (Sync)
        {
            BuildDefaultServices(role);
        }
    }

    private static void BuildDefaultServices(UserRole role)
    {
        _licenseService = new LicenseService();
        _licenseService.Load();

        _premiumAccessService = new PremiumAccessService(_licenseService);

        _roleAccessService = new RoleAccessService();
        _roleAccessService.SetCurrentRole(role);

        _appAccessService = new AppAccessService(
            _roleAccessService,
            _premiumAccessService,
            _licenseService);

        _guard = new AppAccessGuard(_appAccessService);
        _snapshotService = new AppAccessSnapshotService(_appAccessService);

        RebuildDependentServices();
    }

    private static void RebuildDependentServices()
    {
        _selfTestService = new AppAccessSelfTestService(
            _roleAccessService!,
            _appAccessService!,
            _licenseService!);

        _diagnosticsService = new AppAccessDiagnosticsService(
            _licenseService!,
            _roleAccessService!,
            _selfTestService,
            _snapshotService!);

        _diagnosticsFileService =
            new AppAccessDiagnosticsFileService(_diagnosticsService);

        _diagnosticsRunnerService =
            new AppAccessDiagnosticsRunnerService(_diagnosticsFileService);
    }
}

