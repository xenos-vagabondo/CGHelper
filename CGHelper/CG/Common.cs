using CGHelper.CG.Enum;
using CommonLibrary;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace CGHelper.CG
{
    class Common
    {
        public static bool GB2312 { get; set; }

        private static Stopwatch Timer { get; set; } = new Stopwatch();
        private static object LockObject { get; set; } = new object();

        public static int DelayTime { get; set; }

        public static void Delay(int delayTime)
        {
            SpinWait.SpinUntil(() => false, delayTime);
            //Thread.Sleep(delayTime);
        }

        public static string GetNameFromAddr(int hProcess, int addr)
        {
            return GetStringFromAddr(hProcess, addr);
        }

        public static string GetStringFromAddr(int hProcess, int addr)
        {
            return GetStringFromAddr(hProcess, addr, GB2312);
        }

        public static string GetStringFromAddr(int hProcess, int addr, bool useGB2312)
        {
            int output, length = 0;
            do
            {
                WinAPI.ReadProcessMemory(hProcess, addr + length, out output, 1, 0);
                length++;
            } while (output != 0);

            length = length - 1;

            if (length == 0)
                return null;

            byte[] str = new byte[length];
            WinAPI.ReadProcessMemory(hProcess, addr, str, length, 0);

            if (useGB2312)
            {
                return ChineseConverter.ToTraditional(Encoding.GetEncoding("GB2312").GetString(str));
            }

            return Encoding.Default.GetString(str);
        }

        public static string GetRoleName(int hProcess)
        {
            return GetNameFromAddr(hProcess, CGAddr.RoleNameAddr);
        }

        public static int GetXORValue(int hProcess, int addr)
        {
            WinAPI.ReadProcessMemory(hProcess, addr + 0xC, out int temp, 4, 0);
            WinAPI.ReadProcessMemory(hProcess, addr + 0x4, out int value1, 4, 0);
            WinAPI.ReadProcessMemory(hProcess, addr + 0x8, out int value2, 4, 0);
            return value1 ^ value2;
        }

        public static long GetUnixTimeMilliseconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static RoleDirection GetTargetDirection(IntPtr hWnd, Location NPClocation)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            Location location = Location.GetLocation(hProcess);

            var r = Math.Atan2(NPClocation.X - location.X, NPClocation.Y - location.Y) / Math.PI * 180;
            if (r < -135 - 22.5 || (r <= 180 + 22.5 && r >= 180 - 22.5))
            {
                //↖
                return RoleDirection.N;
            }
            else if (r <= 135 + 22.5 && r >= 135 - 22.5)
            {
                //↑
                return RoleDirection.NE;
            }
            else if (r <= 90 + 22.5 && r >= 90 - 22.5)
            {
                //↗
                return RoleDirection.E;
            }
            else if (r <= 45 + 22.5 && r >= 45 - 22.5)
            {
                //→
                return RoleDirection.ES;
            }
            else if (r <= 0 + 22.5 && r >= 0 - 22.5)
            {
                //↘
                return RoleDirection.S;
            }
            else if (r <= -45 + 22.5 && r >= -45 - 22.5)
            {
                //↓
                return RoleDirection.SW;
            }
            else if (r <= -90 + 22.5 && r >= -90 - 22.5)
            {
                //↙
                return RoleDirection.W;
            }
            else //if (r <= -135 + 22.5 && r >= -135 - 22.5)
            {
                //←
                return RoleDirection.WN;
            }
        }

        public static RoleDirection GetRoleDirection(IntPtr hWnd)
        {
            int hProcess = WinAPI.GetProcess(hWnd);
            WinAPI.ReadProcessMemory(hProcess, CGAddr.RoleDirectionAddr, out int currentDirection, 4, 0);

            return (RoleDirection)currentDirection;
        }

        public static bool RightButtonClickNPC(IntPtr hWnd, Location NPClocation, int shareDelayTime = 0)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            if (shareDelayTime != 0)
            {
                if (Timer.ElapsedMilliseconds == 0)
                {
                    Timer.Start();
                }
                else
                {
                    if (Timer.ElapsedMilliseconds > shareDelayTime)
                    {
                        Timer.Reset();
                    }
                    return false;
                }
            }

            RoleDirection direction = GetTargetDirection(hWnd, NPClocation);
            Mouse point = DirectionToPoint(direction);
            MoveMouse(hProcess, point, true);

            if (Monitor.TryEnter(LockObject))
            {
                ClickMouseRightButton(hProcess, WinAPI.IsIconic(hWnd));
                Monitor.Exit(LockObject);

                return direction == GetRoleDirection(hWnd);
            }

            return false;
        }

        public static void TriggerLure(IntPtr hWnd)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            RoleDirection direction = (RoleDirection)new Random().Next(4, 7);
            int range = direction == RoleDirection.SW ? new Random().Next(4, 5) : new Random().Next(5, 7);
            Mouse point = DirectionToPoint(direction, range);
            MoveMouse(hProcess, point, true);
            ClickMouseRightButton(hProcess, WinAPI.IsIconic(hWnd));
        }

        public static Mouse DirectionToPoint(RoleDirection direction, int clickDistance = 2)
        {
            int x = 640 / 2;
            int y = 480 / 2;
            switch (direction)
            {
                case RoleDirection.N://↖
                    x = 640 / 2 - 32 * clickDistance;
                    y = 480 / 2 - 24 * clickDistance;
                    break;
                case RoleDirection.NE://↑
                    x = 640 / 2 - 32 * 0;
                    y = 480 / 2 - 24 * (clickDistance * 2);
                    break;
                case RoleDirection.E://↗
                    x = 640 / 2 + 32 * clickDistance;
                    y = 480 / 2 - 24 * clickDistance;
                    break;
                case RoleDirection.ES://→
                    x = 640 / 2 + 32 * (clickDistance * 2);
                    y = 480 / 2 + 24 * 0;
                    break;
                case RoleDirection.S://↘
                    x = 640 / 2 + 32 * clickDistance;
                    y = 480 / 2 + 24 * clickDistance;
                    break;
                case RoleDirection.SW://↓ 
                    x = 640 / 2 - 32 * 0;
                    y = 480 / 2 + 24 * (clickDistance * 2);
                    break;
                case RoleDirection.W://↙
                    x = 640 / 2 - 32 * clickDistance;
                    y = 480 / 2 + 24 * clickDistance;
                    break;
                case RoleDirection.WN://←
                    x = 640 / 2 - 32 * (clickDistance * 2);
                    y = 480 / 2 - 24 * 0;
                    break;

                default:
                    x = 0;
                    y = 0;
                    break;
            }

            return new Mouse(x, y);
        }

        public static void ClickMouseLeftButton(int hProcess, bool isIconic = false)
        {
            WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x1), 4, 0);

            if (isIconic || GetFrameSkipState(hProcess))
            {
                Delay(5);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x0), 4, 0);
            }
        }

        public static void ClickMouseRightButton(int hProcess, bool isIconic = false)
        {
            WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x2), 4, 0);

            if (isIconic || GetFrameSkipState(hProcess))
            {
                Delay(5);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x0), 4, 0);
            }
        }

        public static void DoubleClickMouseLeftButton(int hProcess, bool isIconic = false)
        {
            WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x10), 4, 0);

            if (isIconic || GetFrameSkipState(hProcess))
            {
                Delay(5);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.ClickAddr, BitConverter.GetBytes(0x0), 4, 0);
            }
        }

        public static Mouse GetMouse(int hProcess)
        {
            Mouse mouse = new Mouse();
            int x, y, count = 0;
            do
            {
                x = GetXORValue(hProcess, CGAddr.MouseXAddr);
                y = GetXORValue(hProcess, CGAddr.MouseYAddr);

                if (x <= 800 && y <= 600)
                {
                    mouse = new Mouse(x, y);
                    break;
                }
                count++;
            } while (count <= 5);

            return mouse;
        }

        public static void MoveMouse(int hProcess, Mouse mouse, bool checkPosition = false)
        {
            int count = 0;
            if (checkPosition)
            {
                SkipFrame(hProcess, false);
            }

            do
            {
                MoveMouse(hProcess, mouse.X, mouse.Y);
                Delay(50);
                if (!GetMouse(hProcess).Changed(mouse))
                {
                    break;
                }
                count++;
            } while (checkPosition && count <= 5);
        }

        public static void MoveMouse(int hProcess, int x, int y)
        {
            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseXAddr + 0x4, BitConverter.GetBytes(x), 4, 0);
            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseXAddr + 0x8, BitConverter.GetBytes(0x0), 4, 0);
            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseXAddr + 0xC, BitConverter.GetBytes(0x15), 4, 0);

            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseYAddr + 0x4, BitConverter.GetBytes(y), 4, 0);
            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseYAddr + 0x8, BitConverter.GetBytes(0x0), 4, 0);
            WinAPI.WriteProcessMemory(hProcess, CGAddr.MouseYAddr + 0xC, BitConverter.GetBytes(0x15), 4, 0);
            //WinAPI.PostMessage(HandleWindow, WinAPI.WM_MOUSEMOVE, 0, WinAPI.MAKELPARAM(x, y));
        }

        public static bool ExpWindowShow(IntPtr hWnd, bool click = true)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            WindowObject expWindow = WindowObject.SearchTopWindow(hProcess, 0x4D8CD0);
            if (expWindow != null)
            {
                //Console.WriteLine("xxx 出現經驗值視窗");

                //WinAPI.ReadProcessMemory(hProcess, expWindow.ParentAddr + 0xC, out int x, 4, 0);
                //WinAPI.ReadProcessMemory(hProcess, expWindow.ParentAddr + 0x10, out int y, 4, 0);
                //WinAPI.ReadProcessMemory(hProcess, expWindow.ParentAddr + 0x14, out int maxX, 4, 0);
                //WinAPI.ReadProcessMemory(hProcess, expWindow.ParentAddr + 0x18, out int maxY, 4, 0);

                if (click)
                {
                    //int randomX = (x + maxX) / 5;
                    //int randomY = (y + maxY) / 5;

                    //MoveMouse(hProcess, new Mouse(randomX, randomY), true);
                    MoveMouse(hProcess, new Mouse(640 / 2, 480 / 2), true);

                    ClickMouseLeftButton(hProcess, WinAPI.IsIconic(hWnd));
                }


                return true;
            }

            return false;
        }

        public static bool GetFrameSkipState(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.FrameSkipAddr, out int frameSkip, 4, 0);
            if (frameSkip == 0)
            {
                return true;
            }

            return false;
        }

        public static string GetExeFilePath(int hProcess)
        {
            string path = GetStringFromAddr(hProcess, CGAddr.GamePathAddr, false);

            if (!string.IsNullOrEmpty(path))
            {
                return path.Substring(0, path.LastIndexOf("\\") + 1);
            }

            return null;
        }

        public static void PressKey(IntPtr hWnd, System.Windows.Forms.Keys key)
        {
            WinAPI.PostMessage(hWnd, WinAPI.WM_KEYDOWN, (int)key, WinAPI.MakeLParam((uint)key, "VM_KEYDOWN"));
            WinAPI.PostMessage(hWnd, WinAPI.WM_KEYUP, (int)key, WinAPI.MakeLParam((uint)key, "VM_KEYUP"));
            Delay(100);
            WinAPI.PostMessage(hWnd, WinAPI.WM_KEYDOWN, (int)System.Windows.Forms.Keys.Enter, WinAPI.MakeLParam((uint)System.Windows.Forms.Keys.Enter, "VM_KEYDOWN"));
            WinAPI.PostMessage(hWnd, WinAPI.WM_KEYUP, (int)System.Windows.Forms.Keys.Enter, WinAPI.MakeLParam((uint)System.Windows.Forms.Keys.Enter, "VM_KEYUP"));
            Delay(300);
        }

        public static void SkipFrame(int hProcess, bool skip)
        {
            if (skip)
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.FrameSkipAddr, BitConverter.GetBytes(0x0), 4, 0);
            }
            else
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.FrameSkipAddr, BitConverter.GetBytes(0x2), 4, 0);
            }
        }

        public static string GetNPCMessage(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.NPCMessageWindowExistAddr, out int messageWindow, 4, 0);
            if (messageWindow != 0)
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.NPCMessageAddr, out int messagePtrAddr, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, messagePtrAddr + 0x14, out int messageAddr, 4, 0);
                return GetStringFromAddr(hProcess, messageAddr);
            }

            return null;
        }

        public static void ClickConfirm(int hProcess)
        {
            //0x3BE4B   是
            //0x3BE51   下一步
            //0x3B568   確定
            ArrayList buttonList = Button.SearchButton(hProcess, new int[] { 0x3BE4B, 0x3BE51, 0x3B568 });

            if (buttonList.Count != 0)
            {
                foreach (Button button in buttonList)
                {
                    int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    }
                }
            }
        }

        public static void ClickNegative(int hProcess)
        {
            //0x3BE4E   否
            ArrayList buttonList = Button.SearchButton(hProcess, new int[] { 0x3BE4E });

            if (buttonList.Count != 0)
            {
                foreach (Button button in buttonList)
                {
                    int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    }
                }
            }
        }

        public static string GetHint(int hProcess)
        {
            return GetStringFromAddr(hProcess, 0xF1C2F0);
        }

        public static MapState GetMapState(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.MapStateAddr, out int state, 4, 0);

            return (MapState)state;
        }

        public static int GetBattleRound(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.BattleRoundAddr, out int round, 4, 0);

            return round;
        }
    }
}
