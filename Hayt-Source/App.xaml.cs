using Hayt.Data;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.Services.CloudSync;
using Hayt.ViewModels;
using Hayt.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Windows;
using Hayt.Cloud.Models;
using Hayt.Cloud.Services;

namespace Hayt
{
    public partial class App : Application
    {
        public static CloudSyncManager? GlobalCloudManager { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // اجرای معماری صحیح غیرهمزمان برای استارتاپ بدون قفل کردن Thread اصلی
            _ = InitializeApplicationAsync();
        }

        private async Task InitializeApplicationAsync()
        {
            // Step20-CloudSyncRuntimeBridge-Initialized
            Hayt.Services.CloudSync.CloudSyncRuntimeBridge.TryInitializeFromApplication(this);

            // مرحله ۱۳-C5: بازیابی و اعمال تم ذخیره‌شده پیش از نمایش پنجره‌ها
            ThemeService.Instance.LoadAndApply();

            try
            {
                // تا قبل از نمایش MainWindow، بستن پنجره انتخاب کاربر نباید برنامه را ببندد.
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // فاز ۳: نمایش صفحه خوش‌آمدگویی
                var welcomeWindow = new WelcomeWindow();
                var welcomeResult = welcomeWindow.ShowDialog();

                // اگر کاربر صفحه Welcome را بست، برنامه خاتمه می‌یابد
                if (welcomeResult != true)
                {
                    Shutdown();
                    return;
                }

                await DbInitializer.InitializeAsync();

                var importer = new JsonImportService();
                await importer.ImportCategoriesAsync();
                await importer.ImportSeedBooksAsync();

                var dbContext = new AppDbContext();
                var currentUserService = new CurrentUserService();

                // اگر کاربر چیزی انتخاب نکند، برنامه با کاربر پیش‌فرض ادامه می‌دهد.
                currentUserService.ResetToDefaultUser();

                // مرحله ۱۰: بارگذاری آخرین کاربر انتخاب‌شده از AppSettings
                var savedUserId = await AppSettingsService.GetLastSelectedUserIdAsync();

                if (!string.IsNullOrWhiteSpace(savedUserId))
                {
                    using var startupDb = new AppDbContext();

                    var savedUser = await startupDb.AppUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == savedUserId && x.IsActive);

                    if (savedUser != null)
                    {
                        currentUserService.SetCurrentUser(savedUser);
                    }
                }

                // انتخاب کاربر در شروع برنامه
                var userSelectionWindow = new UserSelectionWindow(currentUserService);
                var selectionResult = userSelectionWindow.ShowDialog();

                // اگر پنجره بسته شد یا انتخابی انجام نشد، همان default فعال می‌ماند.
                if (selectionResult != true && !currentUserService.HasSelectedUser)
                {
                    currentUserService.ResetToDefaultUser();
                }

                                // --- Cloud Sync Manager Initialization ---
                var cloudOptions = new CloudClientOptions
                {
                    BaseUrl = "http://localhost:5088",
                    ApiKey = string.Empty,
                    UserId = currentUserService.CurrentUserId,
                    DisplayName = currentUserService.CurrentUser?.DisplayName ?? "کاربر اصلی",
                    DeviceId = Environment.MachineName
                };

                GlobalCloudManager = new CloudSyncManager(cloudOptions, Application.Current.Dispatcher);
                await GlobalCloudManager.ConnectOnlineAsync();
                // ------------------------------------------

                IDataService dataService = new SqliteDataService(dbContext, currentUserService);
                IDashboardService dashboardService = new DashboardService(dbContext, currentUserService);

                Application.Current.Properties["DashboardService"] = dashboardService;

                var mainViewModel = new MainViewModel(dataService, currentUserService);

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                                mainWindow.Closed += async (_, _) =>
                {
                    try
                    {
                        if (GlobalCloudManager != null)
                        {
                            await GlobalCloudManager.DisconnectOnlineAsync();
                            await GlobalCloudManager.DisposeAsync();
                        }
                    }
                    catch { }
                    finally
                    {
                        dbContext.Dispose();
                    }
                };

                MainWindow = mainWindow;

                // از اینجا به بعد بستن MainWindow باید برنامه را ببندد.
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();

                await mainViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "برنامه هنگام راه‌اندازی با خطا مواجه شد." + Environment.NewLine + Environment.NewLine +
                    "پیام خطا:" + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                    "جزئیات:" + Environment.NewLine + ex,
                    "خطا در راه‌اندازی برنامه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }
    }
}



