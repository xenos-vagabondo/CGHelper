using CommonLibrary;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CGHelper.CG
{
    public class Item
    {
        public int Addr { get; set; }
        public string Name { get; set; }
        public int Level { get; set; } = -1;
        public int Icon { get; set; }
        public int Id { get; set; } = -1;
        public int Number { get; set; }
        public int Type { get; set; } = -1;
        public int Index { get; set; } = -1;
        public int Appraisal { get; set; }

        public int Durability { get; set; }
        public int MaxDurability { get; set; }

        public static bool IsEmpty(int hProcess, int itemAddr)
        {
            WinAPI.ReadProcessMemory(hProcess, itemAddr, out int notEmpty, 1, 0);
            if (notEmpty != 0)
            {
                return false;
            }
            return true;
        }

        public static void GetDurability(int hProcess, Item item)
        {
            for (int offset = 0x30; offset < 0x210; offset += 0x60)
            {
                string detail = Common.GetNameFromAddr(hProcess, item.Addr + offset);
                if (detail == null)
                    break;

                if (detail == null || !detail.Contains("耐久") || !detail.Contains('/'))
                    continue;

                string durability = Regex.Match(detail, @"\d+\/\d+").Groups[0].ToString();
                if (durability == null)
                    continue;

                string[] durabilityValue = durability.Split('/');
                int.TryParse(durabilityValue[0], out int value);
                int.TryParse(durabilityValue[1], out int maxValue);
                //Console.WriteLine(item.Name + " " + value + "/" + maxValue);
                item.Durability = value;
                item.MaxDurability = maxValue;
            }
        }

        public static Item GetItemInfo(int hProcess, int itemAddr)
        {
            if (!IsEmpty(hProcess, itemAddr))
            {
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x24, out int icon, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x20, out int level, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x1C, out int id, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x18, out int number, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x14, out int type, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, itemAddr + CGAddr.ItemsOffset - 0x8, out int appraisal, 4, 0);

                Item item = new Item
                {
                    Addr = itemAddr,
                    Icon = icon,
                    Level = level,
                    Id = id,
                    Number = number,
                    Type = type,
                    Appraisal = appraisal
                };

                item.Name = Common.GetNameFromAddr(hProcess, item.Addr + 0x2);
                GetDurability(hProcess, item);

                return item;
            }

            return null;
        }

        public static void ModifyItemName(int hProcess, Item item)
        {
            //0x4655 魔石 12G 綠
            //0x4659 魔石 150G 綠
            //0x465A 魔石 178G

            //0x466A 魔石 12G 藍
            //0x466D 魔石 124G 藍
            //0x466F 魔石 178G

            //0x467F 魔石 12G 紅
            //0x4684 魔石 178G 紅

            //0x4694 魔石 12G 黃
            //0x4695 魔石 48G 黃
            //0x4697 魔石 124G 黃
            //0x4698 魔石 150G 黃
            //0x4699 魔石 178G

            if (item.Id != 0 && item.Id >= 0x4655 && item.Id < 0x46A9)
            {
                if (!item.Name.Contains("G"))
                {
                    //0x4655 魔石 12G 綠
                    //0x466A 魔石 12G 藍
                    //0x467F 魔石 12G 紅
                    //0x4694 魔石 12G 黃
                    string[] prices = new string[] { "12", "48", "96", "124", "150", "178", "205", "232", "259", "287", "313",
                            "341", "368", "395", "422", "15?", "16?", "17?", "18?", "19?", "20?", "21?"};

                    string reName = item.Name + "-" + prices[(item.Id - 0x4655) % 0x15] + "G";
                    byte[] bytes = Encoding.Default.GetBytes(reName);
                    WinAPI.WriteProcessMemory(hProcess, item.Addr + 0x2, bytes, bytes.Length + 1, 0);
                }
            }

            if (item.Appraisal == 1)
            {
                if (!item.Name.Contains("Lv"))
                {
                    //134456(0x24708)
                    //134477(0x2471D) => 422
                    //591800(0x94188) => 215
                    string reName = item.Name + "-" + "Lv" + item.Level;
                    if (item.Type == 0x29)
                    {
                        reName += " index " + (item.Id - 0x39D0);
                    }
                    //寶石
                    if (item.Type == 0x26)
                    {
                        reName += " 0x" + item.Id.ToString("X");
                    }
                    byte[] bytes = Encoding.Default.GetBytes(reName);
                    WinAPI.WriteProcessMemory(hProcess, item.Addr + 0x2, bytes, bytes.Length + 1, 0);
                }
            }

            //0x3527 冒險之星lv8
            //0x3539 騎士寶石lv6
            //0x93FFA 深藍寶石 lv3
            //0x94034 菫青石的碎片 lv1
            //0x9403F 珍珠 lv2
            //0x94049 破損的很嚴重的石英 lv2
            //0x9404A 破破的石英 lv3
            //0x94071 破破的蛋白石 lv3


            //39D0
            //0x39E6 殭屍的卡片 lv2 卡片？
            //0x3A00 迷你蝙蝠的卡片 lv1 卡片？
            //0x3A60 火焰哥布林的卡片
            //0x3A62 巨人的卡片
        }
    }
}
