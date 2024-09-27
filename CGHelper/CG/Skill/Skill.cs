using CommonLibrary;
using System.Collections;

namespace CGHelper.CG
{
    public class Skill
    {
        public string Name { get; set; }
        public ArrayList Cost { get; set; } = new ArrayList();
        public ArrayList Type { get; set; } = new ArrayList();
        public int Index { get; set; }
        public int Order { get; set; }
        public int Level { get; set; }
        public int UseLevel { get; set; } = 0;
        public bool Handle { get; set; }

        public static string[] HandleSkills { get; set; } = new string[] { "亂射", "氣功彈", "飛刀投擲", "連擊", "崩擊", "因果報應", "乾坤一擲", "補血魔法", "強力補血魔法", "超強補血魔法", "恢復魔法", "強力恢復魔法", "超強恢復魔法", "潔淨魔法", "氣絕回復", "隕石魔法", "冰凍魔法", "火焰魔法", "風刃魔法", "超強隕石魔法", "超強冰凍魔法", "超強火焰魔法", "超強風刃魔法", "治療", "急救", "竊盜", "明鏡止水", "吸血魔法", "追月", "軍隊召集", "精靈的盟約", "混亂攻擊" };
        public static ArrayList GetSkillInfo(int hProcess, int mp = -1)
        {
            ArrayList handleSkills = new ArrayList(HandleSkills);
            ArrayList skillOrderList = GetSkillOrderList(hProcess);
            ArrayList skillList = new ArrayList();

            if (mp == -1)
            {
                mp = Common.GetXORValue(hProcess, CGAddr.MPAddr);
            }

            for (int skillIndex = 0; skillIndex < 15; skillIndex++)
            {
                int skillAddr = CGAddr.SkillAddr + skillIndex * CGAddr.SkillOffset;
                string skillName = Common.GetNameFromAddr(hProcess, skillAddr);
                if (skillName == null)
                    continue;

                Skill skill = new Skill();
                skill.Name = skillName;
                WinAPI.ReadProcessMemory(hProcess, skillAddr + 0x1C, out int level, 4, 0);
                skill.Level = level;
                //Console.WriteLine(skillName + " max level = " + skill.Level);

                if (!string.IsNullOrEmpty(skill.Name) && skill.Name.Equals("超強補血魔法"))
                {
                    //Console.WriteLine("skillAddr = 0x" + skillAddr.ToString("X"));
                }

                for (int skillLevel = 0; skillLevel < skill.Level; skillLevel++)
                {
                    int skillLevelAddr = skillAddr + 0x3C + skillLevel * 0x94;
                    WinAPI.ReadProcessMemory(hProcess, skillLevelAddr + 0x8C, out int use, 4, 0);
                    if (use != 1)
                    {
                        break;
                    }

                    WinAPI.ReadProcessMemory(hProcess, skillLevelAddr + 0x7C, out int cost, 4, 0);
                    if (mp < cost)
                    {
                        break;
                    }

                    skill.Cost.Add(cost);
                    WinAPI.ReadProcessMemory(hProcess, skillLevelAddr + 0x84, out int type, 4, 0);
                    skill.Type.Add(type);
                    //Console.WriteLine(skillName + "Lv" + (skillLevel + 1) + " cost " + cost + " 0x" + type.ToString("X"));
                }

                skill.Index = skillIndex;
                if (handleSkills.IndexOf(skill.Name) != -1)
                {
                    skill.Handle = true;
                    skill.UseLevel = skill.Cost.Count - 1;
                }
                skill.Order = skillOrderList.IndexOf(skill.Index);
                //Console.WriteLine(skillName + " 0x" + skillAddr.ToString("X") + " Index = " + skillIndex + " Order = " + skill.Order);

                skillList.Add(skill);
            }
            //Console.WriteLine("====");

            return skillList;
        }

        private static ArrayList GetSkillOrderList(int hProcess)
        {
            ArrayList orderList = new ArrayList();
            for (int order = 0; order < 15; order++)
            {
                int orderAddr = CGAddr.SkillOrderAddr + 0x8 * order;
                WinAPI.ReadProcessMemory(hProcess, orderAddr, out int skillIndex, 1, 0);
                orderList.Add(skillIndex);
            }
            return orderList;
        }

        public static int GetBattleSkillOrder(int hProcess, int skillOrder)
        {
            for (int index = 0; index < 15; index++)
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.BattleSkillOrderAddr + 0x11 + index, out int order, 1, 0);
                if (skillOrder == order)
                {
                    return index;
                }
            }

            return -1;
        }

        public static Skill SearchSkill(int hProcess, string name)
        {
            ArrayList skillList = GetSkillInfo(hProcess);

            foreach (Skill skill in skillList)
            {
                if (skill.Name.Equals(name))
                {
                    return skill;
                }
            }
            return null;
        }

        public static Skill SearchBattleSkill(int hProcess, string name, int mp)
        {
            ArrayList skillList = GetSkillInfo(hProcess, mp);

            foreach (Skill skill in skillList)
            {
                if (skill.Name.Equals(name))
                {
                    return skill;
                }
            }
            return null;
        }

        public static Skill Usable(int hProcess, ArrayList skills, string skill, int mp)
        {
            if (skills.IndexOf(skill) != -1)
            {
                Skill attackSkill = SearchBattleSkill(hProcess, skill, mp);
                if (attackSkill != null) {

                    switch(skill)
                    {
                        case "亂射":
                            if (!Equipment.IsWeaponBow(hProcess))
                            {
                                return null;
                            }
                            break;
                        case "氣功彈":
                        case "混亂攻擊":
                            if (!Equipment.NoWeapon(hProcess))
                            {
                                return null;
                            }
                            break;
                        case "乾坤一擲":
                            if (Equipment.IsWeaponBoomerang(hProcess) || Equipment.IsWeaponKnife(hProcess))
                            {
                                return null;
                            }
                            break;
                        case "軍隊召集":
                        case "連擊":
                        case "崩擊":
                        case "追月":
                            if (Equipment.IsWeaponBoomerang(hProcess) || Equipment.IsWeaponKnife(hProcess) || Equipment.IsWeaponBow(hProcess))
                            {
                                return null;
                            }   
                            break;
                        case "因果報應":
                            if (!Equipment.IsWeaponBoomerang(hProcess))
                            {
                                return null;
                            }
                            break;
                        case "飛刀投擲":
                            if (!Equipment.IsWeaponKnife(hProcess))
                            {
                                return null;
                            }
                            break;
                    }

                    return attackSkill;
                }
            }

            return null;
        }

        public string ToString()
        {
            return Name + "Lv" + (UseLevel + 1) + "(" + Cost[UseLevel] + ")";
        }
    }
}
