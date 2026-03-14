using System.Management;
using System.Security.Cryptography;
using System.Text;

public static class HardwareFingerprint
{
    public static string Generate()
    {
        string raw =
            Get("Win32_Processor", "ProcessorId") +
            Get("Win32_BIOS", "SerialNumber") +
            Get("Win32_BaseBoard", "SerialNumber") +
            Get("Win32_DiskDrive", "SerialNumber");

        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

    private static string Get(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (ManagementObject obj in searcher.Get())
                return obj[property]?.ToString()?.Trim() ?? "";
        }
        catch { }
        return "";
    }
}
