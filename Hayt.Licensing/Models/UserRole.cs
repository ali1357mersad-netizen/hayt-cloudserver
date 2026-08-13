using Hayt.Licensing.Services;
using Hayt.Licensing.Models;
namespace Hayt.Licensing.Models;

/// <summary>
/// نقش امنیتی/عملیاتی کاربر در برنامه.
/// این enum با AITutorRole فرق دارد.
/// AITutorRole فقط نقش پیام در مکالمه است؛ اما UserRole برای مجوزدهی کاربر است.
/// </summary>
public enum UserRole
{
    Guest = 0,
    Student = 1,
    Teacher = 2,
    UniversityAdmin = 3,
    SystemAdmin = 4
}

