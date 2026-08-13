using Hayt.Licensing.Services;
using System;
using Hayt.Licensing.Models;

namespace Hayt.Licensing.Services;

public interface ILicenseService
{
    LicenseState Current { get; }

    event EventHandler? LicenseChanged;

    LicenseState Load();

    LicenseValidationResult ValidateCurrent();

    LicenseValidationResult Activate(
        string licenseJsonBase64,
        string signatureBase64);

    LicenseValidationResult ActivateFromCombinedText(string combinedText);

    void Deactivate();

    bool HasPremiumAccess();

    LicensePlan GetEffectivePlan();

    string GetMachineId();

    string GetStateFilePath();

    string CreateUnsignedLicensePayloadJson(
        string userName,
        string userEmail,
        LicensePlan plan,
        DateTime? expiresAt,
        bool bindToThisMachine);
}

