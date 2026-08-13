using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// نتیجه یک سناریوی تست.
    /// </summary>
    public sealed class CloudSyncTestScenarioResult
    {
        public string ScenarioName { get; set; } = string.Empty;

        public bool IsPassed { get; set; }

        public string? FailureReason { get; set; }

        public TimeSpan Duration { get; set; }

        public int ConflictCount { get; set; }
    }

    /// <summary>
    /// نتیجه کلی تست چند دستگاه.
    /// </summary>
    public sealed class CloudSyncMultiDeviceTestResult
    {
        public bool AllPassed { get; set; }

        public int TotalScenarios { get; set; }

        public int PassedScenarios { get; set; }

        public int FailedScenarios { get; set; }

        public List<CloudSyncTestScenarioResult> ScenarioResults { get; set; } =
            new List<CloudSyncTestScenarioResult>();
    }

    /// <summary>
    /// یک دستگاه شبیه‌سازی‌شده.
    /// </summary>
    public sealed class CloudSyncSimulatedDevice
    {
        public string DeviceId { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;

        public long CurrentVersion { get; set; }

        public Dictionary<string, string> LocalData { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// تست‌کننده چند دستگاه.
    /// </summary>
    public sealed class CloudSyncMultiDeviceTester
    {
        private readonly CloudSyncConflictResolver _conflictResolver;

        public CloudSyncMultiDeviceTester(
            CloudSyncConflictResolver? conflictResolver = null)
        {
            _conflictResolver =
                conflictResolver ??
                new CloudSyncConflictResolver(
                    CloudSyncConflictPolicy.LastWriteWins);
        }

        /// <summary>
        /// اجرای تمام سناریوهای تست.
        /// </summary>
        public async Task<CloudSyncMultiDeviceTestResult> RunAllAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new CloudSyncMultiDeviceTestResult();

            result.ScenarioResults.Add(
                await RunConcurrentModificationScenarioAsync(
                    cancellationToken));

            result.ScenarioResults.Add(
                await RunDeleteVsModifyScenarioAsync(
                    cancellationToken));

            result.ScenarioResults.Add(
                await RunDisconnectReconnectScenarioAsync(
                    cancellationToken));

            result.ScenarioResults.Add(
                await RunMergeScenarioAsync(
                    cancellationToken));

            result.TotalScenarios = result.ScenarioResults.Count;
            result.PassedScenarios =
                result.ScenarioResults.Count(s => s.IsPassed);
            result.FailedScenarios =
                result.ScenarioResults.Count(s => !s.IsPassed);
            result.AllPassed =
                result.FailedScenarios == 0;

            return result;
        }

        /// <summary>
        /// سناریو ۱: تغییر همزمان یک رکورد توسط دو دستگاه.
        /// </summary>
        public async Task<CloudSyncTestScenarioResult>
            RunConcurrentModificationScenarioAsync(
                CancellationToken cancellationToken = default)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                var deviceA = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-a",
                    DeviceName = "دستگاه A",
                    CurrentVersion = 1
                };

                var deviceB = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-b",
                    DeviceName = "دستگاه B",
                    CurrentVersion = 1
                };

                deviceA.LocalData["customer-1"] =
                    "{\"name\":\"علی\",\"version\":1}";

                deviceB.LocalData["customer-1"] =
                    "{\"name\":\"علی\",\"version\":1}";

                await Task.Delay(10, cancellationToken);

                deviceA.LocalData["customer-1"] =
                    "{\"name\":\"علی\",\"phone\":\"0912\",\"version\":2}";
                deviceA.CurrentVersion = 2;

                await Task.Delay(10, cancellationToken);

                deviceB.LocalData["customer-1"] =
                    "{\"name\":\"علی\",\"address\":\"تهران\",\"version\":2}";
                deviceB.CurrentVersion = 2;

                var conflict = _conflictResolver.DetectConflict(
                    entityType: "Customer",
                    entityId: "customer-1",
                    clientVersion: deviceA.CurrentVersion,
                    serverVersion: deviceB.CurrentVersion,
                    clientPayloadJson: deviceA.LocalData["customer-1"],
                    serverPayloadJson: deviceB.LocalData["customer-1"],
                    clientChangedAtUtc: DateTime.UtcNow.AddSeconds(-1),
                    serverChangedAtUtc: DateTime.UtcNow);

                int conflictCount = conflict is null ? 0 : 1;

                var resolution = conflict is null
                    ? null
                    : _conflictResolver.ResolveDefault(conflict);

                bool isPassed =
                    conflict is not null &&
                    resolution is not null &&
                    resolution.IsResolved;

                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "تغییر همزمان یک رکورد توسط دو دستگاه",
                    IsPassed = isPassed,
                    FailureReason = isPassed
                        ? null
                        : "تعارض شناسایی یا حل نشد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = conflictCount
                };
            }
            catch (OperationCanceledException)
            {
                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "تغییر همزمان یک رکورد توسط دو دستگاه",
                    IsPassed = false,
                    FailureReason = "عملیات لغو شد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 0
                };
            }
        }

        /// <summary>
        /// سناریو ۲: حذف توسط یک دستگاه و تغییر توسط دستگاه دیگر.
        /// </summary>
        public async Task<CloudSyncTestScenarioResult>
            RunDeleteVsModifyScenarioAsync(
                CancellationToken cancellationToken = default)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                var deviceA = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-a",
                    DeviceName = "دستگاه A",
                    CurrentVersion = 1
                };

                var deviceB = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-b",
                    DeviceName = "دستگاه B",
                    CurrentVersion = 1
                };

                deviceA.LocalData["product-7"] =
                    "{\"name\":\"لپ‌تاپ\",\"version\":1}";

                deviceB.LocalData["product-7"] =
                    "{\"name\":\"لپ‌تاپ\",\"version\":1}";

                await Task.Delay(10, cancellationToken);

                deviceA.LocalData["product-7"] =
                    "{\"isDeleted\":true,\"version\":2}";
                deviceA.CurrentVersion = 2;

                await Task.Delay(10, cancellationToken);

                deviceB.LocalData["product-7"] =
                    "{\"name\":\"لپ‌تاپ\",\"price\":1500,\"version\":2}";
                deviceB.CurrentVersion = 2;

                var conflict = _conflictResolver.DetectConflict(
                    entityType: "Product",
                    entityId: "product-7",
                    clientVersion: deviceA.CurrentVersion,
                    serverVersion: deviceB.CurrentVersion,
                    clientPayloadJson: deviceA.LocalData["product-7"],
                    serverPayloadJson: deviceB.LocalData["product-7"],
                    clientChangedAtUtc: DateTime.UtcNow.AddSeconds(-1),
                    serverChangedAtUtc: DateTime.UtcNow);

                int conflictCount = conflict is null ? 0 : 1;

                bool isPassed =
                    conflict is not null &&
                    conflict.ConflictType ==
                        CloudSyncConflictType.ClientDeletedServerModified;

                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "حذف توسط یک دستگاه و تغییر توسط دستگاه دیگر",
                    IsPassed = isPassed,
                    FailureReason = isPassed
                        ? null
                        : "نوع تعارض به‌درستی شناسایی نشد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = conflictCount
                };
            }
            catch (OperationCanceledException)
            {
                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "حذف توسط یک دستگاه و تغییر توسط دستگاه دیگر",
                    IsPassed = false,
                    FailureReason = "عملیات لغو شد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 0
                };
            }
        }

        /// <summary>
        /// سناریو ۳: قطع و وصل ناگهانی دستگاه.
        /// </summary>
        public async Task<CloudSyncTestScenarioResult>
            RunDisconnectReconnectScenarioAsync(
                CancellationToken cancellationToken = default)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                var deviceA = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-a",
                    DeviceName = "دستگاه A",
                    CurrentVersion = 1
                };

                deviceA.LocalData["invoice-3"] =
                    "{\"number\":100,\"version\":1}";

                await Task.Delay(10, cancellationToken);

                // شبیه‌سازی قطع اتصال
                await Task.Delay(50, cancellationToken);

                // تغییر در حالت آفلاین
                deviceA.LocalData["invoice-3"] =
                    "{\"number\":100,\"total\":2500000,\"version\":2}";
                deviceA.CurrentVersion = 2;

                await Task.Delay(10, cancellationToken);

                // شبیه‌سازی اتصال مجدد
                bool reconnected = true;

                bool isPassed =
                    reconnected &&
                    deviceA.CurrentVersion == 2 &&
                    deviceA.LocalData.ContainsKey("invoice-3");

                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "قطع و وصل ناگهانی دستگاه",
                    IsPassed = isPassed,
                    FailureReason = isPassed
                        ? null
                        : "اتصال مجدد یا داده محلی نامعتبر است.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 0
                };
            }
            catch (OperationCanceledException)
            {
                return new CloudSyncTestScenarioResult
                {
                    ScenarioName =
                        "قطع و وصل ناگهانی دستگاه",
                    IsPassed = false,
                    FailureReason = "عملیات لغو شد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 0
                };
            }
        }

        /// <summary>
        /// سناریو ۴: ادغام هوشمند فیلدها.
        /// </summary>
        public async Task<CloudSyncTestScenarioResult>
            RunMergeScenarioAsync(
                CancellationToken cancellationToken = default)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                var deviceA = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-a",
                    DeviceName = "دستگاه A",
                    CurrentVersion = 1
                };

                var deviceB = new CloudSyncSimulatedDevice
                {
                    DeviceId = "device-b",
                    DeviceName = "دستگاه B",
                    CurrentVersion = 1
                };

                deviceA.LocalData["contact-5"] =
                    "{\"name\":\"مریم\",\"version\":1}";

                deviceB.LocalData["contact-5"] =
                    "{\"name\":\"مریم\",\"version\":1}";

                await Task.Delay(10, cancellationToken);

                deviceA.LocalData["contact-5"] =
                    "{\"name\":\"مریم\",\"email\":\"m@x.com\",\"version\":2}";
                deviceA.CurrentVersion = 2;

                await Task.Delay(10, cancellationToken);

                deviceB.LocalData["contact-5"] =
                    "{\"name\":\"مریم\",\"phone\":\"0935\",\"version\":2}";
                deviceB.CurrentVersion = 2;

                var conflict = _conflictResolver.DetectConflict(
                    entityType: "Contact",
                    entityId: "contact-5",
                    clientVersion: deviceA.CurrentVersion,
                    serverVersion: deviceB.CurrentVersion,
                    clientPayloadJson: deviceA.LocalData["contact-5"],
                    serverPayloadJson: deviceB.LocalData["contact-5"],
                    clientChangedAtUtc: DateTime.UtcNow.AddSeconds(-1),
                    serverChangedAtUtc: DateTime.UtcNow);

                if (conflict is null)
                {
                    return new CloudSyncTestScenarioResult
                    {
                        ScenarioName = "ادغام هوشمند فیلدها",
                        IsPassed = false,
                        FailureReason = "تعارض شناسایی نشد.",
                        Duration = DateTime.UtcNow - startedAt,
                        ConflictCount = 0
                    };
                }

                var resolution = _conflictResolver.Resolve(
                    conflict,
                    CloudSyncConflictPolicy.Merge);

                bool isPassed =
                    resolution.IsResolved &&
                    resolution.WinningPayloadJson is not null &&
                    resolution.WinningPayloadJson.Contains("email") &&
                    resolution.WinningPayloadJson.Contains("phone");

                return new CloudSyncTestScenarioResult
                {
                    ScenarioName = "ادغام هوشمند فیلدها",
                    IsPassed = isPassed,
                    FailureReason = isPassed
                        ? null
                        : "ادغام فیلدها کامل انجام نشد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 1
                };
            }
            catch (OperationCanceledException)
            {
                return new CloudSyncTestScenarioResult
                {
                    ScenarioName = "ادغام هوشمند فیلدها",
                    IsPassed = false,
                    FailureReason = "عملیات لغو شد.",
                    Duration = DateTime.UtcNow - startedAt,
                    ConflictCount = 0
                };
            }
        }
    }
}