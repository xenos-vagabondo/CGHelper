using CGHelper.CG.Pet;
using CGHelper.CG.Battle;
using CommonLibrary;
using System;
using System.Collections;
using System.Text;
using CGHelper.CG.Enum;

namespace CGHelper.CG
{
    public class BattleController {

        private static string TAG = "Battle";

        private GameWindow Window { get; set; }

        private int Round { get; set; }
        private int LastRound { get; set; }
        private ArrayList RecoverRoundList { get; set; } = new ArrayList();

        public int SkillThreshold { get; set; } = 3;

        public BattleController(GameWindow window)
        {
            Window = window;
        }

        public void Watcher()
        {
            int hProcess = Window.HandleProcess;

            MapState state = Common.GetMapState(hProcess);
            if (state != MapState.STATE_BATTLE)
            {
                if (Round != -1 || LastRound != -1)
                {
                    Round = LastRound = -1;
                }

                return;
            }

            if (Round == -1)
            {
                Log.WriteLine(TAG, Window.RoleName + " 開始戰鬥!!!");
                if (RecoverRoundList.Count > 0)
                {
                    RecoverRoundList = new ArrayList();
                }
            }

            Round = Common.GetBattleRound(hProcess);

            int battleStep = 0;
            if (Window.ClassName.Equals("御守魔力"))
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.MapStateAddr - 0x4, out battleStep, 4, 0);
            }
            else
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.MapStateAddr + 0x24, out battleStep, 4, 0);
            }

            BattleGroup enemy = GetEnemyGroup(hProcess);

            PetInfo currentPet = null;
            foreach (PetInfo pet in PetInfo.GetAllPetsInfo(hProcess))
            {
                if (pet.BattleState == 0x2)
                {
                    currentPet = pet;
                    break;
                }
            }

            //避免人物技能視窗跳出
            WinAPI.ReadProcessMemory(hProcess, CGAddr.LastBattleCommandAddr, out int lastBattleCommand, 4, 0);
            if (Window.AutoAttack && lastBattleCommand != 0)
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.LastBattleCommandAddr, BitConverter.GetBytes(0x0), 4, 0);
            }

            //避免寵物技能視窗跳出
            if (Window.PetAutoAttack && currentPet != null)
            {
                foreach (PetSkill skill in currentPet.SkillList)
                {
                    if (skill.Name.Contains("攻擊"))
                    {
                        int skillIndex = currentPet.SkillList.IndexOf(skill);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);
                        break;
                    }
                }
            }

            if (battleStep == 0x4 || battleStep == 0x5)
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.CommandAddr, out int step, 4, 0);
                switch (step)
                {
                    case 0x1://人物回合
                        RoleCommand(enemy);
                        break;
                    case 0x4://寵物回合
                        PetCommand(currentPet, enemy);
                        break;
                }
            }
            else if (battleStep == 0x6)
            {
                //戰鬥回合
                if (LastRound != Round)
                {
                    LastRound = Round;
                }
            }
        }

        public static bool IsTargeShade(ArrayList targetList, BattleTarget target)
        {
            foreach (BattleTarget shadeTarget in targetList)
            {
                if (shadeTarget.Index == target.Index + 5)
                    return true;
            }

            return false;
        }

        public static void ModifyTargetInfo(int hProcess, ArrayList list)
        {
            foreach(BattleTarget target in list)
            {
                if (string.IsNullOrEmpty(target.Name))
                {
                    continue;
                }

                if (target.Name.Contains("(") && target.Name.Contains("/") && target.Name.Contains(")"))
                {
                    target.Name = target.Name.Split('(')[0];
                }
                string reName = target.Name + "(" + target.HP + "/" + target.MP + ")";

                if (!target.Name.Equals(reName))
                {
                    byte[] bytes = Encoding.Default.GetBytes(reName);
                    if (Common.GB2312)
                    {
                        string gb = ChineseConverter.ToSimplified(reName);
                        bytes = Encoding.GetEncoding("GB2312").GetBytes(gb);
                    }
                    
                    WinAPI.WriteProcessMemory(hProcess, target.Addr + 0xC4, bytes, bytes.Length + 1, 0);
                }
            }
        }

        public void RecordRecoverRound(BattleTarget cureTarget)
        {
            //Console.WriteLine("RecordRecoverRound " + cureTarget + " Round " + (Round + 1));
            RecoverRoundList.Add(new BattleRecoverRound(cureTarget.Index, Round + 1));
        }

        public void RecordRecoverRound(ArrayList multipleCureTarget)
        {
            foreach (BattleTarget cureTarget in multipleCureTarget)
            {
                RecordRecoverRound(cureTarget);
            }
        }

        public static bool IsRecoveringTarget(BattleTarget target, ArrayList recoverRoundList, int round)
        {
            foreach (BattleRecoverRound recoverTarger in recoverRoundList)
            {
                if (recoverTarger.Index == target.Index && round - recoverTarger.Round < 6)
                {
                    //Console.WriteLine("IsRecoveringTarget " + recoverTarger.Index + " " + target.Index + " " + round + " " + recoverTarger.Round);
                    return true;
                }
            }

            return false;
        }

        public static ArrayList GetCureTargetNearbyList(BattleTarget target, ArrayList list, int threshold)
        {
            ArrayList cureTargetNearbyList = new ArrayList();
            foreach (BattleTarget nearTarget in list)
            {
                if (nearTarget.HP == 0 || nearTarget.MaxHP - nearTarget.HP < threshold)
                {
                    continue;
                }

                if (nearTarget.Index % 5 == target.Index % 5)
                {
                    cureTargetNearbyList.Add(nearTarget);
                    continue;
                }

                if (nearTarget.Index > target.Index + 2)
                {
                    continue;
                }

                if (nearTarget.Index < target.Index - target.Index % 5)
                {
                    continue;
                }

                if (target.Index % 5 == 1 && nearTarget.Index == target.Index + 1)
                {
                    continue;
                }
                else if (target.Index % 5 == 2 && nearTarget.Index % 5 % 2 == 1)
                {
                    continue;
                }
                else if (target.Index % 5 == 3 && nearTarget.Index % 5 != 1)
                {
                    continue;
                }
                else if (target.Index % 5 == 4 && nearTarget.Index % 5 != 2)
                {
                    continue;
                }

                cureTargetNearbyList.Add(nearTarget);
            }

            return cureTargetNearbyList;
        }

        public void SelectTarget(BattleTarget target, bool isHuman = true)
        {
            if (target == null)
                return;

            IntPtr hwnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (isHuman)
            {
                if (Window.ClassName.Equals("御守魔力"))
                {
                    Common.MoveMouse(hProcess, 640 + new Random().Next(-10, 10), 480 + new Random().Next(-10, 10));

                    WinAPI.ReadProcessMemory(hProcess, CGAddr.CommandAddr - 0x4C, out int command, 4, 0);

                    int count = 0;
                    while (count++ < 5)
                    {
                        WinAPI.WriteProcessMemory(hProcess, target.CodeAddr, BitConverter.GetBytes(target.Index), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.TargetCodeAddr, BitConverter.GetBytes(target.Index), 4, 0);

                        Common.ClickMouseLeftButton(hProcess, WinAPI.IsIconic(hwnd));
                        Common.Delay(100);

                        WinAPI.ReadProcessMemory(hProcess, CGAddr.CommandAddr - 0x4C, out int done, 4, 0);
                        if (command != done)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Common.Delay(new Random().Next(250, 500));

                    WinAPI.ReadProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr + 0x1, out int selectCallAddr, 4, 0);

                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr, BitConverter.GetBytes(0xB8), 1, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr + 0x1, BitConverter.GetBytes(target.Index), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr + 0x5, BitConverter.GetBytes(0x9013EB), 3, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr, BitConverter.GetBytes(0xE8), 1, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr + 0x1, BitConverter.GetBytes(selectCallAddr), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.HumanSelectCallAddrPtr + 0x5, BitConverter.GetBytes(0xFFF883), 3, 0);
                }
            } 
            else
            {
                if (Window.ClassName.Equals("御守魔力"))
                {
                    int count = 0;
                    while (count++ < 5)
                    {
                        WinAPI.WriteProcessMemory(hProcess, target.CodeAddr, BitConverter.GetBytes(target.Index), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.TargetCodeAddr, BitConverter.GetBytes(target.Index), 4, 0);
                        //滑鼠點擊
                        Common.ClickMouseLeftButton(hProcess, WinAPI.IsIconic(hwnd));
                        Common.Delay(100);

                        WinAPI.ReadProcessMemory(hProcess, CGAddr.CommandAddr, out int step, 4, 0);
                        if (step != 0x4)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Common.Delay(new Random().Next(250, 500));

                    WinAPI.ReadProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x1, out int selectCallAddr, 4, 0);

                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr, BitConverter.GetBytes(0xB8), 1, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x1, BitConverter.GetBytes(target.Index), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x5, BitConverter.GetBytes(0x0AEB), 2, 0);
                    Common.Delay(100);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr, BitConverter.GetBytes(0xE8), 1, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x1, BitConverter.GetBytes(selectCallAddr), 4, 0);
                    if (Window.ClassName.Equals("魔力寶貝"))
                    {
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x5, BitConverter.GetBytes(0xC63B), 2, 0);
                    }
                    else if (Window.ClassName.Equals("Blue"))
                    {
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSelectCallAddrPtr + 0x5, BitConverter.GetBytes(0xC53B), 2, 0);
                    }
                }
            }
        }

        public void RoleCommand(BattleGroup enemy)
        {
            int hProcess = Window.HandleProcess;

            if (Window.AutoFlee)
            {
                ClickButton("逃跑");
            }
            else 
            {
                BattleGroup friendly = GetFriendlyGroup(hProcess);
                if (!CureSkill(friendly) && !SelfSkill(friendly) && !CapturePet(enemy, friendly.Role))
                {
                    BattleTarget target = GetHumanAttackTarget(enemy);
                    if (target != null)
                    {
                        Inventory inventory = Inventory.GetInventoryInfo(hProcess, true);
                        Item item = inventory.FuzzySearch("封印卡");
                        if (Window.UseSkills.IndexOf("精靈的盟約") != -1 && item != null)
                        {
                            ClickButton("物品");
                            if (Inventory.BattleUsingItem(Window, item))
                            {
                                Common.Delay(50);
                                SelectTarget(target);
                            }

                            return;
                        }

                        if (Window.AutoAttack)
                        {
                            if (!AttackSkill(enemy, target, friendly.Role))
                            {
                                Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + " 攻擊 " + target.ToString());
                                SelectTarget(target);
                            }
                        }
                    }
                }
            }
        }

        public void PetCommand(PetInfo currentPet, BattleGroup enemy)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (!PetSelfSkill(currentPet) && !PetCureSkill(currentPet))
            {
                if (Window.CaptureMode && enemy.LV1List.Count > 0)
                {
                    foreach (PetSkill skill in currentPet.SkillList)
                    {
                        if (skill.Name.Contains("防御"))
                        {
                            ArrayList selectList = WindowObject.SearchWindow(hProcess, 0x4B4B30);
                            if (selectList.Count > 0)
                            {
                                WindowObject windowObject = (WindowObject)selectList[currentPet.SkillList.IndexOf(skill)];
                                int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                                if (fakeClickAddr != 0)
                                {
                                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                                }
                            }
                            else
                            {
                                ClickButton("技能");
                            }
                        }
                    }
                }
                else
                {
                    if (!Window.PetAutoAttack)
                        return;

                    BattleTarget target = GetPetAttackTarget(enemy);
                    if (target != null)
                    {
                        if (!string.IsNullOrEmpty(target.Name) && target.Name.Equals("真．風神希爾芙"))
                        {
                            Console.WriteLine(target);
                        }

                        if (!PetAttackSkill(currentPet, enemy, target))
                        {
                            Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 攻擊 " + target.ToString());
                        }

                        SelectTarget(target, false);
                    }
                }
            }
        }

        public BattleTarget GetHumanAttackTarget(BattleGroup enemy)
        {
            int hProcess = Window.HandleProcess;

            BattleTarget prioritizeTarget = PrioritizeAttackTarget(enemy);
            if (prioritizeTarget != null)
            {
                return prioritizeTarget;
            }

            if (Equipment.IsWeaponKnife(hProcess))
            {
                foreach (BattleTarget shadeTarget in enemy.List)
                {
                    if (shadeTarget.Shade)
                    {
                        return shadeTarget;
                    }
                }
            }

            if (Equipment.IsWeaponBoomerang(hProcess))
            {
                if (enemy.FrontNumber > enemy.RearNumber)
                {
                    return (BattleTarget)enemy.List[new Random().Next(enemy.FrontNumber)];
                }
                else
                {
                    return (BattleTarget)enemy.List[new Random().Next(enemy.FrontNumber, enemy.List.Count)];
                }
            }

            foreach (BattleTarget target in enemy.List)
            {
                if ((target.SelectState & 0x8) == 0 || target.HP == 0)
                {
                    continue;
                }

                return target;
            }

            return null;
        }

        public BattleTarget GetPetAttackTarget(BattleGroup enemy)
        {
            if (enemy.RearNumber > 0)
            {
                BattleTarget randomTarget = (BattleTarget)enemy.List[new Random().Next(enemy.RearNumber)];
                if ((randomTarget.SelectState & 0x8) != 0 && (!randomTarget.Shade || enemy.List.Count <= 4))
                {
                    return randomTarget;
                }
            }

            if (enemy.FrontNumber > 0)
            {
                BattleTarget randomTarget = (BattleTarget)enemy.List[new Random().Next(enemy.RearNumber, enemy.List.Count)];
                if ((randomTarget.SelectState & 0x8) != 0)
                {
                    return randomTarget;
                }
            }

            return null;
        }

        private BattleTarget PrioritizeAttackTarget(BattleGroup enemy)
        {
            foreach (BattleTarget target in enemy.List)
            {
                if ((target.SelectState & 0x8) == 0 || target.HP == 0)
                {
                    continue;
                }

                if ("強化熊妹妹".Equals(target.Name))
                {
                    return target;
                }
                else if ("來幫忙的露比".Equals(target.Name))
                {
                    return target;
                }
                else if ("憤怒使者".Equals(target.Name))
                {
                    return target;
                }
                else if ("咒術之龍".Equals(target.Name))
                {
                    return target;
                }
                else if ("帕布提斯馬".Equals(target.Name))
                {
                    return target;
                }
            }

            return null;
        }

        public bool CureSkill(BattleGroup friendly)
        {
            int hProcess = Window.HandleProcess;

            if (Window.UseSkills.IndexOf("超強補血魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "超強補血魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    BattleTarget multipleCureTarget = null;
                    int needCureNumber = 0;
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0 || cureTarget.MaxHP - cureTarget.HP < (cureSkill.UseLevel + 1) * 25)
                        {
                            continue;
                        }

                        needCureNumber++;
                        multipleCureTarget = cureTarget;
                    }

                    if (needCureNumber > 4 || Window.SkillMode)
                    {
                        if (multipleCureTarget != null)
                        {
                            if ((multipleCureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, multipleCureTarget.Addr + 0x24, BitConverter.GetBytes(multipleCureTarget.SelectState | 0x8), 4, 0);
                                multipleCureTarget.ModifySelectState = true;
                            }
                            if (UseSkill(cureSkill, multipleCureTarget))
                            {
                                return true;
                            }
                        }

                    }
                }
            }

            if (Window.UseSkills.IndexOf("強力補血魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "強力補血魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    BattleTarget multipleCureTarget = null;
                    int maxCureNumberNearby = 0;
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0)
                        {
                            continue;
                        }

                        ArrayList cureTargetNearbyList = GetCureTargetNearbyList(cureTarget, friendly.List, (cureSkill.UseLevel + 1) * 35);
                        if (maxCureNumberNearby < cureTargetNearbyList.Count)
                        {
                            maxCureNumberNearby = cureTargetNearbyList.Count;
                            multipleCureTarget = cureTarget;
                        }
                    }

                    if (Window.SkillMode)
                    {
                        multipleCureTarget = (BattleTarget)friendly.List[0];
                    }

                    if (maxCureNumberNearby > 1 || Window.SkillMode)
                    {
                        if (multipleCureTarget != null)
                        {
                            if ((multipleCureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, multipleCureTarget.Addr + 0x24, BitConverter.GetBytes(multipleCureTarget.SelectState | 0x8), 4, 0);
                                multipleCureTarget.ModifySelectState = true;
                            }

                            if (UseSkill(cureSkill, multipleCureTarget))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("補血魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "補血魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0)
                        {
                            continue;
                        }

                        if ((float)cureTarget.HP / cureTarget.MaxHP <= 0.75 || Window.SkillMode)
                        {
                            if ((cureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, cureTarget.Addr + 0x24, BitConverter.GetBytes(cureTarget.SelectState | 0x8), 4, 0);
                                cureTarget.ModifySelectState = true;
                            }

                            if (!Window.SkillMode)
                            {
                                cureSkill.UseLevel = Math.Min((cureTarget.MaxHP - cureTarget.HP) / 85, cureSkill.UseLevel);
                            }
                            
                            if (UseSkill(cureSkill, cureTarget))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("超強恢復魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "超強恢復魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    BattleTarget multipleCureTarget = null;
                    ArrayList cureList = new ArrayList();
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0 || IsRecoveringTarget(cureTarget, RecoverRoundList, Round))
                        {
                            continue;
                        }

                        cureList.Add(cureTarget);
                        multipleCureTarget = cureTarget;
                    }

                    if (cureList.Count > 0 || Window.SkillMode)
                    {
                        if (multipleCureTarget != null)
                        {
                            if ((multipleCureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, multipleCureTarget.Addr + 0x24, BitConverter.GetBytes(multipleCureTarget.SelectState | 0x8), 4, 0);
                                multipleCureTarget.ModifySelectState = true;
                            }
                            RecordRecoverRound(cureList);
                            if (UseSkill(cureSkill, multipleCureTarget))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("強力恢復魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "強力恢復魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    ArrayList multipleCureTargetNearbyList = new ArrayList();
                    BattleTarget multipleCureTarget = null;
                    int maxCureNumberNearby = 0;
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0 || IsRecoveringTarget(cureTarget, RecoverRoundList, Round))
                        {
                            continue;
                        }

                        ArrayList cureTargetNearbyList = GetCureTargetNearbyList(cureTarget, friendly.List, (cureSkill.UseLevel + 1) * 15 * 3);
                        if (maxCureNumberNearby < cureTargetNearbyList.Count)
                        {
                            maxCureNumberNearby = cureTargetNearbyList.Count;
                            multipleCureTarget = cureTarget;
                            multipleCureTargetNearbyList = cureTargetNearbyList;
                        }
                    }

                    if (maxCureNumberNearby > 1 || Window.SkillMode)
                    {
                        if (multipleCureTarget != null)
                        {
                            if ((multipleCureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, multipleCureTarget.Addr + 0x24, BitConverter.GetBytes(multipleCureTarget.SelectState | 0x8), 4, 0);
                                multipleCureTarget.ModifySelectState = true;
                            }
                            RecordRecoverRound(multipleCureTargetNearbyList);
                            if(UseSkill(cureSkill, multipleCureTarget))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("恢復魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "恢復魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0)
                        {
                            continue;
                        }

                        bool isRecovering = IsRecoveringTarget(cureTarget, RecoverRoundList, Round);
                        if (!isRecovering && (float)cureTarget.HP / cureTarget.MaxHP <= 0.9)
                        {
                            if ((cureTarget.SelectState & 0x8) == 0)
                            {
                                WinAPI.WriteProcessMemory(hProcess, cureTarget.Addr + 0x24, BitConverter.GetBytes(cureTarget.SelectState | 0x8), 4, 0);
                                cureTarget.ModifySelectState = true;
                            }

                            if (UseSkill(cureSkill, cureTarget))
                            {
                                RecordRecoverRound(cureTarget);
                                return true;
                            }
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("潔淨魔法") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "潔淨魔法", friendly.Role.MP);
                if (cureSkill != null)
                {
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0)
                        {
                            continue;
                        }

                        if ((cureTarget.SelectState & 0x8) == 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, cureTarget.Addr + 0x24, BitConverter.GetBytes(cureTarget.SelectState | 0x8), 4, 0);
                            cureTarget.ModifySelectState = true;
                        }
                        if (UseSkill(cureSkill, cureTarget))
                        {
                            return true;
                        }
                    }
                }
            }

            if (Window.UseSkills.IndexOf("氣絕回復") != -1)
            {
                Skill cureSkill = Skill.SearchBattleSkill(hProcess, "氣絕回復", friendly.Role.MP);
                if (cureSkill != null)
                {
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP != 0)
                        {
                            continue;
                        }

                        if ((cureTarget.SelectState & 0x8) == 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, cureTarget.Addr + 0x24, BitConverter.GetBytes(cureTarget.SelectState | 0x8), 4, 0);
                            cureTarget.ModifySelectState = true;
                        }

                        if (UseSkill(cureSkill, cureTarget))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool SelfSkill(BattleGroup friendly)
        {
            int hProcess = Window.HandleProcess;

            if (Window.UseSkills.IndexOf("明鏡止水") != -1)
            {
                Skill skill = Skill.SearchBattleSkill(hProcess, "明鏡止水", friendly.Role.MP);
                if (skill != null)
                {
                    if ((float)friendly.Role.HP / friendly.Role.MaxHP <= 0.8)
                    {
                        if ((friendly.Role.SelectState & 0x8) == 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, friendly.Role.Addr + 0x24, BitConverter.GetBytes(friendly.Role.SelectState | 0x8), 4, 0);
                            friendly.Role.ModifySelectState = true;
                        }

                        return UseSkill(skill, friendly.Role);
                    }
                }
            }

            return false;
        }

        private bool CapturePet(BattleGroup enemy, BattleTarget self)
        {
            int hProcess = Window.HandleProcess;

            if (!Window.CaptureMode || enemy.LV1List.Count == 0)
            {
                return false;
            }

            BattleTarget LV1Target = (BattleTarget)enemy.LV1List[0];
            if (LV1Target != null)
            {
                Inventory inventory = Inventory.GetInventoryInfo(hProcess, true);
                Item item = inventory.FuzzySearch("封印卡");

                Skill skill = Skill.SearchBattleSkill(hProcess, "手下留情", self.MP);
                if (skill == null || item == null)
                {
                    ClickButton("防禦");
                }
                else if (PetInfo.GetPetsNumber(hProcess) == 5)
                {
                    return false;
                }
                else if (LV1Target.HP == 1)
                {
                    if (item != null)
                    {
                        ClickButton("物品");
                        if (Inventory.BattleUsingItem(Window, item))
                        {
                            Common.Delay(50);
                            SelectTarget(LV1Target);
                        }
                    }
                }
                else
                {
                    skill.UseLevel = skill.Cost.Count - 1;
                    UseSkill(skill, LV1Target);
                }
            }

            return true;
        }

        public bool AttackSkill(BattleGroup enemy, BattleTarget target, BattleTarget self)
        {
            int hProcess = Window.HandleProcess;

            if ((float)self.HP / self.MaxHP <= 0.8)
            {
                if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "吸血魔法", self.MP), target))
                {
                    return true;
                }
            }

            if (enemy.List.Count >= SkillThreshold || Window.SkillMode)
            {
                if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "亂射", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "氣功彈", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "飛刀投擲", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "追月", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "軍隊召集", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "超強隕石魔法", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "超強冰凍魔法", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "超強火焰魔法", self.MP), target))
                {
                    return true;
                }
                else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "超強風刃魔法", self.MP), target))
                {
                    return true;
                }
            }

            if (enemy.List.Count <= 2 || Window.SkillMode)
            {
                if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "崩擊", self.MP), target))
                {
                    return true;
                }
            }

            if (enemy.FrontNumber >= 2 || enemy.RearNumber >= 2 || Window.SkillMode)
            {
                if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "因果報應", self.MP), target))
                {
                    return true;
                }
            }
            
            if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "連擊", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "乾坤一擲", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "混亂攻擊", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "竊盜", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "隕石魔法", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "冰凍魔法", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "火焰魔法", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "風刃魔法", self.MP), target))
            {
                return true;
            }
            else if (UseSkill(Skill.Usable(hProcess, Window.UseSkills, "吸血魔法", self.MP), target))
            {
                return true;
            }

            return false;
        }

        /*
        public bool UseSkill(Skill skill)
        {
            if (skill == null || skill.Level == -1)
                return false;

            if (Window.ClassName.Equals("御守魔力"))
            {
                WinAPI.ReadProcessMemory(Window.HandleProcess, CGAddr.CommandAddr - 0x8C, out int state, 4, 0);
                if (state == 1)
                    return false;
            }
            else
            {
                WinAPI.ReadProcessMemory(Window.HandleProcess, CGAddr.BattleCommandAddr - 0x8, out int state, 4, 0);
                if (state == 1)
                    return false;

                Common.Delay(Window.Random.Next(1000, 2000));
            }

            if (!SelectSkill(skill.Order))
            {
                ClickButton("技能");
                SelectSkillLevel(skill.Level);
            }

            BattleGroup enemy = GetEnemyGroup(Window.HandleProcess);
            Target target = GetHumanAttackTarget(enemy);

            if (target != null)
            {
                Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.FigureName + " use " + skill.ToString() + " to " + target.ToString());
                SelectTarget(target);
            }
            return true;
        }
        */

        public bool UseSkill(Skill skill, BattleTarget target)
        {
            if (skill == null || skill.UseLevel == -1 || target == null)
                return false;

            int hProcess = Window.HandleProcess;

            if (Window.ClassName.Equals("御守魔力"))
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.CommandAddr - 0x8C, out int state, 4, 0);
                if (state == 1)
                    return false;
            }
            else
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.BattleCommandAddr - 0x8, out int state, 4, 0);
                if (state == 1)
                    return false;

                Common.Delay(new Random().Next(1000, 2000));
            }

            WinAPI.WriteProcessMemory(hProcess, CGAddr.BattleCommandAddr, BitConverter.GetBytes(0x2), 4, 0);
            //WinAPI.WriteProcessMemory(Window.HandleProcess, CGAddr.BattleCommandAddr + 0xC, BitConverter.GetBytes(0x0), 4, 0);

            //技能index
            WinAPI.WriteProcessMemory(hProcess, CGAddr.UseSkillAddr, BitConverter.GetBytes(skill.Index), 4, 0);
            //技能等級
            WinAPI.WriteProcessMemory(hProcess, CGAddr.UseSkillLevelAddr, BitConverter.GetBytes(skill.UseLevel), 4, 0);

            if (Window.RoleName.Equals(target.Name))
            {
                Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + " 使用 " + skill.ToString());
            }
            else
            {
                Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + " 對 " + target.ToString() + " 使用 " + skill.ToString());
            }
    
            target.Index = GetSelectIndexByType(target.Index, (int)skill.Type[skill.UseLevel]);

            SelectTarget(target);

            return true;
        }

        public int GetSelectIndexByType(int targetIndex, int type)
        {
            int hProcess = Window.HandleProcess;

            if (Window.ClassName.Equals("御守魔力"))
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.CommandAddr + 0x30, BitConverter.GetBytes(type), 4, 0);
            }
            else
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.CommandAddr + 0x8, BitConverter.GetBytes(type), 4, 0);
            }

            if ((type & 0x80) > 0)
            {
                targetIndex += 0x14;
            }
            if ((type & 0x100) > 0)
            {
                if (targetIndex >= 0xA)
                {
                    targetIndex = 0x29;
                }
                else
                {
                    targetIndex = 0x28;
                }
            }
            if ((type & 0x200) > 0)
            {
                targetIndex = 0x2A;
            }

            return targetIndex;
        }

        public bool PetCureSkill(PetInfo currentPet)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (!Window.PetAutoAttack)
                return false;

            BattleGroup friendly = GetFriendlyGroup(hProcess);

            foreach (PetSkill skill in currentPet.SkillList)
            {
                if (currentPet.MP < skill.Cost)
                {
                    continue;
                }

                if (skill.Name.Contains("超強補血魔法"))
                {
                    //御守寵物的超補是Lv7
                    int threshold = 7 * 25;

                    int cureNumber = 0;
                    foreach (BattleTarget cureTarget in friendly.List)
                    {
                        if (cureTarget.HP == 0 || cureTarget.MaxHP - cureTarget.HP < threshold)
                        {
                            continue;
                        }

                        cureNumber++;
                    }

                    if (cureNumber >= 2 || (skill.Cost <= 100 && cureNumber >= 1))
                    {
                        BattleTarget cureTarget = (BattleTarget)friendly.List[0];
                        if ((cureTarget.SelectState & 0x8) == 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, cureTarget.Addr + 0x24, BitConverter.GetBytes(cureTarget.SelectState | 0x8), 4, 0);
                            cureTarget.ModifySelectState = true;
                        }

                        int skillIndex = currentPet.SkillList.IndexOf(skill);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);

                        Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 對 " + cureTarget.ToString() + " 使用 " + skill.Name);

                        cureTarget.Index = GetSelectIndexByType(cureTarget.Index, skill.Type);
                        SelectTarget(cureTarget, false);
                        return true;
                    }
                }
                else if (skill.Name.Contains("明鏡止水") && (float)currentPet.HP / currentPet.MaxHP <= 0.5)
                {
                    BattleTarget target = friendly.Role;

                    if ((target.SelectState & 0x8) == 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, target.Addr + 0x24, BitConverter.GetBytes(target.SelectState | 0x8), 4, 0);
                        target.ModifySelectState = true;
                    }

                    int skillIndex = currentPet.SkillList.IndexOf(skill);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);

                    Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 使用 " + skill.Name);

                    target.Index = GetSelectIndexByType(target.Index, skill.Type);
                    SelectTarget(target, false);

                    return true;
                    /*
                    ArrayList selectList = WindowObject.SearchWindow(hProcess, 0x4B4B30);
                    if (selectList.Count > 0)
                    {
                        WindowObject windowObject = (WindowObject)selectList[currentPet.SkillList.IndexOf(skill)];
                        int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                        if (fakeClickAddr != 0)
                        {
                            WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                            Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + currentPet.Name + " use " + skill.Name);
                        }
                    }
                    else
                    {
                        ClickButton("技能");
                    }
                    return true;
                    */
                }
            }

            return false;
        }

        public bool PetSelfSkill(PetInfo currentPet)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (!Window.PetAutoAttack)
                return false;

            BattleGroup friendly = GetFriendlyGroup(hProcess);

            foreach (PetSkill skill in currentPet.SkillList)
            {
                if (skill.Name.Contains("騎士之譽"))
                {
                    if (currentPet.MP < skill.Cost)
                    {
                        continue;
                    }

                    BattleTarget target = friendly.Role;

                    if ((target.SelectState & 0x8) == 0)
                    {
                        WinAPI.WriteProcessMemory(hProcess, target.Addr + 0x24, BitConverter.GetBytes(target.SelectState | 0x8), 4, 0);
                        target.ModifySelectState = true;
                    }

                    int skillIndex = currentPet.SkillList.IndexOf(skill);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                    WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);

                    Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 使用 " + skill.Name);

                    target.Index = GetSelectIndexByType(target.Index, skill.Type);
                    SelectTarget(target, false);
                    return true;
                }
            }

            return false;
        }

        public bool PetAttackSkill(PetInfo currentPet, BattleGroup enemy, BattleTarget target)
        {
            int hProcess = Window.HandleProcess;

            if (target == null)
            {
                return false;
            }

            if ((float)currentPet.HP / currentPet.MaxHP <= 0.5)
            {
                foreach (PetSkill skill in currentPet.SkillList)
                {
                    if (skill.Name.Contains("吸血魔法"))
                    {
                        int skillIndex = currentPet.SkillList.IndexOf(skill);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);
                        Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 對 " + target.ToString() + " 使用 " + skill.Name);

                        return true;
                    }
                }
            }

            if (enemy.List.Count >= SkillThreshold)
            {
                foreach (PetSkill skill in currentPet.SkillList)
                {
                    if (currentPet.MP < skill.Cost)
                    {
                        continue;
                    }

                    if (skill.Name.Contains("超強隕石魔法") || skill.Name.Contains("超強火焰魔法") || skill.Name.Contains("超強冰凍魔法") || skill.Name.Contains("超強風刃魔法"))
                    {
                        if (currentPet.MaxMP < currentPet.MaxHP)
                        {
                            continue;
                        }

                        int skillIndex = currentPet.SkillList.IndexOf(skill);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);

                        target.Index = GetSelectIndexByType(target.Index, skill.Type);

                        Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 對 " + target.ToString() + " 使用 " + skill.Name);

                        return true;
                    }

                    if (skill.Name.Contains("氣功彈") || skill.Name.Contains("追月") || skill.Name.Contains("龍之吐息"))
                    {
                        int skillIndex = currentPet.SkillList.IndexOf(skill);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr, BitConverter.GetBytes(skillIndex), 4, 0);
                        WinAPI.WriteProcessMemory(hProcess, CGAddr.PetSkillAddr + 0x4, BitConverter.GetBytes(0x3), 4, 0);
                        Log.WriteLine(TAG, "[Round" + (Round + 1) + "] " + Window.RoleName + "(" + currentPet.Name + ") 對 " + target.ToString() + " 使用 " + skill.Name);

                        return true;
                    }
                }
            }

            return false;
        }

        private void ClickButton(string buttonName)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            int buttonCode = 0;
            switch(buttonName)
            {
                case "攻擊":
                    buttonCode = 0x3B9C4;
                    break;
                case "技能":
                    buttonCode = 0x3B9C7;
                    break;
                case "防禦":
                    buttonCode = 0x3B9D0;
                    break;
                case "物品":
                    buttonCode = 0x3B9D3;
                    break;
                case "逃跑":
                    buttonCode = 0x3B9D9;
                    break;
            }

            if (buttonCode == 0)
            {
                return;
            }

            Button button = Button.SearchButton(hProcess, buttonCode);
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

        private bool SelectSkill(int skillOrder)
        {
            int hProcess = Window.HandleProcess;

            int battleSkillOrder = Skill.GetBattleSkillOrder(hProcess, skillOrder);
            if (battleSkillOrder == -1)
            {
                return false;
            }

            //選擇技能
            ArrayList selectList = WindowObject.SearchWindow(hProcess, CGAddr.BattleSelectSkillCallAddr);
            if (selectList.Count > 0)
            {
                WindowObject windowObject = (WindowObject)selectList[battleSkillOrder];
                int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                }
                return true;
            }
            return false;
        }

        private bool SelectSkillLevel(int skillLevel)
        {
            int hProcess = Window.HandleProcess;

            //選擇技能等級
            ArrayList skillLvSelectList = WindowObject.SearchWindow(hProcess, CGAddr.BattleSelectSkillLevelCallAddr);
            if (skillLvSelectList.Count > 0)
            {
                WindowObject windowObject = (WindowObject)skillLvSelectList[skillLevel];
                int fakeClickAddr = CGCall.SelectSkill(hProcess, windowObject.CallAddr);
                if (fakeClickAddr != 0)
                {
                    WinAPI.WriteProcessMemory(hProcess, windowObject.CallAddrPtr, BitConverter.GetBytes(fakeClickAddr), 4, 0);
                }
                return true;
            }

            return false;
        }

        public BattleGroup GetFriendlyGroup(int hProcess)
        {
            BattleGroup group = new BattleGroup();
            ArrayList battleTargetList = new ArrayList();

            string roleName = Common.GetRoleName(hProcess);
            for (int targetIndex = 0; targetIndex <= 19; targetIndex++)
            {
                int targetInfoAddr = CGAddr.BattleTargetAddr + targetIndex * 4;
                BattleTarget target = BattleTarget.GetTargetInfo(hProcess, targetInfoAddr);
                if (target != null)
                {
                    target.Index = targetIndex;
                    battleTargetList.Add(target);
                    if (target.Name == roleName)
                    {
                        group.Role = target;
                        group.RoleIndex = targetIndex;
                        group.PeyIndex = targetIndex <= 4 ? targetIndex + 5 : targetIndex - 5;
                    }
                }
            }

            //E  C   A  B   D       10~14
            //13 11  F  10  12      15~19
            //9	7 5	6 8     
            //4 2 0 1 3

            if (group.RoleIndex != -1)
            {
                int friendly = group.RoleIndex / 10 == 0 ? 0 : 1;
                foreach (BattleTarget target in battleTargetList)
                {
                    if (target.Index / 10 == friendly)
                    {
                        group.List.Add(target);
                    }
                }
            }
            return group;
        }

        public BattleGroup GetEnemyGroup(int hProcess)
        {
            BattleGroup group = new BattleGroup();
            ArrayList battleTargetList = new ArrayList();

            string roleName = Common.GetRoleName(hProcess);
            for (int targetIndex = 19; targetIndex >= 0; targetIndex--)
            {
                int targetInfoAddr = CGAddr.BattleTargetAddr + targetIndex * 4;
                BattleTarget target = BattleTarget.GetTargetInfo(hProcess, targetInfoAddr);
                if (target != null)
                {
                    target.Index = targetIndex;
                    battleTargetList.Add(target);
                    if (target.Name == roleName)
                    {
                        group.RoleIndex = targetIndex;
                    }
                }
            }

            //E  C   A  B   D       10~14
            //13 11  F  10  12      15~19
            //9	7 5	6 8     
            //4 2 0 1 3

            if (group.RoleIndex != -1)
            {
                int friendly = group.RoleIndex / 10 == 0 ? 0 : 1;
                foreach (BattleTarget target in battleTargetList)
                {
                    if (target.Index / 10 != friendly)
                    {
                        if (target.Index % 10 < 5)
                        {
                            target.Shade = IsTargeShade(group.List, target);
                            group.RearNumber += 1;
                        }
                        else
                        {
                            group.FrontNumber += 1;
                        }

                        group.List.Add(target);

                        if (target.LV == 1)
                        {
                            if (target.Name.Contains("毒龍骨"))
                            {
                                continue;
                            }

                            group.LV1List.Add(target);
                        }
                    }
                }
            }

            ModifyTargetInfo(hProcess, group.List);

            group.List.Reverse();
            return group;
        }
    }
}
