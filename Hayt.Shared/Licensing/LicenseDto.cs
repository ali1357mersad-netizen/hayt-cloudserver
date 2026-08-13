using System;

namespace Hayt.Shared.Licensing;

public sealed class LicenseDto
{
    public string LicenseId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public LicensePlan Plan { get; set; } = LicensePlan.Free;
    public LicenseStatus Status { get; set; } = LicenseStatus.Unknown;
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public bool IsActive =>
        Status == LicenseStatus.Active &&
        (ExpiresAtUtc == null || ExpiresAtUtc > DateTimeOffset.UtcNow);
}
