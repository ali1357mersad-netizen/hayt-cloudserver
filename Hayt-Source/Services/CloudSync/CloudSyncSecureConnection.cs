using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// نتیجه اعتبارسنجی گواهی.
    /// </summary>
    public sealed class CloudSyncCertificateValidationResult
    {
        public bool IsValid { get; set; }

        public string? Reason { get; set; }

        public string? CertificateSubject { get; set; }

        public DateTimeOffset? CertificateExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// مدیر اتصال امن HTTPS و TLS.
    /// </summary>
    public sealed class CloudSyncSecureConnection
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _pinnedFingerprints;
        private readonly bool _enableCertificatePinning;

        /// <summary>
        /// ایجاد مدیر اتصال امن.
        /// </summary>
        public CloudSyncSecureConnection(
            string baseAddress,
            string[]? pinnedFingerprints = null,
            bool enableCertificatePinning = true,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                throw new ArgumentException(
                    "آدرس پایه نمی‌تواند خالی باشد.",
                    nameof(baseAddress));
            }

            if (!baseAddress.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "آدرس پایه باید با https:// شروع شود.",
                    nameof(baseAddress));
            }

            _pinnedFingerprints =
                pinnedFingerprints ?? Array.Empty<string>();

            _enableCertificatePinning =
                enableCertificatePinning;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    ValidateServerCertificate,
                SslProtocols =
                    SslProtocols.Tls12 |
                    SslProtocols.Tls13,
                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress, UriKind.Absolute)
            };

            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            if (timeout.HasValue)
            {
                _httpClient.Timeout = timeout.Value;
            }
            else
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }
        }

        /// <summary>
        /// ارسال درخواست GET امن.
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri))
            {
                return await _httpClient
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ارسال درخواست POST امن.
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(
            string requestUri,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri))
            {
                request.Content = content;

                return await _httpClient
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ارسال درخواست PUT امن.
        /// </summary>
        public async Task<HttpResponseMessage> PutAsync(
            string requestUri,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    requestUri))
            {
                request.Content = content;

                return await _httpClient
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ارسال درخواست DELETE امن.
        /// </summary>
        public async Task<HttpResponseMessage> DeleteAsync(
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Delete,
                    requestUri))
            {
                return await _httpClient
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// اعتبارسنجی گواهی سرور.
        /// </summary>
        public CloudSyncCertificateValidationResult ValidateCertificate(
            string host,
            X509Certificate2 certificate)
        {
            if (certificate is null)
            {
                return new CloudSyncCertificateValidationResult
                {
                    IsValid = false,
                    Reason = "گواهی سرور دریافت نشد."
                };
            }

            if (certificate.NotAfter < DateTime.UtcNow)
            {
                return new CloudSyncCertificateValidationResult
                {
                    IsValid = false,
                    Reason = "گواهی سرور منقضی شده است.",
                    CertificateSubject = certificate.Subject,
                    CertificateExpiresAtUtc =
                        certificate.NotAfter
                };
            }

            if (certificate.NotBefore > DateTime.UtcNow)
            {
                return new CloudSyncCertificateValidationResult
                {
                    IsValid = false,
                    Reason = "گواهی سرور هنوز معتبر نشده است.",
                    CertificateSubject = certificate.Subject,
                    CertificateExpiresAtUtc =
                        certificate.NotAfter
                };
            }

            if (_enableCertificatePinning &&
                _pinnedFingerprints.Length > 0)
            {
                string fingerprint =
                    certificate.GetCertHashString();

                bool fingerprintMatched = false;

                foreach (string pinned in _pinnedFingerprints)
                {
                    if (string.Equals(
                            fingerprint,
                            pinned,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        fingerprintMatched = true;
                        break;
                    }
                }

                if (!fingerprintMatched)
                {
                    return new CloudSyncCertificateValidationResult
                    {
                        IsValid = false,
                        Reason =
                            "اثر انگشت گواهی با پین مطابقت ندارد.",
                        CertificateSubject = certificate.Subject,
                        CertificateExpiresAtUtc =
                            certificate.NotAfter
                    };
                }
            }

            return new CloudSyncCertificateValidationResult
            {
                IsValid = true,
                Reason = "گواهی معتبر است.",
                CertificateSubject = certificate.Subject,
                CertificateExpiresAtUtc =
                    certificate.NotAfter
            };
        }

        /// <summary>
        /// آزادسازی منابع.
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private bool ValidateServerCertificate(
            HttpRequestMessage request,
            X509Certificate2? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate is null)
            {
                return false;
            }

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }

            if (_enableCertificatePinning &&
                _pinnedFingerprints.Length > 0)
            {
                string fingerprint =
                    certificate.GetCertHashString();

                foreach (string pinned in _pinnedFingerprints)
                {
                    if (string.Equals(
                            fingerprint,
                            pinned,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}