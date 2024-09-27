using CGHelper.CG.Base;
using CommonLibrary;

namespace CGHelper.CG.Battle
{
    public class BattleTarget : Attribute
    {
        public int Addr { get; set; }
        public int Index { get; set; }
        public int Code { get; set; }
        public int SelectState { get; set; }
        public bool Shade { get; set; }
        public int CodeAddr { get; set; }
        public bool ModifySelectState { get; set; }

        public BattleTarget() { }

        public BattleTarget(int addr)
        {
            Addr = addr;
        }

        override public string ToString()
        {
            return Name + "(0x" + Index.ToString("X") + " Lv" + LV + ")";
            //return "Lv" + Level + " " + Name + "(0x" + Index.ToString("X") + ") " + HP + "/" + MaxHP + " " + MP + "/" + MaxMP;
        }

        public static BattleTarget GetTargetInfo(int hProcess, int targetInfoAddr)
        {
            WinAPI.ReadProcessMemory(hProcess, targetInfoAddr, out int targetAddr, 4, 0);
            if (targetAddr == 0)
            {
                return null;
            }

            BattleTarget target = new BattleTarget(targetAddr);
            target.Name = Common.GetNameFromAddr(hProcess, target.Addr + 0xC4);
            target.HP = Common.GetXORValue(hProcess, target.Addr + 0x170);
            target.MaxHP = Common.GetXORValue(hProcess, target.Addr + 0x180);
            target.MP = Common.GetXORValue(hProcess, target.Addr + 0x1B0);
            target.MaxMP = Common.GetXORValue(hProcess, target.Addr + 0x1C0);

            WinAPI.ReadProcessMemory(hProcess, target.Addr + 0x1D0, out int level, 4, 0);
            target.LV = level;
            WinAPI.ReadProcessMemory(hProcess, target.Addr + 0x24, out int selectState, 4, 0);
            target.SelectState = selectState;

            target.CodeAddr = target.Addr + 0x18;
            WinAPI.ReadProcessMemory(hProcess, target.CodeAddr, out int code, 4, 0);
            target.Code = code;

            //Console.WriteLine("0x" + target.Addr.ToString("X") + " Lv" + target.Level + " " + target.Name + " " + target.HP + "/" + target.MaxHP + " " + target.MP + "/" + target.MaxMP);

            WinAPI.ReadProcessMemory(hProcess, target.Addr + 0x30, out int x, 4, 0);
            WinAPI.ReadProcessMemory(hProcess, target.Addr + 0x34, out int y, 4, 0);

            //Console.WriteLine("0x" + target.Addr.ToString("X") + " Lv" + target.Level + " " + target.Name + " " + target.HP + "/" + target.MaxHP + " " + target.MP + "/" + target.MaxMP + " " + x + "," + y + " " + " 0x" + target.Code.ToString("X"));

            return target;
        }
    }
}
