using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace FocusAttendance.Hardware;

/// <summary>
/// Makes the official ZKTeco Standalone SDK (64-bit zkemkeeper) usable without a system installer.
/// The DLLs ship beside the executable, and the COM class plus its type library are registered for
/// the current user only (HKCU), so no administrator rights are required.
/// </summary>
public static class ZkSdkBootstrapper
{
    public const string ProgId = "zkemkeeper.ZKEM";
    public const string ClassId = "{00853A19-BD51-419B-9269-2DABE57EB61F}";
    private const string FileName = "zkemkeeper.dll";

    /// <summary>Loads the SDK dependencies from the application folder and registers the COM server.</summary>
    public static bool EnsureRegistered(out string message)
    {
        if (!OperatingSystem.IsWindows())
        {
            message = "Windows only.";
            return false;
        }

        var folder = AppContext.BaseDirectory;
        SetDllDirectory(folder);
        PreloadDependencies(folder);

        var dll = Path.Combine(folder, FileName);
        if (!File.Exists(dll))
        {
            message = $"{FileName} is not present next to the application.";
            return false;
        }

        if (CanInstantiate())
        {
            message = "ZKTeco SDK detected and working.";
            return true;
        }

        try
        {
            WriteComRegistration(dll);
            RegisterTypeLibrary(dll, folder);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            message = "SDK registration failed: " + ex.Message;
            return false;
        }

        if (CanInstantiate())
        {
            message = "ZKTeco SDK registered for the current user (64-bit).";
            return true;
        }

        message = "ZKTeco SDK files are present but the COM class could not be loaded " +
                  "(0x8007007E means a missing dependency; the 64-bit build requires the whole sdk folder).";
        return false;
    }

    /// <summary>True when the SDK COM class can be created in this process.</summary>
    [SupportedOSPlatform("windows")]
    public static bool CanInstantiate()
    {
        object? com = null;
        try
        {
            var type = Type.GetTypeFromProgID(ProgId) ?? Type.GetTypeFromCLSID(new Guid(ClassId));
            if (type is null)
            {
                return false;
            }

            com = Activator.CreateInstance(type);
            return com is not null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"ZKTeco SDK COM instantiation failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (com is not null)
            {
                Marshal.FinalReleaseComObject(com);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void WriteComRegistration(string dll)
    {
        using (var progId = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
        {
            progId?.SetValue(null, "ZKEMKeeper Class");
            using var clsidKey = progId?.CreateSubKey("CLSID");
            clsidKey?.SetValue(null, ClassId);
        }

        using var clsid = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{ClassId}");
        clsid?.SetValue(null, "ZKEMKeeper Class");
        clsid?.SetValue("ProgID", ProgId);
        using var inproc = clsid?.CreateSubKey("InprocServer32");
        inproc?.SetValue(null, dll);
        inproc?.SetValue("ThreadingModel", "Apartment");
    }

    /// <summary>
    /// The CLR needs the SDK type library to build the COM wrapper; register it per user too.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void RegisterTypeLibrary(string dll, string folder)
    {
        if (LoadTypeLibEx(dll, RegKindNone, out var typeLib) != 0 || typeLib == IntPtr.Zero)
        {
            return;
        }

        var result = RegisterTypeLibForUser(typeLib, dll, folder);
        System.Diagnostics.Debug.WriteLine($"ZKTeco type library registration: 0x{result:X8}");
    }

    /// <summary>Preloads every SDK DLL from the application folder so imports resolve.</summary>
    [SupportedOSPlatform("windows")]
    private static void PreloadDependencies(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.dll"))
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith("zk", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("comm", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("rsc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            LoadLibraryEx(file, IntPtr.Zero, LoadWithAlteredSearchPath);
        }
    }

    private const int RegKindNone = 1;
    private const uint LoadWithAlteredSearchPath = 0x8;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int LoadTypeLibEx(string szFile, int regKind, out IntPtr typeLib);

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int RegisterTypeLibForUser(IntPtr typeLib, string fullPath, string helpDir);
}
