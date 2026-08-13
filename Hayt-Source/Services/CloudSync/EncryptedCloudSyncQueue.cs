using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    public sealed class EncryptedCloudSyncQueue
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly string _queuePath;
        private readonly string _keyPath;
        private readonly SemaphoreSlim _gate =
            new SemaphoreSlim(1, 1);

        public EncryptedCloudSyncQueue(
            string applicationDataDirectory)
        {
            if (string.IsNullOrWhiteSpace(applicationDataDirectory))
            {
                throw new ArgumentException(
                    "مسیر ذخیره‌سازی نمی‌تواند خالی باشد.",
                    nameof(applicationDataDirectory));
            }

            string syncDirectory = Path.Combine(
                applicationDataDirectory,
                "CloudSync");

            Directory.CreateDirectory(syncDirectory);

            _queuePath = Path.Combine(
                syncDirectory,
                "queue.bin");

            _keyPath = Path.Combine(
                syncDirectory,
                "queue.key");
        }

        public async Task AddAsync(
            CloudSyncQueueItem item,
            CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ValidateItem(item);

            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<CloudSyncQueueItem> items =
                    await ReadInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                items.Add(item);

                await WriteInternalAsync(
                        items,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<IReadOnlyList<CloudSyncQueueItem>> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<CloudSyncQueueItem> items =
                    await ReadInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                return items
                    .OrderBy(item => item.CreatedAtUtc)
                    .ToArray();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task RemoveAsync(
            IEnumerable<Guid> completedIds,
            CancellationToken cancellationToken = default)
        {
            if (completedIds == null)
            {
                throw new ArgumentNullException(
                    nameof(completedIds));
            }

            HashSet<Guid> ids =
                new HashSet<Guid>(completedIds);

            if (ids.Count == 0)
            {
                return;
            }

            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<CloudSyncQueueItem> items =
                    await ReadInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                items.RemoveAll(item => ids.Contains(item.Id));

                await WriteInternalAsync(
                        items,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task UpdateItemAsync(
            CloudSyncQueueItem item,
            CancellationToken cancellationToken = default)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await _gate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<CloudSyncQueueItem> items =
                    await ReadInternalAsync(cancellationToken)
                        .ConfigureAwait(false);

                int index = items.FindIndex(
                    x => x.Id == item.Id);

                if (index >= 0)
                {
                    items[index] = item;
                    await WriteInternalAsync(
                            items,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<List<CloudSyncQueueItem>> ReadInternalAsync(
            CancellationToken cancellationToken)
        {
            if (!File.Exists(_queuePath))
            {
                return new List<CloudSyncQueueItem>();
            }

            byte[] encryptedPackage =
                await File.ReadAllBytesAsync(
                        _queuePath,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (encryptedPackage.Length <
                NonceSize + TagSize + 1)
            {
                throw new InvalidDataException(
                    "فایل صف Cloud Sync معتبر نیست.");
            }

            byte[] key =
                await GetOrCreateKeyAsync(cancellationToken)
                    .ConfigureAwait(false);

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];

            int cipherLength =
                encryptedPackage.Length -
                NonceSize -
                TagSize;

            byte[] cipherText = new byte[cipherLength];
            byte[] plainText = new byte[cipherLength];

            Buffer.BlockCopy(
                encryptedPackage,
                0,
                nonce,
                0,
                NonceSize);

            Buffer.BlockCopy(
                encryptedPackage,
                NonceSize,
                tag,
                0,
                TagSize);

            Buffer.BlockCopy(
                encryptedPackage,
                NonceSize + TagSize,
                cipherText,
                0,
                cipherLength);

            using (AesGcm aes = new AesGcm(key, TagSize))
            {
                aes.Decrypt(
                    nonce,
                    cipherText,
                    tag,
                    plainText);
            }

            List<CloudSyncQueueItem>? result =
                JsonSerializer.Deserialize<
                    List<CloudSyncQueueItem>>(plainText);

            return result ??
                new List<CloudSyncQueueItem>();
        }

        private async Task WriteInternalAsync(
            List<CloudSyncQueueItem> items,
            CancellationToken cancellationToken)
        {
            byte[] key =
                await GetOrCreateKeyAsync(cancellationToken)
                    .ConfigureAwait(false);

            byte[] plainText =
                JsonSerializer.SerializeToUtf8Bytes(items);

            byte[] nonce =
                RandomNumberGenerator.GetBytes(NonceSize);

            byte[] tag = new byte[TagSize];
            byte[] cipherText =
                new byte[plainText.Length];

            using (AesGcm aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(
                    nonce,
                    plainText,
                    cipherText,
                    tag);
            }

            byte[] package = new byte[
                nonce.Length +
                tag.Length +
                cipherText.Length];

            Buffer.BlockCopy(
                nonce,
                0,
                package,
                0,
                nonce.Length);

            Buffer.BlockCopy(
                tag,
                0,
                package,
                nonce.Length,
                tag.Length);

            Buffer.BlockCopy(
                cipherText,
                0,
                package,
                nonce.Length + tag.Length,
                cipherText.Length);

            string temporaryPath = _queuePath + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(
                        temporaryPath,
                        package,
                        cancellationToken)
                    .ConfigureAwait(false);

                File.Move(
                    temporaryPath,
                    _queuePath,
                    true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private async Task<byte[]> GetOrCreateKeyAsync(
            CancellationToken cancellationToken)
        {
            if (File.Exists(_keyPath))
            {
                byte[] storedKey =
                    await File.ReadAllBytesAsync(
                            _keyPath,
                            cancellationToken)
                        .ConfigureAwait(false);

                try
                {
                    byte[] key = UnprotectKey(storedKey);
                    ValidateKey(key);
                    return key;
                }
                catch (CryptographicException)
                {
                    if (storedKey.Length == KeySize)
                    {
                        ValidateKey(storedKey);

                        byte[] protectedKey = ProtectKey(storedKey);
                        await WriteKeyAtomicallyAsync(
                                protectedKey,
                                cancellationToken)
                            .ConfigureAwait(false);

                        return storedKey;
                    }

                    throw new InvalidDataException(
                        "کلید رمزگذاری صف Cloud Sync معتبر نیست.");
                }
            }

            byte[] newKey =
                RandomNumberGenerator.GetBytes(KeySize);

            byte[] protectedNewKey = ProtectKey(newKey);

            await WriteKeyAtomicallyAsync(
                    protectedNewKey,
                    cancellationToken)
                .ConfigureAwait(false);

            return newKey;
        }

        private static byte[] ProtectKey(byte[] key)
        {
            return ProtectedData.Protect(
                key,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);
        }

        private static byte[] UnprotectKey(byte[] protectedKey)
        {
            return ProtectedData.Unprotect(
                protectedKey,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);
        }

        private async Task WriteKeyAtomicallyAsync(
            byte[] protectedKey,
            CancellationToken cancellationToken)
        {
            string temporaryPath =
                _keyPath + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(
                        temporaryPath,
                        protectedKey,
                        cancellationToken)
                    .ConfigureAwait(false);

                File.Move(
                    temporaryPath,
                    _keyPath,
                    true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null || key.Length != KeySize)
            {
                throw new InvalidDataException(
                    "کلید رمزگذاری صف Cloud Sync معتبر نیست.");
            }
        }

        private static void ValidateItem(
            CloudSyncQueueItem item)
        {
            if (item.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "شناسه عملیات Cloud Sync معتبر نیست.",
                    nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.EntityType))
            {
                throw new ArgumentException(
                    "نوع موجودیت نمی‌تواند خالی باشد.",
                    nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.EntityId))
            {
                throw new ArgumentException(
                    "شناسه موجودیت نمی‌تواند خالی باشد.",
                    nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.PayloadJson))
            {
                item.PayloadJson = "{}";
            }

            using (JsonDocument document =
                JsonDocument.Parse(item.PayloadJson))
            {
                // فقط اعتبار JSON بررسی می‌شود.
            }
        }
    }
}

