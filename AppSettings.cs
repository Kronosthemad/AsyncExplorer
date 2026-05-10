using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AsyncExplorer
    {
    [SupportedOSPlatform("windows")]
    public static class AppSettings
        {
        private const string RegPath = @"Software\Kronos\AsyncExplorer";
        private const string LogTypeKey = "UseEventViewer";
        private const string ShowHiddenKey = "ShowHiddenFiles";
        private const string UseIconsKey = "UseIcons";

        public static bool ShowHiddenFiles
            {
            get
                {
                try
                    {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegPath);
                    if (key != null)
                        {
                        object? value = key.GetValue(ShowHiddenKey);
                        if (value is int intValue) return intValue == 1;
                        }
                    }
                catch { }
                return false;
                }
            set
                {
                try
                    {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath);
                    key.SetValue(ShowHiddenKey, value ? 1 : 0, RegistryValueKind.DWord);
                    }
                catch { }
                }
            }

        public static bool UseEventViewer
            {
            get
                {
                try
                    {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegPath);
                    if (key != null)
                        {
                        object? value = key.GetValue(LogTypeKey);
                        if (value is int intValue) return intValue == 1;
                        }
                    }
                catch { }
                return false;
                }
            set
                {
                try
                    {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath);
                    key.SetValue(LogTypeKey, value ? 1 : 0, RegistryValueKind.DWord);
                    }
                catch { }
                }
            }

        public static bool UseIcons
            {
            get
                {
                try
                    {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegPath);
                    if (key != null)
                        {
                        object? value = key.GetValue(UseIconsKey);
                        if (value is int intValue) return intValue == 1;
                        }
                    }
                catch { }
                return true; // Default to true
                }
            set
                {
                try
                    {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath);
                    key.SetValue(UseIconsKey, value ? 1 : 0, RegistryValueKind.DWord);
                    }
                catch { }
                }
            }

        public static bool HomeOrRoot
            {
            get
                {
                try
                    {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegPath);
                    if (key != null)
                        {
                        object? value = key.GetValue("HomeOrRoot");
                        if (value is int intValue) return intValue == 1;
                        }
                    }
                catch { }
                return true; // Default to true (Home)
                }
            set
                {
                try
                    {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath);
                    key.SetValue("HomeOrRoot", value ? 1 : 0, RegistryValueKind.DWord);
                    }
                catch { }

                }
            }
        }
    }
