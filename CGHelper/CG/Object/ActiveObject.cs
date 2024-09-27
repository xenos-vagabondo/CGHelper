using CGHelper.CG.Base;
using CommonLibrary;
using System.Collections;

namespace CGHelper.CG
{
    public class ActiveObject : Coordinate
    {
        public int Addr { get; set; }
        public int Type { get; set; }
        public string Name { get; set; }
        public int TeamState { get; set; }
        public bool TeamMember { get; set; }
        public int State { get; set; }

        public bool NPC { get; set; }

        public bool Injured { get; set; }

        public static ArrayList GetObject(int hProcess)
        {
            ArrayList list = new ArrayList();

            for (int i = 0; i < 1000; i++)
            {
                int addr = CGAddr.ObjectListAddr + i * 0x13C;

                ActiveObject ob = GetObjectInfo(hProcess, addr);
                if (ob == null)
                {
                    break;
                }

                list.Add(ob);
            }

            return list;
        }

        public static ActiveObject GetObjectInfo(int hProcess, int addr)
        {
            WinAPI.ReadProcessMemory(hProcess, addr, out int type, 4, 0);
            if (type == 0)
            {
                return null;
            }

            ActiveObject ob = new ActiveObject();

            ob.Addr = addr;
            ob.Type = type;
            // type & 0x80000 > 0 玩家
            // type & 0x20000 > 0 寶箱?
            // type & 0x10000 > 0 NPC
            ob.NPC = (type & 0x10000) > 0;

            ob.X = Common.GetXORValue(hProcess, addr + 0xC);
            ob.Y = Common.GetXORValue(hProcess, addr + 0x1C);

            WinAPI.ReadProcessMemory(hProcess, addr + 0x43, out int teamState, 1, 0);
            ob.TeamState = teamState;
            //teamState & 0x1 > 0 某隊伍的隊長
            //teamState & 0x2 > 0 隊伍成員
            ob.TeamMember = (teamState & 0x2) > 0;

            WinAPI.ReadProcessMemory(hProcess, addr + 0x11C, out int namePtr, 4, 0);
            ob.Name = Common.GetNameFromAddr(hProcess, namePtr + 0xC4);

            WinAPI.ReadProcessMemory(hProcess, addr + 0x120, out int state, 4, 0);
            ob.State = state;
            //state & 0x1 > 0 受傷
            ob.Injured = (state & 0x1) > 0;

            return ob;
        }

        public static ActiveObject FindNPC(int hProcess, string name)
        {
            for (int i = 0; i < 1000; i++)
            {
                int addr = CGAddr.ObjectListAddr + i * 0x13C;

                WinAPI.ReadProcessMemory(hProcess, addr, out int type, 4, 0);
                if (type == 0)
                {
                    break;
                }

                if ((type & 0x10000) == 0)
                {
                    continue;
                }

                WinAPI.ReadProcessMemory(hProcess, addr + 0x11C, out int namePtr, 4, 0);
                string npcName = Common.GetNameFromAddr(hProcess, namePtr + 0xC4);

                if (!string.IsNullOrEmpty(name) && name.Equals(npcName))
                {
                    return GetObjectInfo(hProcess, addr);
                }
            }

            return null;
        }

        public string ToString()
        {
            return Name + " (" + X + "," + Y + ") TeamState = " + TeamState + " State " + State;
        }
    }
}
