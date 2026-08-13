using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services
{
    public interface ICurrentUserService
    {
        string CurrentUserId { get; }

        AppUser? CurrentUser { get; }

        bool HasSelectedUser { get; }

        void SetCurrentUser(AppUser user);

        void ResetToDefaultUser();
    }
}

