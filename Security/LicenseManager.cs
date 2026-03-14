using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class LicenseManager
{
    private static readonly string TimeFile =
        Path.Combine(AppContext.BaseDirectory, ".runtime");

    private static readonly TimeSpan AllowedDrift = TimeSpan.FromMinutes(10);

    static void CheckTimeRollback()
    {
        var now = DateTime.UtcNow;

        if (File.Exists(TimeFile))
        {
            if (DateTime.TryParse(File.ReadAllText(TimeFile),
                                null,
                                System.Globalization.DateTimeStyles.RoundtripKind,
                                out DateTime last))
            {
                // Only trigger if LARGE backward jump
                if (last - now > AllowedDrift)
                    throw new Exception("System clock rollback detected");
            }
        }

        File.WriteAllText(TimeFile, now.ToString("O"));
    }


    private static string PublicKey = File.ReadAllText("Licensing/public.key");

    public static bool Validate(out string message)
    {
        if (!File.Exists("license.dat"))
        {
            message = "License file missing";
            return false;
        }

        var json = File.ReadAllText("license.dat");
        var lic = JsonSerializer.Deserialize<License>(json);

        string currentHw = HardwareFingerprint.Generate();

        if (currentHw != lic.HardwareId)
        {
            message = "License not for this machine";
            return false;
        }

        try
        {
            CheckTimeRollback();
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }

        if (DateTime.Now > DateTime.Parse(lic.Expiry).ToUniversalTime())
        {
            message = "License expired";
            return false;
        }

        var data = lic.HardwareId + "|" + lic.Expiry;

        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(PublicKey), out _);

        bool valid = rsa.VerifyData(
            Encoding.UTF8.GetBytes(data),
            Convert.FromBase64String(lic.Signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        message = valid ? "OK" : "License tampered";
        return valid;
    }

    private class License
    {
        public string HardwareId { get; set; }
        public string Expiry { get; set; }
        public string Signature { get; set; }
    }
}
