using System;
using Hayt.Models;
using Hayt.Licensing.Models;

namespace Hayt.Services
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        public const string DefaultUserId = "default";

        private AppUser? _currentUser;

        public string CurrentUserId =>
            _currentUser?.Id ?? DefaultUserId;

        public AppUser? CurrentUser => _currentUser;

        public bool HasSelectedUser => _currentUser is not null;

        public void SetCurrentUser(AppUser user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(user.Id))
            {
                throw new ArgumentException(
                    "شناسه کاربر نمی‌تواند خالی باشد.",
                    nameof(user));
            }

            _currentUser = user;
        }

        public void ResetToDefaultUser()
        {
            _currentUser = new AppUser
            {
                Id = DefaultUserId,
                DisplayName = "کاربر اصلی",
                IsActive = true
            };
        }
    }
}

