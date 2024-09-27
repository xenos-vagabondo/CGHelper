using CommonLibrary;
using System.Collections;

namespace CGHelper.CG
{
    public class WindowObject
    {
        public int Addr { get; set; }

        public int ParentAddr { get; set; }
        public int CallAddrPtr { get; set; }
        public int CallAddr { get; set; }

        public override string ToString()
        {
            return "ParentAddr = 0x" + ParentAddr.ToString("X") + " Window Addr = 0x" + Addr.ToString("X") + " CallAddr = 0x" + CallAddr.ToString("X");
        }

        public static ArrayList GetWindows(int hProcess)
        {
            return GetWindows(hProcess, false);
        }

        public static ArrayList GetWindows(int hProcess, bool onlyFirst)
        {
            ArrayList windowList = new ArrayList();

            int multipleWindowAddrPtr = CGAddr.MultipleWindowPtrAddr;
            while (true)
            {
                WinAPI.ReadProcessMemory(hProcess, multipleWindowAddrPtr, out int windowAddrPtr, 4, 0);
                if (windowAddrPtr == 0)
                {
                    break;
                }

                WinAPI.ReadProcessMemory(hProcess, windowAddrPtr + 0x28, out int windowMaxNumber, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, windowAddrPtr + 0x2C, out int windowAddr, 4, 0);

                for (int windowIndex = 0; windowIndex < windowMaxNumber; windowIndex++)
                {
                    WindowObject window = new WindowObject();
                    window.Addr = windowAddr + windowIndex * 9 * 4;
                    window.ParentAddr = windowAddrPtr;
                    window.CallAddrPtr = window.Addr + 0x20;
                    WinAPI.ReadProcessMemory(hProcess, window.CallAddrPtr, out int callAddr, 4, 0);
                    window.CallAddr = callAddr;
                    windowList.Add(window);

                    //Console.WriteLine("0x" + multipleWindowAddrPtr.ToString("X") + " " + window.ToString());
                }

                if (onlyFirst)
                {
                    break;
                }
                else
                {
                    multipleWindowAddrPtr += 4;
                }
            }

            return windowList;
        }

        public static ArrayList SearchWindow(int hProcess, int callAddr)
        {
            ArrayList windowList = new ArrayList();
            foreach (WindowObject window in GetWindows(hProcess))
            {
                if (window.CallAddr != callAddr)
                    continue;

                windowList.Add(window);
            }

            return windowList;
        }

        public static WindowObject SearchTopWindow(int hProcess, int callAddr)
        {
            foreach (WindowObject window in GetWindows(hProcess, true))
            {
                if (window.CallAddr == callAddr)
                    return window;

            }

            return null;
        }
    }
}
