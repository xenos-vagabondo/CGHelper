using CommonLibrary;
using System;
using System.Collections;

namespace CGHelper.CG
{
    //0x3B603   全部

    //0x3BE48   取消
    //0x3BE4B   是
    //0x3BE4E   否
    //0x3BE51   下一步
    //0x3BE54   回上步
    //0x3B538   - 關閉視窗icon
    //0x3B562   取消
    //0x3B565   停止
    //0x3B568   確定
    //0x3B56B   重試

    //0x3B962   技能
    //0x3B965   物品

    public class Button : WindowObject
    {
        public int Icon { get; set; }

        public Button(WindowObject window, int icon)
        {
            Addr = window.Addr;
            CallAddrPtr = window.CallAddrPtr;
            CallAddr = window.CallAddr;
            Icon = icon;
        }

        public override string ToString()
        {
            return "Button Addr = 0x" + Addr.ToString("X") + " CallAddr = 0x" + CallAddr.ToString("X") + " Icon = 0x" + Icon.ToString("X");
        }

        public static ArrayList GetButtonList(int hProcess)
        {
            ArrayList buttonList = new ArrayList();

            foreach (WindowObject window in GetWindows(hProcess))
            {
                WinAPI.ReadProcessMemory(hProcess, window.Addr + 0x18, out int buttonExist, 4, 0);
                if (buttonExist == 0)
                    continue;

                WinAPI.ReadProcessMemory(hProcess, window.Addr + 0x1C, out int buttonIconPtr, 4, 0);
                if (buttonIconPtr == 0)
                    continue;

                WinAPI.ReadProcessMemory(hProcess, buttonIconPtr, out int buttonIcon, 4, 0);

                if (buttonIcon == 0)
                    continue;

                Button button = new Button(window, buttonIcon);
                buttonList.Add(button);
            }

            return buttonList;
        }

        public static Button SearchButton(int hProcess, int buttonIcon)
        {
            ArrayList buttonList = GetButtonList(hProcess);
            foreach (Button button in buttonList)
            {
                if (button.Icon == buttonIcon)
                {
                    return button;
                }
            }
            return null;
        }

        public static ArrayList SearchButton(int hProcess, int[] buttonIcon)
        {
            ArrayList buttonList = new ArrayList();
            foreach (Button button in GetButtonList(hProcess))
            {
                foreach (int icon in buttonIcon)
                {
                    if (button.Icon == icon)
                    {
                        buttonList.Add(button);
                    }
                }
            }

            return buttonList;
        }

        public static void OpenSkillWindow(int hProcess)
        {
            //0x3B962   技能
            Button button = SearchButton(hProcess, 0x3B962);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(50);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                }
            }
        }

        public static void Inventory(int hProcess, bool open = true)
        {
            string hint = Common.GetHint(hProcess);
            if (!string.IsNullOrEmpty(hint))
            {
                int randomX = 640 + new Random().Next(-10, 10);
                int randomY = 480 + new Random().Next(-10, 10);

                Common.MoveMouse(hProcess, new Mouse(randomX, randomY));
            }

            if (open)
            {
                //0x3B964   物品欄已打開
                //0x3B965   物品
                Button button = SearchButton(hProcess, 0x3B964);
                if (button == null)
                {
                    button = SearchButton(hProcess, 0x3B965);
                    if (button != null)
                    {
                        int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                        if (fakeClickAddr != 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                            Common.Delay(10);
                            WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                        }
                    }
                    else
                    {
                        int randomX = 640 + new Random().Next(-10, 10);
                        int randomY = 480 + new Random().Next(-10, 10);

                        Common.MoveMouse(hProcess, new Mouse(randomX, randomY));
                    }
                }
            } 
            else
            {
                WindowObject window = SearchTopWindow(hProcess, CGAddr.InventoryCallAddr);
                if (window != null)
                {
                    CloseWindow(hProcess);
                }
            }
        }

        public static void CloseWindow(int hProcess)
        {
            //0x3B538   - 關閉視窗icon
            Button button = SearchButton(hProcess, 0x3B538);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(50);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                }
            }
        }

        public static void OpenPetWindow(int hProcess)
        {
            //0x3B968   寵物
            Button button = SearchButton(hProcess, 0x3B968);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(50);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                }
            }
        }
    }
}
