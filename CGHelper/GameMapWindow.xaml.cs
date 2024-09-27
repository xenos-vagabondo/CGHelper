using CGHelper.CG;
using CGHelper.CG.Enum;
using CommonLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CGHelper
{
    /// <summary>
    /// GameMapWindow.xaml 的互動邏輯
    /// </summary>
    public partial class GameMapWindow : Window
    {
        ArrayList GameWindows { get; set; }

        int WindowMinSize { get; set; } = 320;
        int ReflashTime { get; set; } = 100;

        bool DisplayNPC { get; set; } = true;

        bool DisplayTreasureChest { get; set; } = true;

        bool AutoSaveMapImage { get; set; } = false;

        private CancellationTokenSource CTS { get; set; }
        public Task WorkTask { get; set; }

        Task LoadBinTask { get; set; }

        ArrayList ActiveObjectList { get; set; }

        Location CurrentLocation { get; set; }
        Location LastLocation { get; set; }

        Map LastMap { get; set; }

        byte[] LastPixels { get; set; }

        public enum MAPS_STRUCT
        {
            FOG,
            BUILDING,
            WALKABLE,
            TRANSPORT,
            STAIR_UP,
            STAIR_DOWN,
        }

        public GameMapWindow()
        {
            InitializeComponent();
        }

        public GameMapWindow(ArrayList gameWindows)
        {
            GameWindows = gameWindows;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowList();
        }

        private void Reflash_Time_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            if (!(menuItem.Parent is MenuItem parent))
                return;

            if (int.TryParse(menuItem.Header.ToString().Replace("ms", ""), out int value))
            {
                foreach (MenuItem mi in parent.Items)
                {
                    if (mi != menuItem)
                    {
                        mi.IsChecked = false;
                    }
                }

                ReflashTime = value;
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            switch (menuItem.Header)
            {
                case "NPC":
                    DisplayNPC = menuItem.IsChecked;
                    break;
                case "寶箱":
                    DisplayTreasureChest = menuItem.IsChecked;
                    break;
                case "儲存地圖":
                    AutoSaveMapImage = menuItem.IsChecked;
                    break;
            }
        }

        private void WindowList()
        {
            WindowListMenuItem.Items.Clear();
            foreach (GameWindow window in GameWindows)
            {
                if (string.IsNullOrEmpty(window.RoleName))
                {
                    continue;
                }

                MenuItem subItem = new MenuItem();
                subItem.Header = window.RoleName;
                subItem.Tag = window.HandleWindow;
                subItem.Click += new RoutedEventHandler(Select_Window_Click);
                WindowListMenuItem.Items.Add(subItem);
            }
        }

        private void Select_Window_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            IntPtr hWnd = (IntPtr)menuItem.Tag;

            if (WorkTask != null)
            {
                CTS.Cancel();
                LastMap = null;
            }
            else
            {
                MapNameTextBlock.Text = "讀取地圖檔...需要幾秒鐘(づ￣ ³￣)づ";
                Cursor = Cursors.Wait;

                LoadBinTask = new Task(() =>
                {
                    Map.GetMapInfoFromBinFiles(GetExeFilePath(hWnd));
                });
                LoadBinTask.Start();
            }

            CTS = new CancellationTokenSource();
            WorkTask = new Task(() => {
                Watcher(hWnd);
            } , CTS.Token);
            WorkTask.Start();
        }

        void Watcher(IntPtr hWnd)
        {
            LoadBinTask.Wait();
            Dispatcher.Invoke(new Action(() =>
            {
                Cursor = Cursors.Arrow;
            }));

            while (!CTS.Token.IsCancellationRequested)
            {
                int hProcess = WinAPI.GetProcess(hWnd);
                if (hProcess == 0)
                {
                    Dispatcher.Invoke(new Action(() => { Close(); }));
                }

                CurrentLocation = Location.GetLocation(hProcess);

                Map map = Map.GetMap(hProcess);
                if (map != null && !map.IsSame(LastMap))
                {
                    if (AutoSaveMapImage)
                    {
                        SaveMapImage(hWnd, map);
                    }

                    LastMap = map;

                    ReSizeWindow(map);
                }

                if (DisplayNPC || DisplayTreasureChest)
                {
                    ActiveObjectList = ActiveObject.GetObject(hProcess);
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    BitmapSource bitmap = CreateMapBitmap(hWnd, map, true);
                    if (bitmap != null)
                    {
                        MapNameTextBlock.Text = CurrentLocation.Name + "(" + map.Code + ")";
                        MapImage.Source = bitmap;
                    }
                }));

                LastLocation = CurrentLocation;
                Common.Delay(ReflashTime);
            }
        }

        void SaveMapImage(IntPtr hWnd, Map map)
        {
            if (LastMap == null || LastMap.Code == map.Code || !LastMap.RandomMap)
            {
                return;
            }

            BitmapSource bitmap = CreateMapBitmap(hWnd, LastMap, false);
            string saveDirectory = Environment.CurrentDirectory + "\\MAP\\";
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            string savePath = saveDirectory + LastMap.Code + ".png";

            int width = LastMap.Size.Width;
            int height = LastMap.Size.Height;
            int min = Math.Min(width, height);
            if (min < WindowMinSize)
            {
                float n = (float)WindowMinSize / min;
                width = (int)(width * n);
                height = (int)(height * n);
            }
            //BitmapSaver.SaveImageToFile(bitmap, width, height, savePath, LastLocation.Name);
        }

        private string GetExeFilePath(IntPtr hWnd)
        {
            int hProcess = WinAPI.GetProcess(hWnd);

            string path = Common.GetStringFromAddr(hProcess, CGAddr.GamePathAddr, false);
            if (!string.IsNullOrEmpty(path))
            {
                return path.Replace("omamori.exe", "").Replace("bluecg.exe", "");
            }

            return null;
        }

        void ReSizeWindow(Map map)
        {
            int width = map.Size.Width;
            int height = map.Size.Height;
            int min = Math.Min(width, height);
            if (min < WindowMinSize)
            {
                float n = (float)WindowMinSize / min;
                width = (int)(width * n);
                height = (int)(height * n);

                //Console.WriteLine(map.Size + " n = " + n);
            }

            Dispatcher.Invoke(new Action(() =>
            {
                int minWindow = Math.Min((int)Application.Current.MainWindow.Width, (int)Application.Current.MainWindow.Height);
                if (minWindow < Math.Min(width, height))
                {
                    //Application.Current.MainWindow.Width = width;
                    //Application.Current.MainWindow.Height = height;
                }
            }));

        }

        BitmapSource CreateMapBitmap(IntPtr hWnd, Map map, bool drawSelf)
        {
            int width = map.Size.Width;
            int height = map.Size.Height;

            if (width == 0 || height == 0)
            {
                return null;
            }

            int stride = 4 * width;
            byte[] pixels = new byte[stride * height];

            void SetPixel(int x, int y, Color color)
            {
                int i = x * 4 + y * stride;
                if (i + 3 < pixels.Length)
                {
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                    pixels[i + 3] = color.A;
                }
            }

            for (int x = 0; x < map.Size.Width; x++)
            {
                for (int y = 0; y < map.Size.Height; y++)
                {
                    switch (map.Data[x, y])
                    {
                        case (int)MapType.FOG:
                            SetPixel(x, y, Color.Black);
                            break;
                        case (int)MapType.IMMOBILE:
                            SetPixel(x, y, Color.DimGray);
                            break;
                        case (int)MapType.WALKABLE:
                            SetPixel(x, y, Color.White);
                            break;
                        case (int)MapType.STAIR_UP:
                            SetPixel(x, y, Color.Red);
                            break;
                        case (int)MapType.STAIR_DOWN:
                            SetPixel(x, y, Color.DarkOrange);
                            break;
                        case (int)MapType.TRANSPORT:
                            SetPixel(x, y, Color.Chocolate);
                            break;
                    }
                }
            }

            foreach (GameWindow window in GameWindows)
            {
                if (window.HandleWindow == hWnd)
                {
                    try
                    {
                        Queue<Node> Nodes = new Queue<Node>(window.MoveManager.Path.Nodes);
                        foreach (Node node in Nodes)
                        {
                            if (node.X < width && node.Y < height && map.Data[node.X, node.Y] < 3)
                            {
                                SetPixel(node.X, node.Y, Color.Aqua);
                            }
                        }
                    }
                    catch { }
                }
            }

            if (drawSelf && CurrentLocation.X < width && CurrentLocation.Y < height)
            {
                SetPixel(CurrentLocation.X, CurrentLocation.Y, Color.Blue);
            }

            if (ActiveObjectList != null)
            {
                if (DisplayNPC || DisplayTreasureChest)
                {
                    foreach (ActiveObject ao in ActiveObjectList)
                    {
                        if (!string.IsNullOrWhiteSpace(ao.Name) && ao.X <= width && ao.Y <= height)
                        {
                            if (DisplayNPC && ao.NPC)
                            {
                                SetPixel(ao.X, ao.Y, Color.Green);
                            }

                            if (DisplayTreasureChest && ao.Name.Contains("寶箱"))
                            {
                                SetPixel(ao.X, ao.Y, Color.Goldenrod);
                            }
                        }
                    }
                }
            }

            //using (MemoryStream stream = new MemoryStream(pixels))
            {
                
            }

            LastPixels = pixels;

            return BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (WorkTask != null)
            {
                CTS.Cancel();
            }
        }
    }
}
