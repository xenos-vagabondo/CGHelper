using CommonLibrary;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace CGHelper.CG
{
    public class Inventory
    {
        public ArrayList ItemList = new ArrayList();
        public int EmptyNumber;

        public static Inventory GetInventoryInfo(int hProcess, bool modifyItemName = false)
        {
            Inventory inventory = new Inventory();
            for (int itemIndex = 0; itemIndex < 20; itemIndex++)
            {
                int itemAddr = CGAddr.ItemsAddr + itemIndex * CGAddr.ItemsOffset;
                Item item = Item.GetItemInfo(hProcess, itemAddr);
                if (item != null)
                {
                    item.Index = itemIndex;

                    if (modifyItemName)
                    {
                        //Item.ModifyItemName(hProcess, item);
                    }

                    inventory.ItemList.Add(item);
                } 
                else
                {
                    ++inventory.EmptyNumber; 
                }
            }

            return inventory;
        }

        public int GetItemNumber(string name)
        {
            int total = 0;

            foreach(Item item in ItemList)
            {
                if (item.Name.Equals(name))
                {
                    if (item.Number == 0)
                    {
                        total += 1;
                    } 
                    total += item.Number;
                } 
                else if (item.Name.StartsWith(name))
                {
                    Match match = Regex.Match(item.Name, "[0-9]+");
                    if (match.Success)
                    {
                        int.TryParse(match.Value, out int matchNumber);
                        total += matchNumber * item.Number;
                    } 
                    else if ("棉布".Equals(name))
                    {
                        total += item.Number;
                    }
                }
            }
            
            return total;
        }

        public bool CheckMaterials(ArrayList materialList)
        {
            if (EmptyNumber > 0)
                return true;

            foreach (Material material in materialList)
            {
                foreach (Item item in ItemList)
                {
                    if (material.Id != item.Id)
                        continue;

                    if (material.Number == item.Number)
                        return true;
                }
            }

            return false;
        }

        public Item SearchMaterial(Material material)
        {
            foreach (Item item in ItemList)
            {
                if (material.Id != item.Id || material.Number > item.Number)
                    continue;

                return item;
            }

            return null;
        }

        public Item Search(string name)
        {
            foreach (Item item in ItemList)
            {
                if (item.Name.Equals(name))
                {
                    return item;
                }
            }

            return null;
        }

        public Item FuzzySearch(string name, string[] exclusion = null)
        {
            foreach (Item item in ItemList)
            {
                if (item.Name.Contains(name))
                {
                    bool pass = false;
                    if (exclusion != null)
                    {
                        foreach (string key in exclusion)
                        {
                            if (item.Name.Contains(key))
                            {
                                pass = true;
                                break;
                            }
                        }
                    }
                    
                    if (!pass)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        public static bool UpdateItemInfo(int hProcess, Item oldItem)
        {
            if (oldItem != null)
            {
                Inventory inventory = Inventory.GetInventoryInfo(hProcess);
                foreach (Item item in inventory.ItemList)
                {
                    if (item.Index == oldItem.Index && item.Name.Equals(oldItem.Name))
                    {
                        if (item.Durability > oldItem.Durability)
                        {
                            return false;
                        }

                        oldItem.Durability = item.Durability;
                        return true;
                    }
                }
            }
            
            return false;
        }

        public static bool BattleUsingItem(GameWindow gameWindow, Item item)
        {
            IntPtr hWnd = gameWindow.HandleWindow;
            int hProcess = gameWindow.HandleProcess;

            WindowObject window = WindowObject.SearchTopWindow(hProcess, 0x4D9470);
            if (window != null)
            {
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x4, out int check1, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x8, out int check2, 4, 0);
                if (check1 == 0x1b && check2 == 0x4)
                {
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0xC, out int x, 4, 0);
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x10, out int y, 4, 0);

                    x = 23 + x + 50 * (item.Index % 5) + 24;
                    y = 48 + y + 50 * (item.Index / 5) + 24;

                    Common.MoveMouse(hProcess, new Mouse(x, y), true);

                    WinAPI.ReadProcessMemory(hProcess, CGAddr.InventorySelectItemIndexAddr + 0x8, out int index, 4, 0);
                    if (index >= 0)
                    {
                        Common.DoubleClickMouseLeftButton(hProcess, WinAPI.IsIconic(hWnd));
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool UseItem(IntPtr hWnd, Item item)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            WindowObject window = WindowObject.SearchTopWindow(hProcess, CGAddr.InventoryCallAddr);
            if (window != null)
            {
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x4, out int check1, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x8, out int check2, 4, 0);
                if (check1 == 0xE && check2 == 0x4)
                {
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0xC, out int x, 4, 0);
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x10, out int y, 4, 0);

                    x = 23 + x + 50 * (item.Index % 5) + 24;
                    y = 48 + y + 50 * (item.Index / 5) + 24;

                    Common.MoveMouse(hProcess, new Mouse(x, y), true);

                    WinAPI.ReadProcessMemory(hProcess, CGAddr.InventorySelectItemIndexAddr, out int index, 4, 0);
                    if (index >= 0)
                    {
                        Common.DoubleClickMouseLeftButton(hProcess, WinAPI.IsIconic(hWnd));
                        return true;
                    }
                }
            }
            else
            {
                Button.Inventory(hProcess);
            }

            return false;
        }

        public static bool UseItem(IntPtr hWnd, Item item, bool battle)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            if (!battle && Common.ExpWindowShow(hWnd))
            {
                return false;
            }

            int inventoryCallAddr = battle ? 0x4D9470 : CGAddr.InventoryCallAddr;

            WindowObject window = WindowObject.SearchTopWindow(hProcess, inventoryCallAddr);
            if (window != null)
            {
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x4, out int check1, 4, 0);
                WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x8, out int check2, 4, 0);
                //if (check1 == 0x1b && check2 == 0x4) //battle
                //if (check1 == 0xE && check2 == 0x4)
                {
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0xC, out int x, 4, 0);
                    WinAPI.ReadProcessMemory(hProcess, window.ParentAddr + 0x10, out int y, 4, 0);

                    x = 23 + x + 50 * (item.Index % 5) + 24;
                    y = 48 + y + 50 * (item.Index / 5) + 24;

                    Common.MoveMouse(hProcess, new Mouse(x, y), true);

                    int inventorySelectItemIndexAddrOffset = battle ? 0x8 : 0;
                    WinAPI.ReadProcessMemory(hProcess, CGAddr.InventorySelectItemIndexAddr + inventorySelectItemIndexAddrOffset, out int index, 4, 0);
                    if (index >= 0)
                    {
                        Common.DoubleClickMouseLeftButton(hProcess, WinAPI.IsIconic(hWnd));
                        return true;
                    }
                }
            }
            else
            {
                Button.Inventory(hProcess);
            }

            return false;
        }
    }
}
