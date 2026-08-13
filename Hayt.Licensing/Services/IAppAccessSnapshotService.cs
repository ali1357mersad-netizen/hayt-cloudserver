using Hayt.Licensing.Services;
using System.Collections.Generic;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

/// <summary>
/// سرویس تهیه وضعیت دسترسی برای UI.
/// </summary>
public interface IAppAccessSnapshotService
{
    IReadOnlyList<AppAccessStatusItem> GetAll();

    IReadOnlyList<AppAccessStatusItem> GetTeacherPanelItems();

    IReadOnlyList<AppAccessStatusItem> GetStudentPanelItems();

    IReadOnlyList<AppAccessStatusItem> GetAdminPanelItems();
}

