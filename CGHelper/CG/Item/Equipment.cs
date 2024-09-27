using System.Linq;
using System.Text.RegularExpressions;

namespace CGHelper.CG
{
    public class Equipment : Item
    {
        //index 0-帽盔 1-衣鎧袍 23-武器盾牌 4-鞋靴 56-戒指護身符 7-水晶
        //type 0x1-斧 0x3-杖 0x4-弓 0x5-小刀 0x6-鏢 
        //type 0x7-盾 0x8-盔 0x9-帽 0xB-衣 0xC-袍 0xD-靴 0xF-鞋 0x10-樂器 0x12-戒指 0x15-護身符 0x16-水晶

        public static int GetEquipType(int hProcess, int equipIndex)
        {
            int equipAddr = CGAddr.EquipAddr + equipIndex * CGAddr.ItemsOffset;
            Item item = GetItemInfo(hProcess, equipAddr);
            if (item != null)
            {
                return item.Type;
            }

            return -1;
        }

        public static int GetLeftHandType(int hProcess)
        {
            return GetEquipType(hProcess, 2);
        }

        public static int GetRightHandType(int hProcess)
        {
            return GetEquipType(hProcess, 3);
        }

        public static bool IsWeaponBow(int hProcess)
        {
            //裝備弓
            return GetLeftHandType(hProcess) == 0x4 || GetRightHandType(hProcess) == 0x4;
        }

        public static bool IsWeaponKnife(int hProcess)
        {
            //裝備小刀
            return GetLeftHandType(hProcess) == 0x5 || GetRightHandType(hProcess) == 0x5;
        }

        public static bool IsWeaponBoomerang(int hProcess)
        {
            //裝備投擲武器
            return GetLeftHandType(hProcess) == 0x6 || GetRightHandType(hProcess) == 0x6;
        }

        public static bool NoWeapon(int hProcess)
        {
            return GetLeftHandType(hProcess) == -1 && GetRightHandType(hProcess) == 0x7
                || GetLeftHandType(hProcess) == 0x7 && GetRightHandType(hProcess) == -1
                || GetLeftHandType(hProcess) == -1 && GetRightHandType(hProcess) == -1;
        }

        public static Item GetWeapon(int hProcess)
        {
            if (GetRightHandType(hProcess) == 0x7 || GetRightHandType(hProcess) == -1)
            {
                int equipAddr = CGAddr.EquipAddr + 2 * CGAddr.ItemsOffset;
                Item item = GetItemInfo(hProcess, equipAddr);
                if (item != null)
                {
                    return item;
                }
            }
            else if (GetLeftHandType(hProcess) == 0x7 || GetLeftHandType(hProcess) == -1)
            {
                int equipAddr = CGAddr.EquipAddr + 3 * CGAddr.ItemsOffset;
                Item item = GetItemInfo(hProcess, equipAddr);
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        public static bool IsFullDurability(int hProcess, Item item)
        {
            if (item.Type >= 0x10)
            {
                //Console.WriteLine(item.Name + " type = " + item.Type);
                return true;
            }

            for (int offset = 0x30; offset < 0x210; offset += 0x60)
            {
                string detail = Common.GetNameFromAddr(hProcess, item.Addr + offset);
                if (detail == null)
                    break;

                //Console.WriteLine(detail);
                if (detail == null || !detail.Contains("耐久") || !detail.Contains('/'))
                    continue;

                string durability = Regex.Match(detail, @"\d+\/\d+").Groups[0].ToString();
                if (durability == null)
                    continue;

                string[] durabilityValue = durability.Split('/');
                int.TryParse(durabilityValue[0], out int value);
                int.TryParse(durabilityValue[1], out int maxValue);

                //Console.WriteLine(item.Name + " " + value + "/" + maxValue);
                if (value < maxValue)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsWeaponFullDurability(int hProcess)
        {
            Item item = GetWeapon(hProcess);
            if (item != null)
            {
                return IsFullDurability(hProcess, item);
            }

            return true;
        }
    }
}
