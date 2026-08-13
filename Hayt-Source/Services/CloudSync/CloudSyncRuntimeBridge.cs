using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    public static class CloudSyncRuntimeBridge
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static bool _initialized;
        private static Func<bool>? _hasPremiumAccess;
        private static EncryptedCloudSyncQueue? _queue;
        private static object? _notesAdapter;
        private static object? _progressAdapter;
        private static object? _certificateAdapter;

        public static bool IsInitialized { get { return _initialized; } }

        public static void TryInitializeFromApplication(object? application)
        {
            try
            {
                string appDataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Hayt");
                _queue = new EncryptedCloudSyncQueue(appDataDirectory);
                _hasPremiumAccess = BuildPremiumAccessProbe(application);
                TryCreateAdapters(application, appDataDirectory);
                _initialized = true;
            }
            catch
            {
                _initialized = false;
                _queue = null;
                _hasPremiumAccess = null;
                _notesAdapter = null;
                _progressAdapter = null;
                _certificateAdapter = null;
            }
        }

        public static async Task TrackNoteChangedAsync(string operation, object? note, object? extra = null)
        {
            await TrackAsync("StudyNote", ExtractEntityId(note, extra), operation, note, extra, _notesAdapter).ConfigureAwait(false);
        }

        public static async Task TrackLessonProgressSavedAsync(object? progress, object? extra = null)
        {
            await TrackAsync("LessonProgress", ExtractEntityId(progress, extra), "SaveLessonProgress", progress, extra, _progressAdapter).ConfigureAwait(false);
        }

        public static async Task TrackCertificateIssuedAsync(object? certificate, object? extra = null)
        {
            await TrackAsync("Certificate", ExtractEntityId(certificate, extra), "CertificateIssued", certificate, extra, _certificateAdapter).ConfigureAwait(false);
        }

        private static async Task TrackAsync(string entityType, string entityId, string operation, object? payload, object? extra, object? preferredAdapter)
        {
            try
            {
                if (!_initialized) return;
                if (_hasPremiumAccess == null || !_hasPremiumAccess()) return;
                if (_queue == null) return;

                await TryInvokeAdapterAsync(preferredAdapter, payload, extra).ConfigureAwait(false);

                string json = BuildPayloadJson(entityType, entityId, operation, payload, extra);
                CloudSyncQueueItem item = new CloudSyncQueueItem
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    OperationType = ResolveOperationType(operation),
                    PayloadJson = json,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    RetryCount = 0
                };

                await Gate.WaitAsync().ConfigureAwait(false);
                try { await _queue.AddAsync(item).ConfigureAwait(false); }
                finally { Gate.Release(); }
            }
            catch { }
        }

        private static CloudSyncOperationType ResolveOperationType(string operation)
        {
            if (operation.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 || operation.IndexOf("remove", StringComparison.OrdinalIgnoreCase) >= 0)
                return CloudSyncOperationType.Delete;
            if (operation.IndexOf("add", StringComparison.OrdinalIgnoreCase) >= 0 || operation.IndexOf("create", StringComparison.OrdinalIgnoreCase) >= 0 || operation.IndexOf("issue", StringComparison.OrdinalIgnoreCase) >= 0)
                return CloudSyncOperationType.Create;
            return CloudSyncOperationType.Update;
        }

        private static string BuildPayloadJson(string entityType, string entityId, string operation, object? payload, object? extra)
        {
            var envelope = new
            {
                entityType, entityId, operation,
                capturedAtUtc = DateTimeOffset.UtcNow,
                payloadType = payload == null ? null : payload.GetType().FullName,
                payload = SafeObjectToDictionary(payload),
                extraType = extra == null ? null : extra.GetType().FullName,
                extra = SafeObjectToDictionary(extra)
            };
            return JsonSerializer.Serialize(envelope);
        }

        private static object? SafeObjectToDictionary(object? value)
        {
            try
            {
                if (value == null) return null;
                Type type = value.GetType();
                if (type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(decimal))
                    return value.ToString();
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Take(40)
                    .ToDictionary(p => p.Name, p => { try { object? v = p.GetValue(value); if (v == null) return null; Type pt = v.GetType(); if (pt == typeof(string) || pt.IsPrimitive || pt.IsEnum || pt == typeof(Guid) || pt == typeof(DateTime) || pt == typeof(DateTimeOffset) || pt == typeof(decimal)) return v; return v.ToString(); } catch { return null; } });
            }
            catch { return null; }
        }

        private static string ExtractEntityId(object? payload, object? extra)
        {
            string? id = TryExtractId(payload) ?? TryExtractId(extra);
            if (!string.IsNullOrWhiteSpace(id)) return id;
            return Guid.NewGuid().ToString("N");
        }

        private static string? TryExtractId(object? value)
        {
            try
            {
                if (value == null) return null;
                if (value is string text) return string.IsNullOrWhiteSpace(text) ? null : text;
                Type type = value.GetType();
                foreach (string name in new[] { "Id", "ID", "NoteId", "LessonId", "BookId", "UserId", "CertificateId" })
                {
                    PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (property != null && property.CanRead)
                    {
                        object? propertyValue = property.GetValue(value);
                        if (propertyValue != null)
                        {
                            string result = Convert.ToString(propertyValue) ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(result)) return result;
                        }
                    }
                }
            }
            catch { return null; }
            return null;
        }

        private static Func<bool> BuildPremiumAccessProbe(object? application)
        {
            object? premiumObject = FindObjectByTypeOrName(application, "PremiumAccessService");
            if (premiumObject == null) premiumObject = FindObjectInLoadedAssemblies("PremiumAccessService");
            if (premiumObject == null) return () => false;

            MethodInfo? method = FindBooleanMethod(premiumObject.GetType(), new[] { "HasPremiumAccess", "IsPremium", "IsPremiumUser", "CanUsePremium", "HasActivePremium", "HasPremium" });
            if (method == null)
            {
                PropertyInfo? property = FindBooleanProperty(premiumObject.GetType(), new[] { "HasPremiumAccess", "IsPremium", "IsPremiumUser", "HasActivePremium", "HasPremium" });
                if (property == null) return () => false;
                return () => { try { object? result = property.GetValue(premiumObject); return result is bool b && b; } catch { return false; } };
            }
            return () => { try { object? result = method.Invoke(premiumObject, null); return result is bool b && b; } catch { return false; } };
        }

        private static void TryCreateAdapters(object? application, string appDataDirectory)
        {
            try
            {
                Type? factoryType = FindType("CloudSyncEventTrackerFactory");
                if (factoryType == null) return;
                MethodInfo? createDefault = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "CreateDefault");
                object? tracker = null;
                if (createDefault != null)
                {
                    ParameterInfo[] parameters = createDefault.GetParameters();
                    object?[] args = new object?[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType == typeof(string)) args[i] = appDataDirectory;
                        else if (parameterType == typeof(Func<bool>)) args[i] = _hasPremiumAccess ?? (() => false);
                        else args[i] = FindObjectByAssignableType(application, parameterType) ?? FindObjectInLoadedAssemblies(parameterType.Name);
                    }
                    tracker = createDefault.Invoke(null, args);
                }
                if (tracker == null) return;
                _notesAdapter = TryCreateAdapter("CloudSyncNotesAdapter", tracker);
                _progressAdapter = TryCreateAdapter("CloudSyncProgressAdapter", tracker);
                _certificateAdapter = TryCreateAdapter("CloudSyncCertificateAdapter", tracker);
            }
            catch { _notesAdapter = null; _progressAdapter = null; _certificateAdapter = null; }
        }

        private static object? TryCreateAdapter(string typeName, object tracker)
        {
            try
            {
                Type? type = FindType(typeName);
                if (type == null) return null;
                ConstructorInfo? ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
                if (ctor == null) return null;
                ParameterInfo[] parameters = ctor.GetParameters();
                object?[] args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsInstanceOfType(tracker)) args[i] = tracker;
                    else if (parameters[i].ParameterType == typeof(Func<bool>)) args[i] = _hasPremiumAccess ?? (() => false);
                    else args[i] = null;
                }
                return ctor.Invoke(args);
            }
            catch { return null; }
        }

        private static async Task TryInvokeAdapterAsync(object? adapter, object? payload, object? extra)
        {
            try
            {
                if (adapter == null) return;
                MethodInfo? method = adapter.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name.StartsWith("On", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.GetParameters().Length).FirstOrDefault();
                if (method == null) return;
                ParameterInfo[] parameters = method.GetParameters();
                object?[] args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (payload != null && parameterType.IsInstanceOfType(payload)) args[i] = payload;
                    else if (extra != null && parameterType.IsInstanceOfType(extra)) args[i] = extra;
                    else if (parameterType == typeof(string)) args[i] = Convert.ToString(TryExtractId(payload) ?? TryExtractId(extra) ?? string.Empty);
                    else if (parameterType == typeof(Guid)) args[i] = Guid.NewGuid();
                    else if (parameterType == typeof(DateTime)) args[i] = DateTime.UtcNow;
                    else if (parameterType == typeof(DateTimeOffset)) args[i] = DateTimeOffset.UtcNow;
                    else if (parameterType.IsValueType) args[i] = Activator.CreateInstance(parameterType);
                    else args[i] = null;
                }
                object? result = method.Invoke(adapter, args);
                if (result is Task task) await task.ConfigureAwait(false);
            }
            catch { }
        }

        private static Type? FindType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase) || string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase));
                if (type != null) return type;
            }
            return null;
        }

        private static object? FindObjectByTypeOrName(object? root, string typeOrName)
        {
            if (root == null) return null;
            try
            {
                Type type = root.GetType();
                if (type.Name.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0) return root;
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    object? value = field.GetValue(field.IsStatic ? null : root);
                    if (value != null && value.GetType().Name.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0) return value;
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
                    object? value = property.GetValue(property.GetMethod != null && property.GetMethod.IsStatic ? null : root);
                    if (value != null && value.GetType().Name.IndexOf(typeOrName, StringComparison.OrdinalIgnoreCase) >= 0) return value;
                }
            }
            catch { return null; }
            return null;
        }

        private static object? FindObjectByAssignableType(object? root, Type targetType)
        {
            if (root == null) return null;
            try
            {
                Type type = root.GetType();
                if (targetType.IsInstanceOfType(root)) return root;
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    object? value = field.GetValue(field.IsStatic ? null : root);
                    if (value != null && targetType.IsInstanceOfType(value)) return value;
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
                    object? value = property.GetValue(property.GetMethod != null && property.GetMethod.IsStatic ? null : root);
                    if (value != null && targetType.IsInstanceOfType(value)) return value;
                }
            }
            catch { return null; }
            return null;
        }

        private static object? FindObjectInLoadedAssemblies(string typeName) { return null; }

        private static MethodInfo? FindBooleanMethod(Type type, string[] methodNames)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .FirstOrDefault(m => methodNames.Any(n => string.Equals(n, m.Name, StringComparison.OrdinalIgnoreCase)) && m.GetParameters().Length == 0 && m.ReturnType == typeof(bool));
        }

        private static PropertyInfo? FindBooleanProperty(Type type, string[] propertyNames)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .FirstOrDefault(p => propertyNames.Any(n => string.Equals(n, p.Name, StringComparison.OrdinalIgnoreCase)) && p.GetIndexParameters().Length == 0 && p.PropertyType == typeof(bool));
        }
    }
}