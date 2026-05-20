using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AsyncExplorer.Services
{
    public static class IconHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon? GetIcon(string path, bool isDirectory)
        {
            uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
            uint attributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr res = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                Icon icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
                DestroyIcon(shfi.hIcon);
                return icon;
            }
            return null;
        }
    }
}
