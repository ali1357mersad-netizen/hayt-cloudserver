using System;

namespace Hayt.Services.CloudSync
{
    /// <summary>
    /// خطای موقت؛ قابل Retry است.
    /// </summary>
    public sealed class CloudSyncTransientException : Exception
    {
        public CloudSyncTransientException(string message)
            : base(message)
        {
        }

        public CloudSyncTransientException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// خطای دائمی؛ غیرقابل Retry است.
    /// </summary>
    public sealed class CloudSyncPermanentException : Exception
    {
        public CloudSyncPermanentException(string message)
            : base(message)
        {
        }

        public CloudSyncPermanentException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
