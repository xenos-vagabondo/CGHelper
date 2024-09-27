using CommonLibrary;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CGHelper.CG
{
    class TeamInfo
    {
        public List<string> Member { get; set; } = new List<string>();

        public bool TeamLeader { get; set; }

        public static TeamInfo GetTeamInfo(int hProcess)
        {
            TeamInfo teamInfo = new TeamInfo();
            WinAPI.ReadProcessMemory(hProcess, CGAddr.TeamInfoAddr, out int team, 4, 0);
            if (team != 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    int addr = CGAddr.TeamInfoAddr + i * 0x30;
                    WinAPI.ReadProcessMemory(hProcess, addr + 0x2C, out int teamMemberInfoPtr, 4, 0);
                    if (teamMemberInfoPtr != 0)
                    {
                        string name = Common.GetNameFromAddr(hProcess, teamMemberInfoPtr + 0xC4);
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (i == 0 && name.Equals(Common.GetRoleName(hProcess)))
                            {
                                teamInfo.TeamLeader = true;
                            }
                            teamInfo.Member.Add(name);
                        }
                    }
                }
            }

            return teamInfo;
        }

        public static ArrayList GetInjuredTeamMember(int hProcess)
        {
            ArrayList injuredList = new ArrayList();

            for (int i = 0; i < 1000; i++)
            {
                int addr = CGAddr.ObjectListAddr + i * 0x13C;
                WinAPI.ReadProcessMemory(hProcess, addr, out int temp, 4, 0);
                if (temp == 0)
                {
                    break;
                }
                WinAPI.ReadProcessMemory(hProcess, addr + 0x43, out int isTeamMember, 1, 0);
                WinAPI.ReadProcessMemory(hProcess, addr + 0x120, out int state, 4, 0);
                bool injured = (state & 0x1) == 1;

                if ((isTeamMember & 0x2) > 0 && injured)
                {
                    WinAPI.ReadProcessMemory(hProcess, addr + 0x11C, out int namePtr, 4, 0);
                    string name = Common.GetNameFromAddr(hProcess, namePtr + 0xC4);
                    injuredList.Add(name);
                }
            }

            WinAPI.ReadProcessMemory(hProcess, CGAddr.HealthAddr, out int selfHeath, 4, 0);
            if (selfHeath > 0)
            {
                injuredList.Add(Common.GetRoleName(hProcess));
            }

            return injuredList;
        }

        public static void DisbandTeam(int hProcess)
        {
            TeamInfo teamInfo = GetTeamInfo(hProcess);
            if (teamInfo.TeamLeader && teamInfo.Member.Count > 0)
            {
                //0x3B914 組隊
                Button button = Button.SearchButton(hProcess, 0x3B914);
                if (button != null)
                {
                    int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                        Common.Delay(20);
                        WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                    }
                }
            }
        }

        public static bool TeamMemberSameLocation(ArrayList activeObjectList, Location location)
        {
            foreach (ActiveObject ao in activeObjectList)
            {
                if (ao.TeamMember && (ao.X != location.X || ao.Y != location.Y))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TeamMemberSameLocation(int hProcess, string leaderName)
        {
            Location location = Location.GetLocation(hProcess);

            ArrayList teamMember = new ArrayList();
            foreach (GameWindow window in UsingGameWindow.GameWindows)
            {
                if (string.IsNullOrEmpty(window.RoleName) || window.RoleName.Equals(leaderName))
                {
                    continue;
                }

                teamMember.Add(window.RoleName);
            }

            if (teamMember.Count == 0)
            {
                return false;
            }

            ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
            foreach (ActiveObject ao in activeObjectList)
            {
                if (teamMember.IndexOf(ao.Name) != -1 && ao.X == location.X && ao.Y == location.Y)
                {
                    teamMember.Remove(ao.Name);
                }
            }

            return teamMember.Count == 0;
        }
    }
}
