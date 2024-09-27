using CGHelper.CG.Enum;
using CommonLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CGHelper.CG
{
    public class ProduceController
    {
        private GameWindow Window;

        public static Dictionary<string, int> ShoppingCart { get; set; } = new Dictionary<string, int>();

        bool ClearItemSelectable { get; set; } = false;

        public ProduceController(GameWindow window)
        {
            Window = window;
        }

        private int GetMaterialInfoAddr(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.ProduceSkillIndexAddr, out int produceSkillIndex, 4, 0);
            WinAPI.ReadProcessMemory(hProcess, CGAddr.ProduceIndexAddr, out int produceIitemIndex, 4, 0);

            if (Window.ClassName.Equals("御守魔力"))
            {
                int produceSkillIndexOffset = produceSkillIndex * 0x49FC;
                int produceIitem = produceIitemIndex * 0x134;

                return CGAddr.ProduceRequestItemInfoAddr + produceSkillIndexOffset + produceIitem + 0x6C;
            }
            else
            {
                int produceSkillIndexOffset = produceSkillIndex * 0x4C4C;
                int produceIitem = (produceIitemIndex * 5);
                produceIitem <<= 3;
                produceIitem -= produceIitemIndex;
                produceIitem += produceIitem;
                produceIitem -= produceIitemIndex;

                //Console.WriteLine("produceIitem = 0x" + produceIitem.ToString("X"));

                return CGAddr.ProduceRequestItemInfoAddr + produceSkillIndexOffset + produceIitem * 4;
            }
        }

        private ArrayList GetMaterialList(int hProcess)
        {
            ArrayList list = new ArrayList();
            int materialInfoAddr = GetMaterialInfoAddr(hProcess);
            for (int materialIndex = 0; materialIndex < 5; materialIndex++)
            {
                int infoAddr = materialInfoAddr + materialIndex * 0x28;
                WinAPI.ReadProcessMemory(hProcess, infoAddr, out int id, 4, 0);
                if (id == -1)
                    continue;

                WinAPI.ReadProcessMemory(hProcess, infoAddr + 0x4, out int number, 4, 0);

                Material material = new Material(materialIndex)
                {
                    Id = id,
                    Number = number
                };
                //Console.WriteLine("material = 0x" + infoAddr.ToString("X") + " 0x" + material.Id.ToString("X") + " 0x" + material.Number.ToString("X"));

                list.Add(material);
            }
            list.Reverse();

            return list;
        }

        public void Watcher()
        {
            int hProcess = Window.HandleProcess;

            if (Common.GetMapState(hProcess) == MapState.STATE_MAP)
            {
                return;
            }

            SkillRestore();
            SkillRestoreInjured();
            //TestUsingItem();

            WinAPI.ReadProcessMemory(hProcess, CGAddr.WorkIconAddr, out int workIcon, 4, 0);
            //Console.WriteLine("workIcon = " + workIcon.ToString("X"));
            // 0x5伐木 0x6挖礦 0x7狩獵 0x9造斧 0xA料理 0xB鑑定 0xC修理 0x10剪取
            if (workIcon == 0x9 || workIcon == 0xA)
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.ProduceWorkingAddr, out int working, 1, 0);
                if (working == 0)
                {
                    SelectMaterial();
                    ClickExecute();
                }
                else if (working == 2)
                {
                    ClickRetry();
                }
            }
            else if (workIcon == 0xB)
            {
                AppraisalItem();
            }
            else if (workIcon == 0xC)
            {
                FixEquip();
            }
            else
            {
                if (Window.FixMode)
                {
                    AutoUseFixEquip();
                }

                if (ClearItemSelectable)
                {
                    /*
                    WinAPI.ReadProcessMemory(Window.HandleProcess, CGAddr.NpcTradeWindowAddr, out int npcTradeWindow, 4, 0);
                    if (npcTradeWindow == 0)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            WinAPI.ReadProcessMemory(Window.HandleProcess, CGAddr.ItemSelectableAddr + i, out int itemSelectable, 1, 0);
                            if (itemSelectable != 0)
                            {
                                //Console.WriteLine(i + " " + itemSelectable.ToString("X"));
                                WinAPI.WriteProcessMemory(Window.HandleProcess, CGAddr.ItemSelectableAddr + i, BitConverter.GetBytes(0x0), 1, 0);
                            }
                        }
                    }
                    */
                    ClearItemSelectable = false;
                }

                WinAPI.ReadProcessMemory(hProcess, CGAddr.TradeFlagAddr, out int trade, 4, 0);
                if (trade == 0)
                {
                    WinAPI.ReadProcessMemory(hProcess, CGAddr.ConfirmEnableAddr, out int confirm, 4, 0);
                    if (confirm == 0)
                    {
                        Inventory inventory = Inventory.GetInventoryInfo(hProcess);

                        int totalCost = 0;

                        foreach (KeyValuePair<string, int> item in ShoppingCart)
                        {
                            int reservationNumber = 0;
                            int holdingNumber = inventory.GetItemNumber(item.Key);
                            if (Window.ClassName.Equals("Blue") && item.Key.Equals("綿"))
                            {
                                holdingNumber = inventory.GetItemNumber("棉布");
                            }

                            if (holdingNumber >= item.Value)
                            {
                                continue;
                            }

                            for (int i = 9; i >= 0; i--)
                            {
                                int itemAddr = CGAddr.NPCItemListAddr + CGAddr.ItemListOffset * i;
                                WinAPI.ReadProcessMemory(hProcess, itemAddr, out int empty, 1, 0);
                                if (empty == 0)
                                {
                                    continue;
                                }

                                int buyNumber = item.Value - holdingNumber - reservationNumber;
                                string name = Common.GetNameFromAddr(hProcess, itemAddr + 1);
                                if (string.IsNullOrEmpty(name))
                                {
                                    break;
                                }

                                if (Window.ClassName.Equals("Blue") && name.StartsWith("棉布"))
                                {
                                    name = name.Replace(" ", "").Replace("棉布", "綿");
                                }

                                if (name.Equals(item.Key))
                                {
                                    if (reservationNumber == 0 || buyNumber <= 20)
                                    {
                                        WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x34, out int itemPrice, 4, 0);
                                        WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x1C, out int selectable, 4, 0);

                                        if (selectable == 1)
                                        {
                                            int number = Math.Min(buyNumber, 20);
                                            WinAPI.WriteProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x30, BitConverter.GetBytes(number), 4, 0);
                                            totalCost += number * itemPrice;
                                            reservationNumber += number;
                                        }
                                    }
                                }
                                else if (name.StartsWith(item.Key))
                                {
                                    Match match = Regex.Match(name, "[0-9]+");
                                    if (match.Success)
                                    {
                                        int.TryParse(match.Value, out int setNumber);

                                        WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x34, out int itemPrice, 4, 0);
                                        WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x1C, out int selectable, 4, 0);

                                        if (selectable == 1)
                                        {
                                            int number = Math.Min(buyNumber / setNumber, 20);
                                            WinAPI.WriteProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x30, BitConverter.GetBytes(number), 4, 0);
                                            totalCost += number * itemPrice;
                                            reservationNumber += number * setNumber;
                                        }
                                    }
                                }
                            }
                        }

                        if (totalCost > 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, CGAddr.TotalCostAddr, BitConverter.GetBytes(totalCost), 4, 0);
                            WinAPI.WriteProcessMemory(hProcess, CGAddr.ConfirmEnableAddr, BitConverter.GetBytes(1), 4, 0);
                        }
                    }
                } 
                else if (trade == 1)
                {
                    for (int i = 0; i < 20 ; i++)
                    {
                        int itemAddr = CGAddr.NPCItemListAddr + CGAddr.ItemListOffset * i;
                        WinAPI.ReadProcessMemory(hProcess, itemAddr, out int empty, 1, 0);
                        if (empty == 0)
                        {
                            continue;
                        }
                        string name = Common.GetNameFromAddr(hProcess, itemAddr + 1);
                        WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x20, out int set, 4, 0);
                        //WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x1C, out int sale, 4, 0);
                        if (set == 3)
                        {
                            WinAPI.WriteProcessMemory(hProcess, itemAddr + CGAddr.ItemListOffset - 0x1C, BitConverter.GetBytes(0), 4, 0);
                        }
                    }
                } 
                else if (trade == 2)
                {
                    //礦壓條
                    WinAPI.ReadProcessMemory(hProcess, CGAddr.ChangeNumberAddr, out int changeNumber, 4, 0);
                    if (changeNumber > 0)
                    {
                        WinAPI.ReadProcessMemory(hProcess, CGAddr.ConfirmEnableAddr, out int confirm, 4, 0);
                        WinAPI.ReadProcessMemory(hProcess, CGAddr.RequestNumberAddr, out int requestNumber, 4, 0);
                        if (requestNumber != 0 && confirm == 0 && changeNumber >= requestNumber)
                        {
                            int itemAddr = CGAddr.NPCItemListAddr;
                            WinAPI.ReadProcessMemory(hProcess, itemAddr, out int empty, 1, 0);
                            if (empty != 0)
                            {
                                string name = Common.GetNameFromAddr(hProcess, itemAddr + 1);
                                if (!string.IsNullOrEmpty(name) && name.Contains("條("))
                                {
                                    int number = changeNumber / requestNumber;
                                    WinAPI.WriteProcessMemory(hProcess, CGAddr.RequestNumberAddr + 0x4, BitConverter.GetBytes(number), 4, 0);
                                    WinAPI.WriteProcessMemory(hProcess, CGAddr.TotalCostAddr, BitConverter.GetBytes(number * requestNumber), 4, 0);
                                    WinAPI.WriteProcessMemory(hProcess, CGAddr.ConfirmEnableAddr, BitConverter.GetBytes(1), 4, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TestUsingItem()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            int HP = Common.GetXORValue(hProcess, CGAddr.HPAddr);
            int MaxHP = Common.GetXORValue(hProcess, CGAddr.MAXHPAddr);

            if (HP == MaxHP)
            {
                //return;
            }

            if (!SkillSelectTarget(Window.RoleName))
            {
                if (!SkillSelectHuman(Window.RoleName))
                {
                    //ArrayList itemWindow = WindowObject.SearchWindow(hProcess, 0x4815E0);
                    ArrayList itemWindow = WindowObject.SearchWindow(hProcess, 0x4C19B0);
                    if (itemWindow.Count > 0)
                    {
                        WindowObject windowObject = (WindowObject)itemWindow[0];
                        

                        //int fakeUseItemAddr = CGCall.UseItem(hWnd, windowObject.CallAddr, 0);
                        int fakeUseItemAddr = CGCall.TestUseItem(hProcess, windowObject.CallAddr, 0, 0x184);
                        if (fakeUseItemAddr != 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeUseItemAddr), 4, 0);

                            /*
                            int fakeSelectItemAddr = CGCall.SelectItem(hWnd, 10);
                            if (fakeSelectItemAddr != 0)
                            {
                                int selectItemAddr = 0x4817D5;
                                int selectItemCallAddr = 0x4FA990;

                                WinAPI.WriteProcessMemory(hProcess, selectItemAddr + 0x1, BitConverter.GetBytes(fakeSelectItemAddr - (selectItemAddr + 0x5)), 4, 0);
                                WinAPI.WriteProcessMemory(hProcess, 0x481877 + 0x1, BitConverter.GetBytes(0x85), 1, 0);
                                Common.Delay(20);
                                WinAPI.WriteProcessMemory(hProcess, 0x481877 + 0x1, BitConverter.GetBytes(0x84), 1, 0);
                                WinAPI.WriteProcessMemory(hProcess, selectItemAddr + 0x1, BitConverter.GetBytes(selectItemCallAddr - (selectItemAddr + 0x5)), 4, 0);
                            }
                            */
                            Common.Delay(20);
                            WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                        }
                    }
                    else
                    {
                        Button.Inventory(hProcess);
                    }
                }
            }
        }

        public ArrayList GetSkillToTargetList()
        {
            int hProcess = Window.HandleProcess;

            ArrayList list = new ArrayList();
            for (int i = 0; i < 5; i++)
            {
                int nameAddr = CGAddr.SkillToTargetListAddr + i * 0x11;
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

        public ArrayList GetSkillToHumanList()
        {
            int hProcess = Window.HandleProcess;

            ArrayList list = new ArrayList();
            for (int i = 0; i < 5; i++)
            {
                int nameAddr = CGAddr.SkillToHumanListAddr + i * 0x11;
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

        public static ArrayList GetInjuredGroupMember(int hProcess)
        {
            ArrayList injuredList = new ArrayList();

            for (int i = 0; i < 1000; i++)
            {
                int addr = CGAddr.ObjectListAddr + i * 0x13C;
                WinAPI.ReadProcessMemory(hProcess, addr, out int temp, 4, 0);
                if (temp == 0)
                    break;
                int X = Common.GetXORValue(hProcess, addr + 0xC);
                int Y = Common.GetXORValue(hProcess, addr + 0x1C);

                WinAPI.ReadProcessMemory(hProcess, addr + 0x43, out int isTeamMember, 1, 0);

                WinAPI.ReadProcessMemory(hProcess, addr + 0x11C, out int namePtr, 4, 0);
                string name = Common.GetNameFromAddr(hProcess, namePtr + 0xC4);

                WinAPI.ReadProcessMemory(hProcess, addr + 0x120, out int state, 4, 0);
                bool injured = (state & 0x1) == 1;

                if ((isTeamMember & 0x2) > 0 && injured)
                {
                    Console.WriteLine("0x" + addr.ToString("X") + " " + temp.ToString("X") + " " + name + "(" + X + "," + Y + ") " + isTeamMember + " " + injured);
                    injuredList.Add(name);
                }
            }

            WinAPI.ReadProcessMemory(hProcess, CGAddr.HealthAddr, out int selfHeath, 4, 0);
            if (selfHeath > 0)
            {
                //injuredList.Add(Window.FigureName);
            }

            return injuredList;
        }

        private bool SelectSkill(int skillOrder)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //選擇技能
            ArrayList selectList = WindowObject.SearchWindow(hProcess, CGAddr.SelectSkillCallAddr);
            if (selectList.Count > 0)
            {
                WindowObject windowObject = (WindowObject)selectList[skillOrder];
                int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                }
                return true;
            }
            return false;
        }

        private bool SkillSelectTarget(string targetName)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //治療/急救-選擇人物/寵物
            ArrayList skillSelectTargetList = WindowObject.SearchWindow(hProcess, CGAddr.SkillSelectTargetCallAddr);
            if (skillSelectTargetList.Count > 0)
            {
                ArrayList list = GetSkillToTargetList();
                int index = list.IndexOf(targetName);
                if (index != -1)
                {
                    WindowObject windowObject = (WindowObject)skillSelectTargetList[index];
                    int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                        Common.Delay(50);
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                    }
                }
                return true;
            }

            return false;
        }

        private bool SkillSelectHuman(string targetName)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //治療/急救-選擇人物
            ArrayList skillSelectHumanList = WindowObject.SearchWindow(hProcess, CGAddr.SkillSelectHumanCallAddr);
            if (skillSelectHumanList.Count > 0)
            {
                ArrayList list = GetSkillToHumanList();
                int index = list.IndexOf(targetName);
                if (index != -1)
                {
                    WindowObject windowObject = (WindowObject)skillSelectHumanList[index];
                    int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                    if (fakeClickAddr != 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                        Common.Delay(50);
                        WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                    }
                }
                return true;
            }

            return false;
        }

        private void SkillSelectLevel(int skillLevel)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //選擇技能等級
            ArrayList skillLvSelectList = WindowObject.SearchWindow(hProcess, CGAddr.SelectSkillLevelCallAddr);
            if (skillLvSelectList.Count > 0)
            {
                WindowObject windowObject = (WindowObject)skillLvSelectList[skillLevel];
                int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(windowObject.CallAddr), 4, 0);
                }
            }
        }

        public void SkillRestoreInjured()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (Window.UseSkills.IndexOf("治療") == -1)
            {
                return;
            }

            Skill skill = Skill.SearchSkill(hProcess, "治療");
            if (skill == null || skill.UseLevel == -1)
            {
                return;
            }

            ArrayList injuredList = TeamInfo.GetInjuredTeamMember(hProcess);
            if (injuredList.Count == 0)
            {
                return;
            }

            Button.OpenSkillWindow(hProcess);
            SelectSkill(skill.Order);

            foreach (string name in injuredList)
            {
                if (!SkillSelectTarget(name))
                {
                    if (!SkillSelectHuman(name))
                    {
                        SkillSelectLevel(skill.UseLevel);
                    }
                }
            }
        }

        public void SkillRestore()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (Window.UseSkills.IndexOf("急救") == -1)
            {
                return;
            }

            Skill skill = Skill.SearchSkill(hProcess, "急救");
            if (skill == null || skill.UseLevel == -1)
            {
                return;
            }

            int HP = Common.GetXORValue(hProcess, CGAddr.HPAddr);
            int MaxHP = Common.GetXORValue(hProcess, CGAddr.MAXHPAddr);

            if (HP == MaxHP)
            {
                return;
            }

            Button.OpenSkillWindow(hProcess);
            SelectSkill(skill.Order);
            if (!SkillSelectTarget(Window.RoleName))
            {
                if (!SkillSelectHuman(Window.RoleName))
                {
                    SkillSelectLevel(skill.UseLevel);
                }
            }
        }

        private void AppraisalItem()
        {
            int hProcess = Window.HandleProcess;

            WinAPI.ReadProcessMemory(hProcess, CGAddr.ProduceWorkingAddr, out int working, 4, 0);
            if ((working & 0x3) == 0x3)
            {
                Skill skill = Skill.SearchSkill(hProcess, "鑑定");
                if (Window.ClassName.Equals("御守魔力") && skill == null)
                {
                    skill = Skill.SearchSkill(hProcess, "鑒定");
                }

                foreach (Item item in Inventory.GetInventoryInfo(hProcess, true).ItemList)
                {
                    if (item.Appraisal == 1 || item.Level > skill.Level)
                        continue;

                    SelectItem(item);
                    if (ClickExecuteSingle())
                    {
                        Log.WriteLine("Appraise item " + (item.Index + 1) + " " + item.Name + "(0x" + item.Id.ToString("X") + ")");
                    }

                    break;
                }
            }
            else if ((working & 0x5) == 0x5)
            {
                ClickRetry();
            }
        }

        public void AutoUseFixEquip()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            bool weaponSkill = true;
            Skill skill = Skill.SearchSkill(hProcess, "修理武器");
            if (skill == null)
            {
                skill = Skill.SearchSkill(hProcess, "修理防具");
                weaponSkill = false;
            }

            if (skill == null || skill.Level == -1)
            {
                return;
            }

            foreach (Item item in Inventory.GetInventoryInfo(hProcess, true).ItemList)
            {
                if (item.Level > skill.Level)
                {
                    continue;
                }

                if (weaponSkill && item.Type >= 0x7 || !weaponSkill && item.Type < 0x7)
                {
                    continue;
                }

                if (!Equipment.IsFullDurability(hProcess, item))
                {
                    Button.OpenSkillWindow(hProcess);
                    SelectSkill(skill.Order);
                    //修理只有一格技能等級
                    SkillSelectLevel(0);
                    Log.WriteLine(Window.RoleName + " use " + skill.Name + skill.Level);
                    break;
                }
            }
        }

        private void FixEquip()
        {
            int hProcess = Window.HandleProcess;

            WinAPI.ReadProcessMemory(hProcess, CGAddr.ProduceWorkingAddr, out int working, 4, 0);
            if (working == 0x3)
            {
                bool weaponSkill = true;
                Skill skill = Skill.SearchSkill(hProcess, "修理武器");
                if (skill == null)
                {
                    skill = Skill.SearchSkill(hProcess, "修理防具");
                    weaponSkill = false;
                }

                foreach (Item item in Inventory.GetInventoryInfo(hProcess, true).ItemList)
                {
                    if (item.Level > skill.Level)
                    {
                        continue;
                    }

                    if (weaponSkill && item.Type >= 0x7 || !weaponSkill && item.Type < 0x7)
                    {
                        continue;
                    }

                    if (Equipment.IsFullDurability(hProcess, item))
                    {
                        continue;
                    }

                    SelectItem(item);
                    if (ClickExecuteSingle())
                    {
                        Log.WriteLine(Window.RoleName + " Fix item " + (item.Index + 1) + " " + item.Name);
                    }
                    break;
                }
            }
            else if (working == 5)
            {
                ClickRetry();
            }
        }

        private void SelectMaterial()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            ArrayList materialList = GetMaterialList(hProcess);

            Inventory inventory = Inventory.GetInventoryInfo(hProcess);
            if (Window.ClassName.Equals("御守魔力"))
            {
                if (inventory.EmptyNumber <= 2)
                {
                    Common.PressKey(hWnd, System.Windows.Forms.Keys.F8);
                }
            }
                
            if (!inventory.CheckMaterials(materialList))
            {
                return;
            }

            foreach (Material material in materialList)
            {
                int readyAddr = CGAddr.ProduceRequestItemReadyAddr + material.Index * 4;
                WinAPI.ReadProcessMemory(hProcess, readyAddr, out int ready, 4, 0);
                if (ready == 1)
                    continue;

                Item item = inventory.SearchMaterial(material);
                if (item != null)
                {
                    //要先寫入否則會被清除材料
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceRequestItemIndexAddr + material.Index * 4, BitConverter.GetBytes(material.Index), 4, 0);

                    WinAPI.WriteProcessMemory(hProcess, readyAddr, BitConverter.GetBytes(0x1), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceRequestItemFromItemIndexAddr + material.Index * 4, BitConverter.GetBytes(item.Index), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceRequestItemFromItemIconAddr + material.Index * 4, BitConverter.GetBytes(item.Icon), 4, 0);

                    WinAPI.WriteProcessMemory(hProcess, CGAddr.ItemSelectableAddr + item.Index, BitConverter.GetBytes(0x1), 1, 0);

                    ClearItemSelectable = true;

                    //Log.WriteLine("selected item " + (item.Index + 1) + " " + item.Name + " 0x" + item.Addr.ToString("X") + " id = 0x" + item.Id.ToString("X") + " icon = 0x" + item.Icon.ToString("X") + " number = " + item.Number);
                }
            }
        }

        private void SelectItem(Item item)
        {
            int hProcess = Window.HandleProcess;

            //要先寫入否則會被清除裝備
            WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceSingleSelect, BitConverter.GetBytes(0x0), 4, 0);

            WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceSingleFromItemIndexAddr, BitConverter.GetBytes(item.Index + 0x8), 4, 0);
            WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceSingleFromItemIconAddr, BitConverter.GetBytes(item.Icon), 4, 0);

            WinAPI.WriteProcessMemory(hProcess, CGAddr.ItemSelectableAddr + item.Index, BitConverter.GetBytes(0x1), 1, 0);
            ClearItemSelectable = true;
        }

        private void ClickRetry()
        {
            if (!Window.AutoProduce)
            {
                return;
            }

            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //0x3BA31   生產-重試
            Button button = Button.SearchButton(hProcess, 0x3BA31);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ProduceRetry(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                }
            }
        }

        private void ClickExecute()
        {
            if (!Window.AutoProduce)
            {
                return;
            }

            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //0x8FD3    生產-執行
            Button button = Button.SearchButton(hProcess, 0x8FD3);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ProduceExecute(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                }
            }
        }

        private bool ClickExecuteSingle()
        {
            if (!Window.AutoProduce)
            {
                return false;
            }

            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            //0x8FD3    生產-執行
            Button button = Button.SearchButton(hProcess, 0x8FD3);
            if (button != null)
            {
                int fakeClickAddr = CGCall.ProduceExecute(hProcess, button.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, button.CallAddrPtr, BitConverter.GetBytes(button.CallAddr), 4, 0);
                    return true;
                }
                //WinAPI.WriteProcessMemory(hProcess, CGAddr.ProduceSingleExecuteAddr + 0xD, BitConverter.GetBytes(0x4), 4, 0);
                //return true;
            }

            return false;
        }
    }
}
