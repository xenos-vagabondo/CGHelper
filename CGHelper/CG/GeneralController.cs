using CGHelper.CG.Enum;
using CGHelper.CG.Pet;
using CommonLibrary;
using System;
using System.Collections;
using System.Diagnostics;

namespace CGHelper.CG
{
    public class GeneralController
    {
        private GameWindow Window { get; set; }

        private bool Restoring { get; set; }
        private bool StopRestore { get; set; }
        private int StopRestoreCount { get; set; }

        private bool StopAutoItemBattle { get; set; }

        private Stopwatch IdleTimer { get; set; } = new Stopwatch();

        private string[] Cuisines { get; set; } = new string[] { "漢堡", "壽喜鍋", "螃蟹鍋" };

        public GeneralController(GameWindow window)
        {
            Window = window;
        }

        public void Watcher()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            MapState state = Common.GetMapState(hProcess);

            FrameSkip(hProcess);

            if (state == MapState.STATE_MAP)
            {
                UseCuisines(hWnd, hProcess);
                CheckAntiLure(hWnd, hProcess);
                CheckBattlePet(hWnd, hProcess);
                NpcRestoreWindow(hProcess);

                Mission.Event(Window);
            }
            CheckIdleTime(hWnd, hProcess);
        }

        public bool RestoreSelect(int select)
        {
            int hProcess = Window.HandleProcess;

            int fakeCallAddr = CGCall.RestoreSelect(Window, select);
            if (fakeCallAddr != 0)
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.RestoreSelectAddr + 0x1, BitConverter.GetBytes(fakeCallAddr - (CGAddr.RestoreSelectAddr + 0x5)), 4, 0);
                Common.Delay(100);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.RestoreSelectAddr + 0x1, BitConverter.GetBytes(CGAddr.RestoreSelectCallAddr - (CGAddr.RestoreSelectAddr + 0x5)), 4, 0);
                return true;
            }
            return false;
        }

        public void NpcRestoreWindow(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.NPCMessageWindowExistAddr, out int npcMsgWindow, 4, 0);
            if (npcMsgWindow != 0)
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.NPCMessageAddr, out int MessagePtrAddr, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, MessagePtrAddr + 0x14, out int MessageAddr, 4, 0);
                string msg = Common.GetStringFromAddr(hProcess, MessageAddr);
                //Console.WriteLine(msg);
                StopRestoreCount = 0;

                if (string.IsNullOrEmpty(msg))
                {
                    return;
                }

                if (msg.Contains("要回復嗎？") && msg.Contains("回復魔法(+回復生命力)生命力"))
                {
                    if (!StopRestore)
                    {
                        int HP = Common.GetXORValue(hProcess, CGAddr.HPAddr);
                        int MaxHP = Common.GetXORValue(hProcess, CGAddr.MAXHPAddr);
                        int MP = Common.GetXORValue(hProcess, CGAddr.MPAddr);
                        int MaxMP = Common.GetXORValue(hProcess, CGAddr.MAXMPAddr);

                        if (MP < MaxMP && HP <= MaxHP)
                        {
                            if (Window.ClassName.Equals("御守魔力"))
                            {

                            }
                            else
                            {
                                Restoring = RestoreSelect(0);
                            }
                                
                        }
                        else if (CheckRestorePet(hProcess))
                        {
                            if (Window.ClassName.Equals("御守魔力"))
                            {

                            }
                            else
                            {
                                Restoring = RestoreSelect(2);
                            }
                        } 
                        else
                        {
                            Restoring = false;
                        }
                    } 
                }
                else if (Restoring)
                {
                    if (msg.Contains("現在魔法力") || msg.Contains("回復寵物"))
                    {
                        Common.ClickConfirm(hProcess);
                        Log.WriteLine(msg);
                    }
                    else if (msg.Contains("已經回復了") || msg.Contains("你沒有回復的必要"))
                    {
                        Common.ClickConfirm(hProcess);
                        Log.WriteLine(msg);
                    }
                    else if (msg.Contains("非戰鬥系的人請去隔壁吧！") || msg.Contains("你的錢不夠"))
                    {
                        Common.ClickConfirm(hProcess);
                        StopRestore = true;
                        Log.WriteLine(msg);
                    }
                }
            } 
            else if (StopRestore && Restoring)
            {
                ++StopRestoreCount;
                Log.WriteLine("StopRestoreCount = " + StopRestoreCount);
                if (StopRestoreCount > 5)
                {
                    Restoring = false;
                    StopRestore = false;
                }
            }
        }

        public bool CheckRestorePet(int hProcess)
        {
            foreach (PetInfo pet in PetInfo.GetAllPetsInfo(hProcess))
            {
                if (pet.HP < pet.MaxHP || pet.MP < pet.MaxMP)
                {
                    return true;
                }
            }

            return false;
        }

        private void FrameSkip(int hProcess)
        {
            //跳幀
            bool skip = Common.GetFrameSkipState(hProcess);

            if (Window.PowerSavingMode)
            {
                if (WinAPI.GetForegroundWindow() == Window.HandleWindow)
                {
                    if (skip)
                    {
                        Common.SkipFrame(hProcess, false);
                    }
                }
                else if (!skip)
                {
                    Common.SkipFrame(hProcess, true);
                }
            }
        }

        private void CheckIdleTime(IntPtr hWnd, int hProcess)
        {
            if (!Window.ItemLure)
            {
                return;
            }

            if (Window.AutoChangePet)
            {
                ArrayList petList = PetInfo.GetAllPetsInfo(hProcess);
                foreach (PetInfo pet in petList)
                {
                    if (pet.BattleState == 0x2)
                    {
                        if (pet.MP < 37)
                        {
                            return;
                        }
                    }
                }
            }

            ArrayList injuredList = TeamInfo.GetInjuredTeamMember(hProcess);
            if (injuredList.Count > 0)
            {
                return;
            }

            Location location = Location.GetLocation(hProcess);
            if (string.IsNullOrEmpty(location.Name) || 
                Window.LocationItemLure == null ||
                string.IsNullOrEmpty(Window.LocationItemLure.Name))
            {
                IdleTimer.Reset();
                return;
            }

            if (Common.GetMapState(hProcess) != MapState.STATE_MAP)
            {
                if (!Window.LocationItemLure.Name.Equals(location.Name))
                {
                    if (Window.UsingItemLure != null)
                    {
                        StopAutoItemBattle = true;
                    }
                }

                IdleTimer.Reset();
                return;
            }

            if (IdleTimer.Elapsed.TotalMilliseconds == 0)
            {
                IdleTimer.Start();
                return;
            }

            if (StopAutoItemBattle)
            {
                if (!Common.ExpWindowShow(hWnd))
                {
                    if (UseItemLure(hWnd, hProcess))
                    {
                        StopAutoItemBattle = false;
                        Console.WriteLine(Window.RoleName + " 停止使用 " + Window.UsingItemLure.Name);
                        Window.UsingItemLure = null;
                    }
                }
            }

            if (!Window.LocationItemLure.Name.Equals(location.Name))
            {
                IdleTimer.Reset();
                return;
            }

            if (IdleTimer.Elapsed.TotalMilliseconds >= 10000)
            {
                if (UseItemLure(hWnd, hProcess))
                {
                    Console.WriteLine(Window.RoleName + " 使用 " + Window.UsingItemLure.Name);
                    IdleTimer.Restart();
                }
            }
            else
            {
                if (IdleTimer.Elapsed.TotalMilliseconds > 2000 && IdleTimer.Elapsed.TotalMilliseconds <= 4000)
                {
                    if (Inventory.UpdateItemInfo(hProcess, Window.UsingItemLure) && Window.UsingItemLure.Durability > 0)
                    {
                        Button.Inventory(hProcess, false);
                        Common.TriggerLure(hWnd);
                        Console.WriteLine(Window.RoleName + " " + Window.UsingItemLure.Name + " 耐久:" + Window.UsingItemLure.Durability);
                        return;
                    }
                    else
                    {
                        Window.UsingItemLure = null;
                    }
                }
            }
        }

        public void UseCuisines(IntPtr hWnd, int hProcess)
        {
            if (!Window.AutoUseCuisines)
            {
                return;
            }

            Inventory inventory = Inventory.GetInventoryInfo(hProcess, true);
            Item item = null;
            foreach (string cuisine in Cuisines)
            {
                item = inventory.Search(cuisine);
                if (item != null)
                {
                    break;
                }
            }

            if (item == null)
            {
                return;
            }

            int MP = Common.GetXORValue(hProcess, CGAddr.MPAddr);
            int MaxMP = Common.GetXORValue(hProcess, CGAddr.MAXMPAddr);

            if ((float)MP / MaxMP <= 0.25 && MaxMP - MP > item.Level * 110)
            {
                if (!ItemSelectTarget(Window.RoleName))
                {
                    if (!ItemSelectPlayer(Window.RoleName))
                    {
                        if (!Common.ExpWindowShow(hWnd))
                        {
                            if (Inventory.UseItem(hWnd, item))
                            {
                                Log.WriteLine(Window.RoleName + " 使用 " + item.Name);
                            }
                        }
                    }
                }
            }
        }

        private bool ItemSelectTarget(string targetName)
        {
            int hProcess = Window.HandleProcess;

            //道具/治療/急救-選擇人物/寵物
            ArrayList skillSelectTargetList = WindowObject.SearchWindow(hProcess, CGAddr.SkillSelectTargetCallAddr);
            if (skillSelectTargetList.Count > 0)
            {
                ArrayList list = GetItemSelectTargetList();
                int index = list.IndexOf(targetName);
                if (index != -1)
                {
                    WindowObject windowObject = (WindowObject)skillSelectTargetList[index];
                    int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                        //Common.Delay(20);
                        //WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                    }
                }
                return true;
            }

            return false;
        }

        private bool ItemSelectPlayer(string targetName)
        {
            int hProcess = Window.HandleProcess;

            //道具/治療/急救-選擇人物
            ArrayList skillSelectHumanList = WindowObject.SearchWindow(hProcess, CGAddr.SkillSelectHumanCallAddr);
            if (skillSelectHumanList.Count > 0)
            {
                ArrayList list = GetItemSelectPlayerListList();
                int index = list.IndexOf(targetName);
                if (index != -1)
                {
                    WindowObject windowObject = (WindowObject)skillSelectHumanList[index];
                    int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                        //Common.Delay(20);
                        //WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                    }
                }
                return true;
            }

            return false;
        }

        public ArrayList GetItemSelectPlayerListList()
        {
            int hProcess = Window.HandleProcess;

            ArrayList list = new ArrayList();
            for (int i = 0; i < 5; i++)
            {
                int nameAddr = CGAddr.ItemSelectPlayerListAddr + i * 0x11;
                string name = Common.GetNameFromAddr(hProcess, nameAddr);
                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                //Console.WriteLine(i + " " + name);
                list.Add(name);
            }

            return list;
        }

        public ArrayList GetItemSelectTargetList()
        {
            int hProcess = Window.HandleProcess;

            ArrayList list = new ArrayList();
            for (int i = 0; i < 5; i++)
            {
                int nameAddr = CGAddr.ItemSelectTargetListAddr + i * 0x34;
                string name = Common.GetNameFromAddr(hProcess, nameAddr);
                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                //Console.WriteLine(i + " " + name);
                list.Add(name);
            }

            return list;
        }

        private void CheckAntiLure(IntPtr hWnd, int hProcess)
        {
            if (!Window.ItemAntiLure)
            {
                return;
            }

            if (Common.ExpWindowShow(hWnd))
            {
                return;
            }

            Inventory inventory = Inventory.GetInventoryInfo(hProcess, true);
            Item item = inventory.FuzzySearch("驅魔香");
            if (item == null)
            {
                Window.MoveManager.AntiLure = false;

                item = inventory.FuzzySearch("驅魔背包");
                if (item == null)
                {
                    return;
                }

                if (Inventory.UseItem(hWnd, item))
                {
                    Log.WriteLine(Window.RoleName + " 使用 " + item.Name);
                }
            }
            else if (!Window.MoveManager.AntiLure)
            {
                if (Inventory.UseItem(hWnd, item))
                {
                    Log.WriteLine(Window.RoleName + " 使用 " + item.Name);
                    Window.UsingItemAntiLure = item;
                    Window.MoveManager.AntiLure = true;
                    Common.Delay(100);
                    Button.Inventory(hProcess, false);
                }
            }
        }

        private void CheckBattlePet(IntPtr hWnd, int hProcess)
        {
            if (!Window.AutoChangePet)
            {
                return;
            }

            ArrayList petList = PetInfo.GetAllPetsInfo(hProcess);
            foreach (PetInfo pet in petList)
            {
                if (pet.BattleState == 0x2)
                {
                    if (pet.MP >= 37)
                    {
                        return;
                    }
                    /*
                    foreach (PetSkill skill in pet.SkillList)
                    {
                        if (skill.Cost != 0)
                        {
                            if (skill.Name.Contains("超強") && pet.MP > skill.Cost)
                            {
                                return;
                            }

                            if (pet.MP > skill.Cost)
                            {
                                return;
                            }
                        }
                    }
                    */
                }
            }

            int petChangeIndex = -1;
            foreach (PetInfo pet in petList)
            {
                if (pet.LV == 1)
                {
                    continue;
                }

                if (pet.BattleState != 0x2)
                {
                    foreach (PetSkill skill in pet.SkillList)
                    {
                        if (skill.Cost == 0)
                        {
                            continue;
                        }

                        if (pet.MP > 37)
                        {
                            petChangeIndex = petList.IndexOf(pet);
                            break;
                        }
                    }
                }

                if (petChangeIndex != -1)
                {
                    break;
                }
            }

            //0x3B589 取消待命
            //0x3B58A 待命
            //0x3B58C 取消戰鬥
            //0x3B58D 戰鬥

            if (petChangeIndex != -1)
            {
                Button.OpenPetWindow(hProcess);
                ArrayList buttonList = Button.SearchButton(hProcess, new int[] { 0x3B58C, 0x3B58D });
                if (petList.Count == buttonList.Count)
                {
                    Button button = (Button)buttonList[petChangeIndex];
                    if (button != null)
                    {
                        int fakeClickAddr = CGCall.ClickButton(hProcess, button.CallAddr);
                        if (fakeClickAddr != 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                            Common.Delay(50);
                            WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);

                            Log.WriteLine(Window.RoleName + " 更換寵物 " + ((PetInfo)petList[petChangeIndex]).Name);
                        }
                    }
                }
            }
            else
            {
                if (Common.GetMapState(hProcess) == MapState.STATE_BATTLE)
                {
                    StopAutoItemBattle = true;
                }
            }
        }

        private bool UseItemLure(IntPtr hWnd, int hProcess)
        {
            TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
            if (teamInfo.TeamLeader || teamInfo.Member.Count == 0)
            {
                Inventory inventory = Inventory.GetInventoryInfo(hProcess, true);
                Item item = inventory.FuzzySearch("誘魔香", new string[] { "粉末", "背包" });
                if (item != null)
                {
                    if (Inventory.UseItem(hWnd, item))
                    {
                        if (Window.UsingItemLure == null)
                        {
                            Window.UsingItemLure = item;
                        }

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
