using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Hayt.Views
{
    public partial class UserSelectionWindow : Window
    {
        private readonly ICurrentUserService _currentUserService;

        public bool UserChanged { get; private set; }

        public UserSelectionWindow(ICurrentUserService currentUserService)
        {
            InitializeComponent();

            _currentUserService = currentUserService ??
                throw new ArgumentNullException(nameof(currentUserService));

            Loaded += async (_, _) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                StatusText.Text = "در حال بارگذاری کاربران...";

                using var db = new AppDbContext();

                await EnsureDefaultUserAsync(db);

                var users = await db.AppUsers
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayName)
                    .ToListAsync();

                UsersList.ItemsSource = users;

                var currentUser = users.FirstOrDefault(x =>
                    x.Id == _currentUserService.CurrentUserId);

                if (currentUser != null)
                {
                    UsersList.SelectedItem = currentUser;
                }

                CurrentUserText.Text =
                    "کاربر فعلی: " + _currentUserService.CurrentUserId;

                StatusText.Text =
                    users.Count == 0
                        ? "هنوز کاربری ثبت نشده است."
                        : $"{users.Count} کاربر بارگذاری شد.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "خطا در بارگذاری کاربران.";

                MessageBox.Show(
                    "خطا در بارگذاری کاربران:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "انتخاب کاربر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static async Task EnsureDefaultUserAsync(AppDbContext db)
        {
            var exists = await db.AppUsers.AnyAsync(x => x.Id == "default");

            if (exists)
            {
                return;
            }

            db.AppUsers.Add(new AppUser
            {
                Id = "default",
                DisplayName = "کاربر اصلی",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            await db.SaveChangesAsync();
        }

        private async void CreateUserButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var displayName = DisplayNameBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    MessageBox.Show(
                        "لطفاً نام کاربر را وارد کنید.",
                        "ساخت کاربر",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DisplayNameBox.Focus();
                    return;
                }

                var id = CreateUserId();

                using var db = new AppDbContext();

                while (await db.AppUsers.AnyAsync(x => x.Id == id))
                {
                    id = CreateUserId();
                }

                var user = new AppUser
                {
                    Id = id,
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };

                db.AppUsers.Add(user);
                await db.SaveChangesAsync();

                _currentUserService.SetCurrentUser(user);
                await AppSettingsService.SetLastSelectedUserIdAsync(user.Id);
                UserChanged = true;

                DisplayNameBox.Text = string.Empty;

                await LoadUsersAsync();

                StatusText.Text =
                    "کاربر جدید ساخته و انتخاب شد: " + user.DisplayName;

                MessageBox.Show(
                    "کاربر جدید ساخته و انتخاب شد.",
                    "ساخت کاربر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ساخت کاربر:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "ساخت کاربر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SelectUserButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await SelectSelectedUserAsync();
        }

        private async void UsersList_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            await SelectSelectedUserAsync();
        }

        private async Task SelectSelectedUserAsync()
        {
            try
            {
                if (UsersList.SelectedItem is not AppUser selectedUser)
                {
                    MessageBox.Show(
                        "لطفاً یک کاربر را از لیست انتخاب کنید.",
                        "انتخاب کاربر",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                using var db = new AppDbContext();

                var user = await db.AppUsers
                    .FirstOrDefaultAsync(x => x.Id == selectedUser.Id);

                if (user == null)
                {
                    MessageBox.Show(
                        "کاربر انتخاب‌شده در دیتابیس پیدا نشد.",
                        "انتخاب کاربر",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    await LoadUsersAsync();
                    return;
                }

                user.LastLoginAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                _currentUserService.SetCurrentUser(user);
                await AppSettingsService.SetLastSelectedUserIdAsync(user.Id);
                UserChanged = true;

                CurrentUserText.Text =
                    "کاربر فعلی: " + _currentUserService.CurrentUserId;

                StatusText.Text =
                    "کاربر انتخاب شد: " + user.DisplayName;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در انتخاب کاربر:" +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "انتخاب کاربر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private static string CreateUserId()
        {
            return "user-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        }
    }
}

