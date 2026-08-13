using Hayt.Licensing.Services;
using System.Collections.Generic;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس تست داخلی لایه دسترسی.
/// این سرویس برای تست بدون UI و بدون تغییر دیتابیس استفاده می‌شود.
/// </summary>
public interface IAppAccessSelfTestService
{
    IReadOnlyList<AppAccessSelfTestResult> RunAll();

    AppAccessSelfTestResult RunForRole(UserRole role);
}

