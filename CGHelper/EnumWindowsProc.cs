using System;
using System.Collections;
using System.Text;

namespace CommonLibrary
{
    public class EnumWindowsProc
    {
        Hashtable Ht { get; set; } = new Hashtable();

        public Hashtable SearchForWindow(string wndclass, string title)
        {
            WinAPI.SearchData sd = new WinAPI.SearchData { Wndclass = wndclass, Title = title };
            WinAPI.EnumWindows(new WinAPI.EnumWindowsProc(EnumProc), ref sd);

            return Ht;
        }

        private void EnumProc(IntPtr hWnd, ref WinAPI.SearchData data)
        {
            StringBuilder classBuffer = new StringBuilder(1024);
            WinAPI.GetClassName(hWnd, classBuffer, classBuffer.Capacity);
            StringBuilder titleBuffer = new StringBuilder(1024);
            WinAPI.GetWindowTextA(hWnd, titleBuffer, titleBuffer.Capacity);

            if (classBuffer.ToString().Equals(data.Wndclass))
            {
                Ht.Add(hWnd, data.Wndclass);
                //Console.WriteLine("0x" + hWnd.ToString("X") + " " + titleBuffer.ToString());
            } 
            else
            {
                string gbClass = StrToSimplified(classBuffer.ToString());
                if (gbClass.Equals(data.Wndclass))
                {
                    Ht.Add(hWnd, data.Wndclass);
                    //Console.WriteLine("0x" + hWnd.ToString("X") + " " + titleBuffer.ToString());
                }
            }
        }

        private static string StrToSimplified(string intputStr)
        {
            byte[] strByte = Encoding.Default.GetBytes(intputStr);
            return Encoding.GetEncoding("GB2312").GetString(strByte);
        }
    }
}
