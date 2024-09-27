using CGHelper.CG.Enum;
using CommonLibrary;
using GvoHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CGHelper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ArrayList GameWindows { get; set; } = new ArrayList();
        private string Settings { get; set; } = "御守";

        private string SettingDirectory { get; set; } = Environment.CurrentDirectory + "\\SET\\";

        private void RemoveClosedWindow()
        {
            ArrayList closedWindows = new ArrayList();
            foreach (CG.GameWindow window in GameWindows)
            {
                if (WinAPI.GetProcess(window.HandleWindow) != 0)
                {
                    continue;
                }

                window.Dispose();
                closedWindows.Add(window);
            }

            foreach (CG.GameWindow window in closedWindows)
            {
                GameWindows.Remove(window);
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (!(sender is ComboBox comboBox))
                return;

            if (comboBox.SelectedItem != null)
            {
                string selectedItem = (string)comboBox.SelectedItem;
                comboBox.Items.Clear();
                comboBox.Items.Add(selectedItem);
                comboBox.SelectedItem = selectedItem;
            } 
            else
            {
                comboBox.Items.Clear();
            }

            string className = null;
            if (Settings.Equals("初心"))
            {
                className = "魔力寶貝";
                CG.Common.GB2312 = false;
            }
            else if (Settings.Equals("水藍"))
            {
                className = "Blue";
                CG.Common.GB2312 = false;
            }
            else if (Settings.Equals("御守"))
            {
                className = "御守魔力";
                CG.Common.GB2312 = true;
            }

            foreach (DictionaryEntry entry in new EnumWindowsProc().SearchForWindow(className, null))
            {
                bool same = false;
                foreach (CG.GameWindow window in GameWindows)
                {
                    if (window.HandleWindow == (IntPtr)entry.Key)
                    {
                        same = true;
                        break;
                    }
                }

                if (!same)
                {
                    GameWindows.Add(new CG.GameWindow((IntPtr)entry.Key, (string)entry.Value));
                }
            }            

            RemoveClosedWindow();

            foreach (CG.GameWindow window in GameWindows)
            {
                int hProcess = WinAPI.GetProcess(window.HandleWindow);

                string roleName = CG.Common.GetRoleName(hProcess);
                if (!string.IsNullOrEmpty(roleName))
                {
                    if (window.WorkTask != null)
                    {
                        if (!window.RoleName.Equals(comboBox.SelectedItem))
                        {
                            comboBox.Items.Remove(window.RoleName);
                        }
                        continue;
                    }

                    if (comboBox.Items.IndexOf(roleName) == -1)
                    {
                        comboBox.Items.Add(roleName);
                    }
                }

                WinAPI.CloseHandle(hProcess);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox comboBox) || comboBox.SelectedItem == null)
                return;

            if (!(comboBox.Parent is Grid grid))
                return;

            if (!(grid.Parent is TabItem tabItem))
                return;

            Console.WriteLine("Text = " + comboBox.Text
                + " SelectedIndex = " + comboBox.SelectedIndex
                + " SelectedItem = " + comboBox.SelectedItem
                + " SelectedValue = " + comboBox.SelectedValue);

            foreach (CG.GameWindow window in GameWindows)
            {
                if (window.UIGrid == grid)
                {
                    if (!window.RoleName.Equals(comboBox.SelectedItem))
                    {
                        CG.UsingGameWindow.GameWindows.Remove(window);
                        window.Dispose();
                        continue;
                    }

                    UpdateSkills(window);
                }

                if (window.UIGrid == null)
                {
                    int hProcess = WinAPI.GetProcess(window.HandleWindow);

                    string roleName = CG.Common.GetRoleName(hProcess);
                    if (!string.IsNullOrEmpty(roleName) && roleName.Equals(comboBox.SelectedItem))
                    {
                        window.UIGrid = grid;
                        window.TabItem = tabItem;
                        window.TabItem.Header = roleName;
                        window.RoleName = roleName;

                        window.Start();

                        UpdateSkills(window);
                        CG.UsingGameWindow.GameWindows.Add(window);
                    }

                    WinAPI.CloseHandle(hProcess);
                }
            }
        }

        private void UpdateSkills(CG.GameWindow window)
        {
            int hProcess = WinAPI.GetProcess(window.HandleWindow);

            foreach (UIElement child in window.UIGrid.Children)
            {
                if (child is ListBox listBox)
                {
                    if (!Directory.Exists(SettingDirectory))
                    {
                        Directory.CreateDirectory(SettingDirectory);
                    }
                    using (CINI ini = new CINI(Path.Combine(SettingDirectory, window.RoleName)))
                    {
                        string saveSkills = ini.getKeyValue("已勾選", "技能");
                        if (!string.IsNullOrWhiteSpace(saveSkills))
                        {
                            while (saveSkills.IndexOf(",") > 0)
                            {
                                window.UseSkills.Add(saveSkills.Substring(0, saveSkills.IndexOf(",")));
                                saveSkills = saveSkills.Substring(saveSkills.IndexOf(",") + 1);
                            }
                            window.UseSkills.Add(saveSkills);
                        }
                    }

                    listBox.Items.Clear();
                    foreach (CG.Skill skill in CG.Skill.GetSkillInfo(hProcess))
                    {
                        if (!skill.Handle)
                        {
                            continue;
                        }

                        CheckBox checkBox = new CheckBox();
                        checkBox.Click += UseSkillsClick;
                        checkBox.Content = skill.Name;
                        if (window.UseSkills.IndexOf(skill.Name) != -1)
                        {
                            checkBox.IsChecked = true;
                        }

                        listBox.Items.Add(checkBox);
                    }
                    break;
                }
                else if (child is CheckBox checkBox)
                {
                    ModeOption(checkBox, window);
                }
            }

            WinAPI.CloseHandle(hProcess);
        }

        private void UseSkillsClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            if (!(checkBox.Parent is ListBox listBox))
                return;

            if (!(listBox.Parent is Grid grid))
                return;

            if (!(grid.Parent is TabItem tabItem))
                return;

            foreach (CG.GameWindow window in GameWindows)
            {
                if (window.TabItem != tabItem)
                    continue;

                if ((bool)checkBox.IsChecked)
                {
                    if (window.UseSkills.IndexOf(checkBox.Content) == -1)
                    {
                        window.UseSkills.Add(checkBox.Content);
                    }
                }
                else
                {
                    window.UseSkills.Remove(checkBox.Content);
                }

                if (!Directory.Exists(SettingDirectory))
                {
                    Directory.CreateDirectory(SettingDirectory);
                }
                using (CINI ini = new CINI(Path.Combine(SettingDirectory, window.RoleName)))
                {
                    if (window.UseSkills.Count == 0)
                    {
                        ini.setKeyValue("已勾選", "技能", null);
                    }
                    else
                    {
                        string saveSkills = null;
                        foreach (string skill in window.UseSkills)
                            saveSkills += skill + ",";
                        if (!string.IsNullOrWhiteSpace(saveSkills))
                        {
                            ini.setKeyValue("已勾選", "技能", saveSkills.Substring(0, saveSkills.Length - 1));
                        }
                    }
                }
            }
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            if (!(checkBox.Parent is Grid grid))
                return;

            if (!(grid.Parent is TabItem tabItem))
                return;

            foreach (CG.GameWindow window in GameWindows)
            {
                if (window.TabItem == tabItem)
                {
                    ModeOption(checkBox, window, true);
                    break;
                }
            }
        }


        private void ModeOption(CheckBox checkBox, CG.GameWindow window, bool set = false)
        {
            if (!Directory.Exists(SettingDirectory))
            {
                Directory.CreateDirectory(SettingDirectory);
            }
            using (CINI ini = new CINI(Path.Combine(SettingDirectory, window.RoleName)))
            {
                if (set)
                {
                    if (checkBox.Content.ToString().Contains("樓") ||
                        checkBox.Content.ToString().Contains("魔香") ||
                        checkBox.Content.ToString().Equals("繞圈"))
                    {
                        ini.setKeyValue("模式", checkBox.Content.ToString(), null);
                    } 
                    else
                    {
                        ini.setKeyValue("模式", checkBox.Content.ToString(), (bool)checkBox.IsChecked ? "true" : null);
                    }
                }
                else
                {
                    checkBox.IsChecked = !string.IsNullOrEmpty(ini.getKeyValue("模式", checkBox.Content.ToString()));
                }
            }

            switch (checkBox.Content)
            {
                case "自動攻擊":
                    window.AutoAttack = (bool)checkBox.IsChecked;
                    break;

                case "寵物自動":
                    window.PetAutoAttack = (bool)checkBox.IsChecked;
                    break;

                case "逃跑":
                    window.AutoFlee = (bool)checkBox.IsChecked;
                    break;

                case "練技模式":
                    window.SkillMode = (bool)checkBox.IsChecked;
                    break;

                case "自動生產":
                    window.AutoProduce = (bool)checkBox.IsChecked;
                    break;

                case "修武防模式":
                    window.FixMode = (bool)checkBox.IsChecked;
                    break;

                case "省電模式":
                    window.PowerSavingMode = (bool)checkBox.IsChecked;
                    if (set && !window.PowerSavingMode)
                    {
                        CG.Common.SkipFrame(window.HandleProcess, false);
                    }
                    break;

                case "抓寵模式":
                    window.CaptureMode = (bool)checkBox.IsChecked;
                    break;

                case "自動換寵":
                    window.AutoChangePet = (bool)checkBox.IsChecked;
                    break;

                case "使用料理":
                    window.AutoUseCuisines = (bool)checkBox.IsChecked;
                    break;

                case "誘魔香":
                    window.ItemLure = (bool)checkBox.IsChecked;
                    if (window.ItemLure)
                    {
                        window.LocationItemLure = CG.Location.GetLocation(window.HandleProcess);
                    } 
                    else
                    {
                        window.UsingItemLure = null;
                    }
                    break;

                case "驅魔香":
                    window.ItemAntiLure = (bool)checkBox.IsChecked;
                    break;

                case "繞圈":
                    window.MoveManager.SetCircleMoveMode((bool)checkBox.IsChecked);
                    break;

                case "上樓":
                    window.MoveManager.SetMazeMode((bool)checkBox.IsChecked, MapType.STAIR_UP);
                    break;

                case "下樓":
                    window.MoveManager.SetMazeMode((bool)checkBox.IsChecked, MapType.STAIR_DOWN);
                    break;
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.Parent is Grid grid))
                return;

            foreach (CG.GameWindow window in GameWindows)
            {
                if (window.UIGrid != grid)
                {
                    continue;
                }

                if (button.Content.Equals("隨機移動"))
                {
                    button.Content = "停止隨機移動";
                    window.MoveManager.Start();
                    foreach (UIElement child in grid.Children)
                    {
                        if (!(child is Label label))
                        {
                            continue;
                        }

                        label.Content = window.MoveManager.GetRecordLocation();
                        break;
                    }
                }
                else
                {
                    button.Content = "隨機移動";
                    window.MoveManager.Stop();
                }
                break;
            }
        }

        private void ItemList_Add(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.Parent is Grid grid))
                return;

            string name = null;
            int number = 0;

            foreach (UIElement child in grid.Children)
            {
                if (child is ComboBox comboBox)
                {
                    if (comboBox.SelectedItem != null)
                    {
                        name = comboBox.SelectedValue.ToString();
                    }
                }
                else if (child is TextBox textBox)
                {
                    int.TryParse(textBox.Text, out number);
                }
            }

            if (!string.IsNullOrEmpty(name) && number > 0)
            {
                if (CG.ProduceController.ShoppingCart.ContainsKey(name))
                {
                    CG.ProduceController.ShoppingCart[name] = number;
                }
                else
                {
                    CG.ProduceController.ShoppingCart.Add(name, number);
                }

                DependencyObject dependencyObject = UIChildFinder.FindChild<object>(grid, "ShoppingCartListBox");
                if (dependencyObject is ListBox listBox)
                {
                    listBox.Items.Clear();
                    foreach (KeyValuePair<string, int> item in CG.ProduceController.ShoppingCart)
                    {
                        listBox.Items.Add(item);
                    }
                }
            }
        }

        private void ItemList_Delete(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
            {
                return;
            }

            if (!(button.Parent is Grid grid))
            {
                return;
            }

            DependencyObject dependencyObject = UIChildFinder.FindChild<object>(grid, "ShoppingCartListBox");
            if (dependencyObject is ListBox listBox)
            {
                if (listBox.SelectedItem == null)
                {
                    return;
                }

                KeyValuePair<string, int> item = (KeyValuePair<string, int>)listBox.SelectedItem;
                CG.ProduceController.ShoppingCart.Remove(item.Key);

                listBox.Items.Remove(listBox.SelectedItem);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            if (!(menuItem.Parent is MenuItem Settings))
                return;

            foreach(MenuItem item in Settings.Items)
            {
                if (menuItem.Equals(item))
                {
                    continue;
                }

                item.IsChecked = false;
            }
        }

        private void Settings_Checked(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            if (!menuItem.IsChecked)
            {
                return;
            }

            foreach (CG.GameWindow window in GameWindows)
            {
                window.Dispose();
            }

            GameWindows = new ArrayList();
            Settings = (string)menuItem.Header;
        }


        private void Map_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            GameMapWindow mapWindow = new GameMapWindow(GameWindows);
            mapWindow.Owner = this;
            mapWindow.Show();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!(sender is Slider slider))
                return;

            if (!(slider.Parent is Grid grid))
                return;

            foreach (CG.GameWindow window in GameWindows)
            {
                if (window.UIGrid != grid)
                {
                    continue;
                }

                window.BattleController.SkillThreshold = (int)slider.Value;
                break;
            }
        }
    }
}
