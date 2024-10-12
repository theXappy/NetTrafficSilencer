using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public static class ProcessAccessHelper
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        QueryInformation = 0x400,
        QueryLimitedInformation = 0x1000,
        VMRead = 0x10,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    public static bool CanAccessProcess(Process process)
    {
        IntPtr handle = IntPtr.Zero;

        try
        {
            // Try to open the process with minimal access rights
            handle = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead, false, process.Id);
            return handle != IntPtr.Zero;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                CloseHandle(handle);
            }
        }
    }

    // Method to get the executable path of a process using its process ID
    public static string GetExecutablePath(Process process)
    {
        IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);
        if (hProcess == IntPtr.Zero)
            return null;

        try
        {
            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
            {
                return buffer.ToString();
            }
            return null;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }
}
