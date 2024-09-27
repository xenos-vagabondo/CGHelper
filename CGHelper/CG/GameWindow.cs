using CGHelper.CG.Enum;
using CommonLibrary;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CGHelper.CG
{
    public class GameWindow : IDisposable
    {
        public TabItem TabItem { get; set; }
        public Grid UIGrid { get; set; }

        public IntPtr HandleWindow { get; set; }

        public int HandleProcess { get; set; }

        private CancellationTokenSource CTS { get; set; }
        public Task WorkTask { get; set; }

        public string ClassName { get; set; }

        public bool AutoAttack { get; set; }
        public bool PetAutoAttack { get; set; }
        public bool AutoFlee { get; set; }
        public bool AutoProduce { get; set; }

        public bool FixMode { get; set; }
        public bool SkillMode { get; set; }

        public bool CaptureMode { get; set; }

        public bool PowerSavingMode { get; set; }

        public bool ItemLure { get; set; }
        public Location LocationItemLure { get; set; }

        public Item UsingItemLure { get; set; }

        public bool ItemAntiLure { get; set; }

        public Item UsingItemAntiLure { get; set; }


        public bool AutoUseCuisines { get; set; }
        public bool AutoChangePet { get; set; }

        public ArrayList UseSkills { get; set; } = new ArrayList();

        public string RoleName { get; set; }

        public Inventory Inventory { get; set; }

        public GeneralController GeneralController { get; set; }
        public BattleController BattleController { get; set; }
        public ProduceController ProduceController { get; set; }
        public MoveManager MoveManager { get; set; }

        public GameWindow(IntPtr handleWindow, string className)
        {
            HandleWindow = handleWindow;
            ClassName = className;

            if (ClassName.Equals("魔力寶貝"))
            {
                //初心
                CGAddr.SetOMAddr();
            }
            else if (ClassName.Equals("Blue"))
            {
                //水藍
                CGAddr.SetBlueAddr();
            }
            else if (ClassName.Equals("御守魔力"))
            {
                //御守
                CGAddr.SetOMAMORIAAddr();
            }

            GeneralController = new GeneralController(this);
            BattleController = new BattleController(this);
            ProduceController = new ProduceController(this);
            MoveManager = new MoveManager(this);
        }

        public void Watcher()
        {
            while (!CTS.Token.IsCancellationRequested)
            {
                HandleProcess = WinAPI.GetProcess(HandleWindow);
                if (HandleProcess == 0)
                {
                    RoleName = null;
                    Application.Current.Dispatcher.Invoke(new Action(() => UpdateUI()));
                    break;
                }

                string roleName = Common.GetRoleName(HandleProcess);
                if (roleName != null && !roleName.Equals(RoleName))
                {
                    RoleName = roleName;
                    UseSkills = new ArrayList();
                    Application.Current.Dispatcher.Invoke(new Action(() => UpdateUI()));
                }

                GeneralController.Watcher();
                ProduceController.Watcher();
                BattleController.Watcher();

                Common.Delay(500);
            }

            Dispose();
        }

        public void Start()
        {
            CTS = new CancellationTokenSource();
            WorkTask = new Task(Watcher, CTS.Token);
            WorkTask.Start();
        }

        public void Stop()
        {
            if (WorkTask != null)
            {
                CTS.Cancel();
            }

            Dispose();
        }

        public void Dispose()
        {
            WorkTask = null;

            UIGrid = null;

            MoveManager.Stop();
        }

        public void UpdateUI()
        {
            TabItem.Header = string.IsNullOrEmpty(RoleName) ? "TabItem" : RoleName;

            foreach (UIElement child in UIGrid.Children)
            {
                if (child is ComboBox comboBox)
                {
                    if (comboBox.Items.IndexOf(RoleName) == -1)
                    {
                        comboBox.Items.Remove(comboBox.SelectedItem);
                        comboBox.SelectedItem = RoleName;
                        if (!string.IsNullOrEmpty(RoleName))
                        {
                            comboBox.Items.Add(RoleName);
                        }
                    }
                    break;
                }
            }
        }
    }
}
