using CGHelper.CG.Base;
using CommonLibrary;
using System.Collections;

namespace CGHelper.CG.Pet
{
    public class PetInfo : Attribute
    {
        public int EXP { get; set; }
        public int LVUPEXP { get; set; }
        public int Health { get; set; }
        public int BattleState { get; set; }

        public ArrayList SkillList { get; set; }

        public PetInfo()
        {
            SkillList = new ArrayList();
        }

        public static PetInfo GetPetInfo(int hProcess, int petInfoAddr, int petSkillNameOffset)
        {
            WinAPI.ReadProcessMemory(hProcess, petInfoAddr, out int petExist, 2, 0);
            if (petExist != 1)
            {
                return null;
            }

            PetInfo pet = new PetInfo();

            WinAPI.ReadProcessMemory(hProcess, petInfoAddr + 0x8, out int lv, 4, 0);
            pet.LV = lv;
            pet.HP = Common.GetXORValue(hProcess, petInfoAddr + 0xC);
            pet.MaxHP = Common.GetXORValue(hProcess, petInfoAddr + 0x1C);
            pet.MP = Common.GetXORValue(hProcess, petInfoAddr + 0x2C);
            pet.MaxMP = Common.GetXORValue(hProcess, petInfoAddr + 0x3C);
            pet.EXP = Common.GetXORValue(hProcess, petInfoAddr + 0x4C);
            pet.LVUPEXP = Common.GetXORValue(hProcess, petInfoAddr + 0x5C);
            WinAPI.ReadProcessMemory(hProcess, petInfoAddr + 0xD4, out int health, 4, 0);
            pet.Health = health;

            WinAPI.ReadProcessMemory(hProcess, petInfoAddr + 0x6A4, out int petSkillNumber, 1, 0);
            for (int petSkillIndex = 0; petSkillIndex < petSkillNumber; petSkillIndex++)
            {
                int petSkillInfoAddr = petInfoAddr + 0xD8 + petSkillIndex * 0x8C;
                WinAPI.ReadProcessMemory(hProcess, petSkillInfoAddr, out int petSkillExist, 2, 0);
                if (petSkillExist == 1)
                {
                    PetSkill skill = new PetSkill();

                    skill.Name = Common.GetStringFromAddr(hProcess, petSkillInfoAddr + petSkillNameOffset);
                    WinAPI.ReadProcessMemory(hProcess, petSkillInfoAddr + 0x6, out int type, 2, 0);
                    skill.Type = type;
                    WinAPI.ReadProcessMemory(hProcess, petSkillInfoAddr + 0x84, out int cost, 4, 0);
                    skill.Cost = cost;
                    pet.SkillList.Add(skill);

                    //Console.WriteLine("0x" + petSkillInfoAddr.ToString("X") + " " + skill.Name + " " + skill.Cost + " 0x" + skill.Type.ToString("X"));
                }
            }
            WinAPI.ReadProcessMemory(hProcess, petInfoAddr + 0x6AA, out int battleState, 2, 0);
            pet.BattleState = battleState;
            pet.Name = Common.GetStringFromAddr(hProcess, petInfoAddr + 0x6AC);

            return pet;
        }

        public static ArrayList GetAllPetsInfo(int hProcess)
        {
            //初心寵物技能13格 水藍寵物技能10格

            ArrayList petList = new ArrayList();
            for (int petIndex = 0; petIndex < 5; petIndex++)
            {
                int petInfoAddr = CGAddr.PetInfoAddr + petIndex * CGAddr.PetInfoOffset;
                WinAPI.ReadProcessMemory(hProcess, petInfoAddr, out int petExist, 2, 0);
                if (petExist == 1)
                {
                    int petSkillNameOffset = 0x6;
                    //御守
                    if (CGAddr.PetInfoOffset < 0x1000)
                    {
                        petSkillNameOffset = 0x8;
                    }

                    PetInfo pet = GetPetInfo(hProcess, petInfoAddr, petSkillNameOffset);
                    if (pet != null)
                    {
                        petList.Add(pet);
                    }
                }
            }

            return petList;
        }

        public static int GetPetsNumber(int hProcess)
        {
            int number = 0;
            for (int petIndex = 0; petIndex < 5; petIndex++)
            {
                int petInfoAddr = CGAddr.PetInfoAddr + petIndex * CGAddr.PetInfoOffset;
                WinAPI.ReadProcessMemory(hProcess, petInfoAddr, out int petExist, 2, 0);
                if (petExist == 1)
                {
                    ++number;
                }
            }

            return number;
        }

        public override string ToString()
        {
            return Health + " Lv" + LV + " " + Name + " " + HP + "/" + MaxHP + " " + MP + "/" + MaxMP + " EXP:" + EXP + "/" + LVUPEXP + " " + BattleState;
        }
    }
}
