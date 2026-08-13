using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
using System.Collections.Generic;
using System.Linq;

namespace Hayt.Licensing.Models;

/// <summary>
/// نتیجه تست داخلی لایه دسترسی.
/// برای اطمینان از اینکه Runtime، Role Gate، License Gate و Guard بدون Crash کار می‌کنند.
/// </summary>
public sealed class AppAccessSelfTestResult
{
    public string TestName { get; init; } = string.Empty;

    public bool Passed { get; init; }

    public UserRole Role { get; init; }

    public LicensePlan EffectivePlan { get; init; }

    public int TotalChecks { get; init; }

    public int PassedChecks { get; init; }

    public int FailedChecks { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<AppAccessSelfTestCheck> Checks { get; init; } =
        new List<AppAccessSelfTestCheck>();

    public string StatusText => Passed ? "PASS" : "FAIL";

    public static AppAccessSelfTestResult FromChecks(
        string testName,
        UserRole role,
        LicensePlan effectivePlan,
        IReadOnlyList<AppAccessSelfTestCheck> checks)
    {
        int total = checks.Count;
        int passed = checks.Count(x => x.Passed);
        int failed = total - passed;

        return new AppAccessSelfTestResult
        {
            TestName = testName,
            Passed = failed == 0,
            Role = role,
            EffectivePlan = effectivePlan,
            TotalChecks = total,
            PassedChecks = passed,
            FailedChecks = failed,
            Message = failed == 0
                ? "همه بررسی‌های دسترسی موفق بودند."
                : $"{failed} بررسی دسترسی ناموفق بود.",
            Checks = checks
        };
    }
}

public sealed class AppAccessSelfTestCheck
{
    public AppFeature Feature { get; init; }

    public string FeatureTitle { get; init; } = string.Empty;

    public bool ExpectedAllowed { get; init; }

    public bool ActualAllowed { get; init; }

    public bool Passed { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public static AppAccessSelfTestCheck Create(
        AppFeature feature,
        string featureTitle,
        bool expectedAllowed,
        bool actualAllowed,
        string reasonCode,
        string message)
    {
        return new AppAccessSelfTestCheck
        {
            Feature = feature,
            FeatureTitle = featureTitle,
            ExpectedAllowed = expectedAllowed,
            ActualAllowed = actualAllowed,
            Passed = expectedAllowed == actualAllowed,
            ReasonCode = reasonCode,
            Message = message
        };
    }
}

