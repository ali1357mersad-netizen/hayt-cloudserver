using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// سیاست حل تعارض.
    /// </summary>
    public enum CloudSyncConflictPolicy
    {
        /// <summary>
        /// نسخه سرور برنده است.
        /// </summary>
        ServerWins = 0,

        /// <summary>
        /// نسخه کلاینت برنده است.
        /// </summary>
        ClientWins = 1,

        /// <summary>
        /// آخرین تغییر (بر اساس زمان) برنده است.
        /// </summary>
        LastWriteWins = 2,

        /// <summary>
        /// ادغام هوشمند فیلدها.
        /// </summary>
        Merge = 3
    }

    /// <summary>
    /// نوع تعارض.
    /// </summary>
    public enum CloudSyncConflictType
    {
        /// <summary>
        /// هر دو طرف یک موجودیت را تغییر داده‌اند.
        /// </summary>
        BothModified = 0,

        /// <summary>
        /// کلاینت حذف کرده ولی سرور تغییر داده است.
        /// </summary>
        ClientDeletedServerModified = 1,

        /// <summary>
        /// سرور حذف کرده ولی کلاینت تغییر داده است.
        /// </summary>
        ServerDeletedClientModified = 2
    }

    /// <summary>
    /// یک تعارض شناسایی‌شده.
    /// </summary>
    public sealed class CloudSyncConflict
    {
        public string EntityType { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public CloudSyncConflictType ConflictType { get; set; }

        public long ClientVersion { get; set; }

        public long ServerVersion { get; set; }

        public string ClientPayloadJson { get; set; } = "{}";

        public string ServerPayloadJson { get; set; } = "{}";

        public DateTimeOffset ClientChangedAtUtc { get; set; }

        public DateTimeOffset ServerChangedAtUtc { get; set; }
    }

    /// <summary>
    /// نتیجه حل تعارض.
    /// </summary>
    public sealed class CloudSyncConflictResolution
    {
        public bool IsResolved { get; set; }

        public string? WinningPayloadJson { get; set; }

        public long WinningVersion { get; set; }

        public string? ResolutionNote { get; set; }

        public bool RequiresUserDecision { get; set; }
    }

    /// <summary>
    /// حل‌کننده تعارض Cloud Sync.
    /// </summary>
    public sealed class CloudSyncConflictResolver
    {
        private readonly CloudSyncConflictPolicy _defaultPolicy;

        public CloudSyncConflictResolver(
            CloudSyncConflictPolicy defaultPolicy =
                CloudSyncConflictPolicy.LastWriteWins)
        {
            _defaultPolicy = defaultPolicy;
        }

        /// <summary>
        /// تشخیص تعارض بین نسخه کلاینت و سرور.
        /// </summary>
        public CloudSyncConflict? DetectConflict(
            string entityType,
            string entityId,
            long clientVersion,
            long serverVersion,
            string clientPayloadJson,
            string serverPayloadJson,
            DateTimeOffset clientChangedAtUtc,
            DateTimeOffset serverChangedAtUtc)
        {
            if (clientVersion == serverVersion)
            {
                return null;
            }

            bool clientIsDelete =
                IsDeleteOperation(clientPayloadJson);

            bool serverIsDelete =
                IsDeleteOperation(serverPayloadJson);

            CloudSyncConflictType conflictType;

            if (clientIsDelete && !serverIsDelete)
            {
                conflictType =
                    CloudSyncConflictType.ClientDeletedServerModified;
            }
            else if (serverIsDelete && !clientIsDelete)
            {
                conflictType =
                    CloudSyncConflictType.ServerDeletedClientModified;
            }
            else
            {
                conflictType =
                    CloudSyncConflictType.BothModified;
            }

            return new CloudSyncConflict
            {
                EntityType = entityType,
                EntityId = entityId,
                ConflictType = conflictType,
                ClientVersion = clientVersion,
                ServerVersion = serverVersion,
                ClientPayloadJson = clientPayloadJson,
                ServerPayloadJson = serverPayloadJson,
                ClientChangedAtUtc = clientChangedAtUtc,
                ServerChangedAtUtc = serverChangedAtUtc
            };
        }

        /// <summary>
        /// حل تعارض با سیاست مشخص.
        /// </summary>
        public CloudSyncConflictResolution Resolve(
            CloudSyncConflict conflict,
            CloudSyncConflictPolicy? policy = null)
        {
            if (conflict is null)
            {
                throw new ArgumentNullException(nameof(conflict));
            }

            CloudSyncConflictPolicy effectivePolicy =
                policy ?? _defaultPolicy;

            switch (effectivePolicy)
            {
                case CloudSyncConflictPolicy.ServerWins:
                    return ResolveServerWins(conflict);

                case CloudSyncConflictPolicy.ClientWins:
                    return ResolveClientWins(conflict);

                case CloudSyncConflictPolicy.LastWriteWins:
                    return ResolveLastWriteWins(conflict);

                case CloudSyncConflictPolicy.Merge:
                    return ResolveMerge(conflict);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(policy),
                        effectivePolicy,
                        "سیاست حل تعارض نامعتبر است.");
            }
        }

        /// <summary>
        /// حل تعارض با سیاست پیش‌فرض.
        /// </summary>
        public CloudSyncConflictResolution ResolveDefault(
            CloudSyncConflict conflict)
        {
            return Resolve(conflict, _defaultPolicy);
        }

        private CloudSyncConflictResolution ResolveServerWins(
            CloudSyncConflict conflict)
        {
            return new CloudSyncConflictResolution
            {
                IsResolved = true,
                WinningPayloadJson = conflict.ServerPayloadJson,
                WinningVersion = conflict.ServerVersion,
                ResolutionNote = "نسخه سرور برنده شد (ServerWins).",
                RequiresUserDecision = false
            };
        }

        private CloudSyncConflictResolution ResolveClientWins(
            CloudSyncConflict conflict)
        {
            return new CloudSyncConflictResolution
            {
                IsResolved = true,
                WinningPayloadJson = conflict.ClientPayloadJson,
                WinningVersion = conflict.ClientVersion,
                ResolutionNote = "نسخه کلاینت برنده شد (ClientWins).",
                RequiresUserDecision = false
            };
        }

        private CloudSyncConflictResolution ResolveLastWriteWins(
            CloudSyncConflict conflict)
        {
            bool clientIsNewer =
                conflict.ClientChangedAtUtc >=
                conflict.ServerChangedAtUtc;

            if (clientIsNewer)
            {
                return new CloudSyncConflictResolution
                {
                    IsResolved = true,
                    WinningPayloadJson = conflict.ClientPayloadJson,
                    WinningVersion = conflict.ClientVersion,
                    ResolutionNote =
                        "آخرین تغییر (کلاینت) برنده شد (LastWriteWins).",
                    RequiresUserDecision = false
                };
            }

            return new CloudSyncConflictResolution
            {
                IsResolved = true,
                WinningPayloadJson = conflict.ServerPayloadJson,
                WinningVersion = conflict.ServerVersion,
                ResolutionNote =
                    "آخرین تغییر (سرور) برنده شد (LastWriteWins).",
                RequiresUserDecision = false
            };
        }

        private CloudSyncConflictResolution ResolveMerge(
            CloudSyncConflict conflict)
        {
            try
            {
                JsonDocument clientDoc =
                    JsonDocument.Parse(conflict.ClientPayloadJson);

                JsonDocument serverDoc =
                    JsonDocument.Parse(conflict.ServerPayloadJson);

                Dictionary<string, JsonElement> merged =
                    new Dictionary<string, JsonElement>(
                        StringComparer.Ordinal);

                foreach (JsonProperty property in serverDoc.RootElement.EnumerateObject())
                {
                    merged[property.Name] = property.Value.Clone();
                }

                foreach (JsonProperty property in clientDoc.RootElement.EnumerateObject())
                {
                    merged[property.Name] = property.Value.Clone();
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    using (Utf8JsonWriter writer =
                        new Utf8JsonWriter(stream))
                    {
                        writer.WriteStartObject();

                        foreach (KeyValuePair<string, JsonElement> pair in merged)
                        {
                            pair.Value.WriteTo(writer);
                        }

                        writer.WriteEndObject();
                    }

                    string mergedJson =
                        System.Text.Encoding.UTF8.GetString(
                            stream.ToArray());

                    long winningVersion =
                        Math.Max(
                            conflict.ClientVersion,
                            conflict.ServerVersion);

                    return new CloudSyncConflictResolution
                    {
                        IsResolved = true,
                        WinningPayloadJson = mergedJson,
                        WinningVersion = winningVersion,
                        ResolutionNote =
                            "فیلدها ادغام شدند (Merge).",
                        RequiresUserDecision = false
                    };
                }
            }
            catch (JsonException)
            {
                return new CloudSyncConflictResolution
                {
                    IsResolved = false,
                    ResolutionNote =
                        "ادغام ناموفق بود؛ JSON نامعتبر است.",
                    RequiresUserDecision = true
                };
            }
        }

        private static bool IsDeleteOperation(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return false;
            }

            try
            {
                using (JsonDocument doc =
                    JsonDocument.Parse(payloadJson))
                {
                    if (doc.RootElement.TryGetProperty(
                            "isDeleted",
                            out JsonElement isDeleted))
                    {
                        return isDeleted.ValueKind ==
                            JsonValueKind.True;
                    }
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }
    }
}