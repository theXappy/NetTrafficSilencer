using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;

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

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;    // Large icon
    private const uint SHGFI_SMALLICON = 0x000000001;    // Small icon
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static ImageSource GetLargeIcon(string filePath)
    {
        var shinfo = new SHFILEINFO();
        IntPtr hImg = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

        if (hImg == IntPtr.Zero)
            return null;

        ImageSource imgSource = Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        DestroyIcon(shinfo.hIcon); // Cleanup the unmanaged icon handle

        return imgSource;
    }

    public static ImageSource GetDefaultExeIcon()
    {
        var shinfo = new SHFILEINFO();
        IntPtr hImg = SHGetFileInfo(".exe", FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

        if (hImg == IntPtr.Zero)
            return null;

        ImageSource imgSource = Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        DestroyIcon(shinfo.hIcon); // Cleanup the unmanaged icon handle

        return imgSource;
    }
}