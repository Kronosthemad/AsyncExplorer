using System;
using Microsoft.Win32;
namespace AsyncExplorer;

public static class AppSettings
    {
    private const string RegPath = @"Software\Kronos\AsyncExplorer";
    private const string LogTypeKey = "UseEventViewer";

    // Returns true for Event Viewer, false for Text File
    public static bool UseEventViewer
        {
        get
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (key != null)
                {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                object value = key.GetValue(name: LogTypeKey);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                if (value != null) return (int)value == 1;
                }
            return false; // Default to Text File
            }
        set
            {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                key.SetValue(LogTypeKey, value ? 1 : 0, RegistryValueKind.DWord);
                }
            }
        }
    }
