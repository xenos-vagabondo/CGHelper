using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonLibrary
{
    public class WinAPI
    {
        public const int OPEN_PROCESS_ALL = 0x1F0FFF;
        public const int PROCESS_CREATE_THREAD = 0x2;
        public const int PROCESS_VM_OPERATION = 0x8;
        public const int PROCESS_VM_READ = 0x10;
        public const int PROCESS_VM_WRITE = 0x20;
        public const int PROCESS_WM_READ = 0x0010;
        public const int PROCESS_QUERY_INFORMATION = 0x400;

        [DllImport("user32", EntryPoint = "GetClassName")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32", EntryPoint = "GetWindowTextA")]
        public static extern int GetWindowTextA(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public class SearchData
        {
            public string Wndclass;
            public string Title;
            public IntPtr hWnd;
        }

        public delegate void EnumWindowsProc(IntPtr hWnd, ref SearchData data);

        [DllImport("user32", EntryPoint = "EnumWindows")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref SearchData data);

        [DllImport("kernel32", EntryPoint = "OpenProcess")]
        public static extern int OpenProcess(int dwDesiredAccess, int bInheritHandle, int dwProcessId);

        [DllImport("user32", EntryPoint = "GetWindowThreadProcessId")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, ref int lpdwProcessId);

        [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
        public static extern int WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesWritten);

        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead);

        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        public static extern int ReadProcessMemory(int hProcess, int lpBaseAddress, out int lpBuffer, int dwSize, int lpNumberOfBytesRead);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int CloseHandle(int hObject);

        public enum MEMMessage
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            MEM_FREE = 0x10000,
            MEM_PRIVATE = 0x20000,
            MEM_MAPPED = 0x40000,
            MEM_RESET = 0x80000,
            MEM_TOP_DOWN = 0x100000,
            MEM_WRITE_WATCH = 0x200000,
            MEM_PHYSICAL = 0x400000,
            MEM_ROTATE = 0x800000,
            MEM_LARGE_PAGES = 0x20000000,
        }

        public enum TokenAccessLevels
        {
            Query = 8,
            AdjustPrivileges = 32,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public int RegionSize;
            public MEMMessage State;
            public MEMMessage Protect;
            public int lType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID LUID;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccessLevels DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint Bufferlength, out TOKEN_PRIVILEGES PreviousState, out uint ReturnLength);

        [DllImport("kernel32")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            //ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        [DllImport("kernel32")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);



        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_MOUSEMOVE = 0x0200;

        [DllImport("user32", EntryPoint = "PostMessage")]
        public static extern IntPtr PostMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        public static int MAKELPARAM(int l, int h)
        {
            return ((h << 16) | (l & 0xFFFF));
        }

        //取得虛擬碼
        [DllImport("user32", EntryPoint = "MapVirtualKey")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static int MakeLParam(uint VirtualKey, string flag)
        {
            string LParam, FirstByte, SecondByte, OtherByte = "0001";
            if (flag == "WM_KEYDOWN")
                FirstByte = "00";
            else
                FirstByte = "C0";
            SecondByte = Convert.ToString(MapVirtualKey(VirtualKey, 0), 16);
            LParam = FirstByte + SecondByte + OtherByte;
            return Convert.ToInt32(LParam, 16);
        }

        public static int GetProcess(IntPtr hWnd)
        {
            int pid = 0;
            GetWindowThreadProcessId(hWnd, ref pid);
            return OpenProcess(OPEN_PROCESS_ALL, 0, pid);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32")]
        public static extern bool ScreenToClient(IntPtr hWnd, out POINT lpPoint);

        [DllImport("user32")]
        public static extern bool IsIconic(IntPtr hwnd);


        [DllImport("user32")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32")]
        public static extern IntPtr GetForegroundWindow();
    }
}
