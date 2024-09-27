using CommonLibrary;
using System;

namespace CGHelper.CG
{
    class CGCall
    {
        public static int RestoreSelect(GameWindow window, int index)
        {
            IntPtr hWnd = window.HandleWindow;
            int hProcess = window.HandleProcess;

            AsmClassLibrary asm = new AsmClassLibrary();

            int addr = 0;

            if (index >= 0)
            {
                index *= 0x2;

                asm.Mov_EAX(0x2);
                asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x3C);
                if (window.ClassName.Equals("魔力寶貝"))
                {
                    asm.Mov_EBP(0x3 + index);
                    asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x28);
                }
                else if (window.ClassName.Equals("Blue"))
                {
                    asm.Mov_EBP(0x3 + index);
                }
                asm.Ret();
                addr = asm.WriteAsm(hProcess);

                Console.WriteLine("Restore addr = 0x" + addr.ToString("X"));
            }

            return addr;
        }

        public static int ClickButton(int hProcess, int callAddr)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Mov_EAX(0x2);
            asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x8);
            asm.Mov_EAX(callAddr);
            asm.JMP_EAX();
            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static int RestoreConfirm(int hProcess, int callAddr)
        {
            int addr = ClickButton(hProcess, callAddr);
            Console.WriteLine("RestoreConfirm addr = 0x" + addr.ToString("X"));

            return addr;
        }

        public static int ProduceRetry(int hProcess, int callAddr)
        {
            int addr = ClickButton(hProcess, callAddr);
            Console.WriteLine("ProduceRetry addr = 0x" + addr.ToString("X"));

            return addr;
        }

        public static int ProduceExecute(int hProcess, int callAddr)
        {
            int addr = ClickButton(hProcess, callAddr);
            Console.WriteLine("ProduceExecute addr = 0x" + addr.ToString("X"));

            return addr;
        }

        public static int SkillToHumanSelect(int hProcess, int callAddr)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Mov_EAX(0x2);
            asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x8);
            asm.Mov_EAX(callAddr);
            asm.JMP_EAX();
            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static int SelectSkill(int hProcess, int callAddr)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Mov_EAX(0x53);
            asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x8);
            asm.Mov_EAX(callAddr);
            asm.JMP_EAX();
            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static int UseItem(int hProcess, int code, int x, int y, int itemIndex)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Push(0);
            asm.Push(0x13);
            asm.Push(y);
            asm.Push(x);
            asm.Push(code);
            asm.Mov_EAX(0x5068B0);
            asm.JMP_EAX();
            asm.Ret();

            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static int SelectItem(int hProcess, int index)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Mov_EAX(index);
            asm.Mov_DWORD_Ptr_ESP_ADD_EAX(0x28);
            asm.Ret();

            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static int SelectTarget(int hProcess, int index)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Mov_EAX(index);

            asm.Ret();
            int addr = asm.WriteAsm(hProcess);

            return addr;
        }

        public static void TestRestoreSelectCall(GameWindow window, int index)
        {
            IntPtr hWnd = window.HandleWindow;
            AsmClassLibrary asm = new AsmClassLibrary();

            int pid = 0;
            WinAPI.GetWindowThreadProcessId(hWnd, ref pid);

            asm.Pushad();
            asm.Push(0x100);
            asm.Push(0x0);
            asm.Push(index);
            asm.Push(0x0);
            asm.Mov_EAX(0x495EA0);
            asm.Call_EAX();
            asm.Popad();
            asm.Ret();
            asm.RunAsm(pid);
        }

        public static int TestUseItem(int hProcess, int callAddr, int itemIndex, int subESP)
        {
            AsmClassLibrary asm = new AsmClassLibrary();

            asm.Push_EBP();
            asm.Mov_EBP_ESP();
            asm.SUB_ESP(subESP);
            asm.Mov_DWORD_Ptr_EBP_ADD(-4, itemIndex);
            asm.Mov_DWORD_Ptr_EBP_ADD(-4, itemIndex);
            asm.Mov_DWORD_Ptr_EBP_ADD(-4, itemIndex);
            asm.Mov_DWORD_Ptr_EBP_ADD(-4, itemIndex);
            asm.Add_ESP(subESP);
            asm.Pop_EBP();
            //asm.Mov_EAX(0x4C1CB1);
            asm.Mov_EAX(callAddr);
            asm.JMP_EAX();
            int addr = asm.WriteAsm(hProcess);

            return addr;
        }
    }
}
