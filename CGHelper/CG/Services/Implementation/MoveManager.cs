using CGHelper.CG.Enum;
using CGHelper.CG.Services.Interface;
using CommonLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CGHelper.CG
{
    public class MoveManager : IManager
    {
        GameWindow Window { get; set; }

        private CancellationTokenSource CTS { get; set; }

        private Task WorkTask { get; set; }

        public Stack<CGNode> WalkingNodes { get; set; } = new Stack<CGNode>();

        public MovePath Path { get; set; } = new MovePath();

        public Location LastLocation { get; set; }

        public MapState LastState { get; set; }

        private Location RecordLocation { get; set; }

        private Location CircleNextLocation { get; set; }

        private bool CircleMoveMode { get; set; }

        private bool MazeMode { get; set; }

        public MapType MazeStairType { get; set; }

        public bool AntiLure { get; set; } = false;

        private bool UsingKey { get; set; } = false;

        private Dictionary<string, RecordPoint> Points { get; set; } = new Dictionary<string, RecordPoint>();

        private ArrayList ExceptionNames { get; set; } = new ArrayList();

        public class MovePath
        {
            public string Name { get; set; }

            public string Action { get; set; }

            public CGNode StartNode { get; set; }
            public CGNode EndNode { get; set; }
            public Queue<CGNode> Nodes { get; set; } = new Queue<CGNode>();

            public string ToString()
            {
                return StartNode + " to " + EndNode + " count = " + Nodes.Count;
            }
        }

        class RecordPoint
        {
            public Location Location { get; set; }

            public int MapCode { get; set; }
        }

        public MoveManager(GameWindow gameWindow)
        {
            Window = gameWindow;
        }

        public void Start()
        {
            int hProcess = Window.HandleProcess;

            SetRecordLocation(hProcess);

            CTS = new CancellationTokenSource();
            WorkTask = new Task(AutoMove, CTS.Token);
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
            RecordLocation = null;
            CircleNextLocation = null;
        }

        public void SetRecordLocation(int hProcess)
        {
            RecordLocation = Location.GetLocation(hProcess);
        }

        public string GetRecordLocation()
        {
            return RecordLocation != null ? RecordLocation.ToString() : null;
        }

        private void AutoMove()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            while (!CTS.Token.IsCancellationRequested)
            {
                Location location = Location.GetLocation(hProcess);
                MapState state = Common.GetMapState(hProcess);

                if (RecordLocation != null)
                {
                    if (MapChanged(location))
                    {
                        if (LastLocation != null)
                        {
                            Log.WriteLine("location changed(" + LastLocation.Name + " -> " + location.Name + "), clear path!!!");
                        }
                        Path = new MovePath();
                        WalkingNodes = new Stack<CGNode>();
                    }

                    if (state == MapState.STATE_MAP)
                    {
                        if (CircleMoveMode)
                        {
                            if (CheckRecordLocationName(location))
                            {
                                CircleMove();
                            }
                        }
                        else
                        {
                            if (Map.MapImmobile.Count == 0)
                            {
                                Map.GetMapInfoFromBinFiles(Common.GetExeFilePath(hProcess));
                            }

                            if (!Window.ItemAntiLure || AntiLure)
                            {
                                GetPathNodes(location);
                                RandomMove();
                            }
                        }
                    }
                    else if (state == MapState.STATE_BATTLE)
                    {
                        if (!UsingKey)
                        {
                            AntiLure = false;
                        }
                    }

                }

                LastState = state;
                LastLocation = location;

                Common.Delay(100);
            }
        }

        private bool CheckRecordLocationName(Location location)
        {
            if (RecordLocation == null || location == null)
            {
                return false;
            }

            if (!RecordLocation.Name.Equals(location.Name))
            {
                if (MazeMode)
                {
                    MatchCollection m = Regex.Matches(RecordLocation.Name, @"\d+");
                    if (m.Count == 0)
                    {
                        return false;
                    }

                    string variable = m[m.Count - 1].ToString();
                    if (string.IsNullOrEmpty(variable))
                    {
                        return false;
                    }

                    int variableIndex = RecordLocation.Name.IndexOf(variable);
                    string first = RecordLocation.Name.Substring(0, variableIndex);
                    string last = RecordLocation.Name.Substring(variableIndex + variable.Length, RecordLocation.Name.Length - (variableIndex + variable.Length));
                    string match = Regex.Match(location.Name, first + @"\d+" + last).Groups[0].ToString();

                    if (string.IsNullOrEmpty(match))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }


            return true;
        }

        private bool FindTreasureChest(Map map, Location location, CGNode locationNode, Inventory inventory)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (!MazeMode)
            {
                return false;
            }

            if (Path.EndNode != null && !string.IsNullOrEmpty(Path.Name) && Path.Name.Contains("寶箱"))
            {
                Location targetLocation = new Location(Path.EndNode.X, Path.EndNode.Y);
                int distance = Location.Distance(location, targetLocation);
                if (distance == 1)
                {
                    string key = "銅鑰匙";
                    if (Path.Name.Contains("黑色"))
                    {
                        key = "黑鑰匙";
                    }
                    else if (Path.Name.Contains("白色"))
                    {
                        key = "白鑰匙";
                    }

                    Item item = inventory.FuzzySearch(key);
                    if (item != null)
                    {
                        if (!Common.ExpWindowShow(hWnd))
                        {
                            if (Common.RightButtonClickNPC(hWnd, targetLocation))
                            {
                                if (Inventory.UseItem(hWnd, item))
                                {
                                    UsingKey = true;
                                    Log.WriteLine(Window.RoleName + " 使用 " + item.Name);
                                    Common.Delay(250);
                                    Button.Inventory(hProcess, false);

                                    Path = new MovePath();
                                    WalkingNodes = new Stack<CGNode>();
                                }
                            }
                            else
                            {
                                Button.Inventory(hProcess, false);
                            }
                        }
                    }
                }
                else
                {
                    MovePath path = new MovePath
                    {
                        Name = Path.Name,
                        StartNode = locationNode,
                        EndNode = Path.EndNode
                    };

                    GeneratePath(map, path, true);
                }

                return true;
            }
            else
            {
                ArrayList ActiveObjectList = ActiveObject.GetObject(hProcess);
                foreach (ActiveObject ao in ActiveObjectList)
                {
                    if (!string.IsNullOrWhiteSpace(ao.Name) && ao.Name.Contains("寶箱") && (ao.Type & 0x20000) > 0)
                    {
                        string key = "銅鑰匙";
                        if (ao.Name.Contains("黑色"))
                        {
                            key = "黑鑰匙";
                        }
                        else if (ao.Name.Contains("白色"))
                        {
                            key = "白鑰匙";
                        }

                        Item item = inventory.FuzzySearch(key);
                        if (item == null)
                        {
                            return false;
                        }

                        Console.WriteLine("發現 " + ao.ToString());

                        CGNode node = map.CGAStar.GetNode(ao.X, ao.Y);
                        MovePath path = new MovePath
                        {
                            Name = ao.Name,
                            StartNode = locationNode,
                            EndNode = node
                        };

                        return GeneratePath(map, path, true);
                    }
                }
            }
            
            return false;
        }

        private bool FindNPC(Map map, Location location, CGNode locationNode, string name)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            ExceptionNames.Add(name);

            ArrayList activeObjectList = ActiveObject.GetObject(hProcess);

            if (Path.EndNode != null && !string.IsNullOrEmpty(Path.Name) && Path.Name.Contains(name))
            {
                Location targetLocation = new Location(Path.EndNode.X, Path.EndNode.Y);
                int distance = Location.Distance(location, targetLocation);
                if (distance == 1 && Path.Nodes.Count == 0)
                {
                    if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                    {
                        Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                    }
                }
                else
                {
                    if (Path.Nodes.Count == 0)
                    {
                        Path = new MovePath();
                        WalkingNodes = new Stack<CGNode>();
                    }
                }

                return true;
            }

            foreach (ActiveObject ao in activeObjectList)
            {
                if (ao.NPC && !string.IsNullOrWhiteSpace(ao.Name) && ao.Name.Equals(name))
                {
                    if (Points.ContainsKey(ao.Name))
                    {
                        RecordPoint recordPoint = Points[ao.Name];
                        if (recordPoint.MapCode != location.Code)
                        {
                            recordPoint.Location = new Location(ao.X, ao.Y);
                            recordPoint.Location.Name = location.Name;
                            recordPoint.MapCode = location.Code;

                            Points.Remove(ao.Name);

                            Points.Add(ao.Name, recordPoint);
                        }
                        continue;
                    }
                    else
                    {
                        RecordPoint recordPoint = new RecordPoint();
                        recordPoint.Location = new Location(ao.X, ao.Y);
                        recordPoint.Location.Name = location.Name;
                        recordPoint.MapCode = location.Code;
                        Points.Add(ao.Name, recordPoint);
                    }

                    Console.WriteLine("發現 " + ao.ToString());

                    CGNode node = map.CGAStar.GetNode(ao.X, ao.Y);
                    int distance = Location.Distance(location, node);
                    if (distance > 1)
                    {
                        MovePath path = new MovePath
                        {
                            Name = ao.Name,
                            StartNode = locationNode,
                            EndNode = node
                        };

                        return GeneratePath(map, path, true);
                    }
                }
            }

            return false;
        }

        private bool FindStairOrTransport(Map map, Location location, CGNode locationNode)
        {
            if (!MazeMode)
            {
                return false;
            }

            //if (Path.EndNode != null && !string.IsNullOrEmpty(Path.Name) && Path.Name.Equals(location.Name) && (Path.EndNode.Type == MazeStairType || Path.EndNode.Type == (byte)Map.Type.TRANSPORT))
            if (Path.EndNode != null && !string.IsNullOrEmpty(Path.Name) && Path.Name.Equals(location.Name))
            {
                CGNode node = map.CGAStar.GetNode(Path.EndNode.X, Path.EndNode.Y);
                if (node != null && node.Type == (byte)MazeStairType || (node.Type == (byte)MapType.TRANSPORT && !RecordLocation.Name.Equals(location.Name)))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("2222222222");
                    Path = new MovePath();
                    WalkingNodes = new Stack<CGNode>();
                }
            }

            foreach (CGNode node in map.ChangeMapNode)
            {
                if (node.Type == (byte)MazeStairType || (node.Type == (byte)MapType.TRANSPORT && !RecordLocation.Name.Equals(location.Name)))
                {
                    if (!string.IsNullOrEmpty(location.Name))
                    {
                        if (RecordLocation.Name.Equals(location.Name) && node.X == RecordLocation.X && node.Y == RecordLocation.Y)
                        {
                            continue;
                        }
                    }

                    MovePath path = new MovePath
                    {
                        Name = location.Name,
                        StartNode = locationNode,
                        EndNode = node
                    };

                    if (GeneratePath(map, path, false))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool VisitFog(Map map, CGNode locationNode)
        {
            /*
            if (Path.EndNode != null && Location.Distance(locationNode, Path.EndNode) <= 13)
            {
                foreach (Node node in adjacentFogNodes)
                {
                    int distance = Location.Distance(Path.EndNode, node);

                    if (distance > 5 || distance == 0)
                    {
                        continue;
                    }

                    if (Path.StartNode != null)
                    {
                        if (Path.StartNode.X < Path.EndNode.X)
                        {
                            if (node.X < Path.EndNode.X)
                            {
                                continue;
                            }
                        }

                        if (Path.StartNode.X > Path.EndNode.X)
                        {
                            if (node.X > Path.EndNode.X)
                            {
                                continue;
                            }
                        }

                        if (Path.StartNode.Y < Path.EndNode.Y)
                        {
                            if (node.Y < Path.EndNode.Y)
                            {
                                continue;
                            }
                        }

                        if (Path.StartNode.Y > Path.EndNode.Y)
                        {
                            if (node.Y > Path.EndNode.Y)
                            {
                                continue;
                            }
                        }
                    }

                    MovePath path = new MovePath
                    {
                        StartNode = map.AStar.GetNode(Path.EndNode.X, Path.EndNode.Y),
                        EndNode = node
                    };

                    if (GeneratePath(map, path, false, true))
                    {
                        return true;
                    }
                }
            }
            */


            List<Node> adjacentFogNodes = new List<Node>();
            if (Path.EndNode != null)
            {
                adjacentFogNodes = GetAdjacentFogNodes(map, Path.EndNode);
            }
            else
            {
                adjacentFogNodes = GetAdjacentFogNodes(map, locationNode);
            }

            if ((Path.EndNode != null && Location.Distance(locationNode, Path.EndNode) <= 12) || Path.Nodes.Count == 0)
            {
                foreach (CGNode node in adjacentFogNodes)
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };

                    if (GeneratePath(map, path, false))
                    {
                        return true;
                    }
                }
            }

            if (adjacentFogNodes.Count > 0)
            {
                return true;
            }

            return false;
        }

        private List<Node> GetAdjacentFogNodes(Map map, CGNode locationNode)
        {
            List<CGNode> walkableNodes = map.WalkableNodes.OrderBy(n => Location.Distance(locationNode, n)).ToList();

            List<Node> adjacentFogNodes = new List<Node>();
            foreach (CGNode node in walkableNodes)
            {
                if (!map.AdjacentFog(node.X, node.Y))
                {
                    continue;
                }

                adjacentFogNodes.Add(node);
            }
            return adjacentFogNodes.OrderBy(n => Location.Distance(locationNode, n)).ToList();
        }

        private void VisitRandomNode(Map map, CGNode locationNode)
        {
            //random walk
            if (Path.Nodes.Count == 0 && map.WalkableNodes.Count > 0)
            {
                MovePath path = new MovePath();
                int count = 10;
                do
                {
                    path.EndNode = map.WalkableNodes[new Random().Next(0, map.WalkableNodes.Count)];
                    path.Nodes = map.CGAStar.FindPath(locationNode, path.EndNode);
                } while (path.Nodes == null && --count > 0);

                if (path.Nodes != null)
                {
                    Path.StartNode = locationNode;
                    Path.EndNode = path.EndNode;
                    path.Nodes = ReversePath(path.Nodes);
                    path.Nodes.Dequeue();
                    Path.Nodes = path.Nodes;
                    //Console.WriteLine("4444 " + Path.ToString());
                }
            }
        }

        private void GetPathNodes(Location location)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            Map map = Map.GetMap(hProcess);
            map.AnalyzeNodes();

            UsingKey = false;

            CGNode locationNode = map.CGAStar.GetNode(location);
            if (locationNode == null)
            {
                return;
            }

            Inventory inventory = Inventory.GetInventoryInfo(hProcess);
            if (inventory.FuzzySearch("鑰匙") != null)
            {
                if (FindTreasureChest(map, location, locationNode, inventory))
                {
                    return;
                }
            }

            if (CheckMissionPath(map, location, locationNode))
            {
                return;
            }

            if (!CheckRecordLocationName(location))
            {
                return;
            }

            if (!FindStairOrTransport(map, location, locationNode))
            {
                if (!VisitFog(map, locationNode))
                {
                    VisitRandomNode(map, locationNode);
                }
            }
        }

        private bool CheckMissionPath(Map map, Location location, CGNode locationNode)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            Inventory inventory = Inventory.GetInventoryInfo(hProcess);

            if (inventory.FuzzySearch("刀刃的碎片") != null)
            {
                bool changeMap = true;

                ArrayList nodes = new ArrayList();
                if (location.Name.Equals("民家地下"))
                {
                    nodes.Add(map.CGAStar.GetNode(7, 3));
                }
                else if (location.Name.Equals("民家"))
                {
                    nodes.Add(map.CGAStar.GetNode(11, 17));
                }
                else if (location.Name.Equals("阿巴尼斯村"))
                {
                    nodes.Add(map.CGAStar.GetNode(37, 71));
                }
                else if (location.Name.Equals("莎蓮娜"))
                {
                    nodes.Add(map.CGAStar.GetNode(54, 162));
                    changeMap = false;
                }

                GeneratePathFromList(map, locationNode, nodes, false, changeMap);

                return true;
            }
            else if (location.Name.Contains("詛咒的迷宮"))
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Contains("地下13樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(16, 3));
                }
                else if (location.Name.Contains("地下15樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(25, 21));
                }
                else if (location.Name.Contains("地下16樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(17, 18));
                }
                else if (location.Name.Contains("地下18樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(7, 12));
                }
                else if (location.Name.Contains("地下22樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(7, 4));
                    nodes.Add(map.CGAStar.GetNode(17, 12));
                }
                else if (location.Name.Contains("地下23樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(14, 12));
                }
                else if (location.Name.Contains("地下24樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(7, 20));
                }
                else if (location.Name.Contains("地下27樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(9, 11));
                    nodes.Add(map.CGAStar.GetNode(23, 16));
                }
                else if (location.Name.Contains("地下28樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(25, 21));
                }
                else if (location.Name.Contains("地下31樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(24, 13));
                }
                else if (location.Name.Contains("地下33樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(6, 13));
                }
                else if (location.Name.Contains("地下34樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(23, 4));
                }
                else if (location.Name.Contains("地下35樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(6, 13));
                }
                else if (location.Name.Contains("地下37樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(24, 21));
                }
                else if (location.Name.Contains("地下38樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(8, 21));
                }
                else if (location.Name.Contains("地下51樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(25, 23));
                }
                else if (location.Name.Contains("地下52樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(23, 4));
                }
                else if (location.Name.Contains("地下53樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(25, 23));
                    nodes.Add(map.CGAStar.GetNode(15, 5));
                }
                else if (location.Name.Contains("地下54樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(23, 4));
                    nodes.Add(map.CGAStar.GetNode(15, 11));
                }
                else if (location.Name.Contains("地下55樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(19, 17));
                    nodes.Add(map.CGAStar.GetNode(15, 8));
                }
                else if (location.Name.Contains("地下56樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(25, 12));
                }
                else if (location.Name.Contains("地下57樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(23, 3));
                    nodes.Add(map.CGAStar.GetNode(13, 15));
                }
                else if (location.Name.Contains("地下58樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(18, 25));
                }

                return GeneratePathFromList(map, locationNode, nodes, false, true);
            }
            else if (location.Name.Contains("個難關"))
            {
                ArrayList nodes = new ArrayList();
                if (map.ChangeMapNode.Count == 1 && map.ChangeMapNode[0].Type == (byte)MapType.STAIR_DOWN)
                {
                    //nodes.Add(map.ChangeMapNode[0]);
                }
                else
                {
                    if (location.Name.Equals("第一個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(22, 15));
                    }
                    else if (location.Name.Equals("第二個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(25, 17));
                    }
                    else if (location.Name.Equals("第三個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(20, 20));
                    }
                    else if (location.Name.Equals("第四個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(16, 18));
                    }
                    else if (location.Name.Equals("第五個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(22, 15));
                    }
                    else if (location.Name.Equals("第六個難關"))
                    {
                        nodes.Add(map.CGAStar.GetNode(23, 19));
                    }
                }
                
                return GeneratePathFromList(map, locationNode, nodes, false, false);
            }

            else if (inventory.FuzzySearch("希望的蠟燭") != null)
            {
                if (location.Name.Equals("奇利村"))
                {
                    CGNode node = map.CGAStar.GetNode(59, 45);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            Name = "奇利村",
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }

                    return true;
                }
                else if (location.Name.Equals("索奇亞"))
                {
                    CGNode node = map.CGAStar.GetNode(240, 265);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            Name = "索奇亞",
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }

                    return true;
                }
                else if (location.Name.Equals("索奇亞海底洞窟 地下1樓"))
                {
                    CGNode node = map.CGAStar.GetNode(24, 13);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            Name = "索奇亞海底洞窟 地下1樓",
                            StartNode = locationNode,
                            EndNode = node
                        };

                        if (GeneratePath(map, path, false))
                        {
                            return true;
                        }
                    }

                    node = map.CGAStar.GetNode(7, 41);
                    if (node != null)
                    {
                        int distance = Location.Distance(locationNode, node);
                        if (distance > 1)
                        {
                            MovePath path = new MovePath
                            {
                                StartNode = locationNode,
                                EndNode = node
                            };
                            GeneratePath(map, path, true);
                        }
                        else
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(7, 41));
                        }
                    }

                    return true;
                }
                else if (location.Name.Equals("索奇亞海底洞窟 地下2樓"))
                {
                    CGNode node = map.CGAStar.GetNode(35, 7);
                    if (node != null)
                    {
                        if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node) || Path.Nodes.Count == 0)
                        {
                            int distance = Location.Distance(locationNode, node);
                            if (distance > 1)
                            {
                                MovePath path = new MovePath
                                {
                                    StartNode = locationNode,
                                    EndNode = node
                                };

                                GeneratePath(map, path, true);
                            }
                            else
                            {
                                if (Path.Nodes.Count == 0)
                                {
                                    ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                                    if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                                    {
                                        Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                                    }
                                    else
                                    {
                                        TeamInfo.DisbandTeam(hProcess);
                                    }
                                }
                            }
                        }
                    }

                    return true;
                }
                else if (location.Name.Equals("海底洞窟 地下3樓"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader && teamInfo.Member.Count > 0)
                    {
                        CGNode node = map.CGAStar.GetNode(14, 14);
                        if (node != null)
                        {
                            if ((MapType)node.Type == MapType.TRANSPORT)
                            {
                                MovePath path = new MovePath
                                {
                                    StartNode = locationNode,
                                    EndNode = node
                                };

                                GeneratePath(map, path, false);
                            }
                        }
                    }

                    return true;
                }
                else if (location.Name.Contains("黑色的祈禱"))
                {

                }
                else if (location.Name.Equals("黑之祈禱") && inventory.FuzzySearch("恐怖旅團之證") != null)
                {
                    CGNode node = map.CGAStar.GetNode(24, 32);
                    if (node != null)
                    {
                        //if (Path.EndNode == null || (Path.EndNode.X != node.X && Path.EndNode.Y != node.Y))
                        {
                            int distance = Location.Distance(locationNode, node);
                            if (distance > 1)
                            {
                                MovePath path = new MovePath
                                {
                                    StartNode = locationNode,
                                    EndNode = node
                                };

                                GeneratePath(map, path, true);
                            }
                            else
                            {
                                if (Path.Nodes.Count == 0)
                                {
                                    ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                                    if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                                    {
                                        Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                                    }
                                    else
                                    {
                                        TeamInfo.DisbandTeam(hProcess);
                                    }
                                }
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    return true;
                }
            }
            else if (location.Name.Contains("黑色的祈禱") && inventory.FuzzySearch("恐怖旅團之證") == null)
            {
                if (FindNPC(map, location, locationNode, "萌子"))
                {
                    return true;
                }

                if (Points.ContainsKey("萌子"))
                {
                    RecordPoint recordPoint = Points["萌子"];

                    if (location.Name.Equals("黑色的祈禱地下1樓") && GetAdjacentFogNodes(map, locationNode).Count > 0)
                    {
                        if (!recordPoint.Location.Name.Equals(location.Name))
                        {
                            Points.Remove("萌子");
                        }
                    }

                    if (recordPoint.MapCode == location.Code)
                    {
                        CGNode node = map.CGAStar.GetNode(recordPoint.Location.X, recordPoint.Location.Y);
                        MovePath path = new MovePath
                        {
                            Name = "萌子",
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, true);
                    }
                    else
                    {
                        FindStairOrTransport(map, location, locationNode);
                    }
                }
                else
                {
                    if (!VisitFog(map, locationNode))
                    {
                        FindStairOrTransport(map, location, locationNode);
                    }
                }

                return true;
            }

            else if (location.Name.Contains("地下遺跡"))
            {
                if (inventory.FuzzySearch("木之鑰匙") != null)
                {
                    if (location.Name.Equals("地下遺跡 最下層"))
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (FindNPC(map, location, locationNode, "雅可布督府執政官"))
                    {
                        return true;
                    }

                    if (location.Name.Equals("地下遺跡地下1階") && GetAdjacentFogNodes(map, locationNode).Count > 0)
                    {
                        Points.Remove("雅可布督府執政官");
                    }

                    RecordPoint recordPoint = new RecordPoint();
                    if (Points.TryGetValue("雅可布督府執政官", out recordPoint))
                    {
                        if (recordPoint.MapCode == location.Code)
                        {
                            CGNode node = map.CGAStar.GetNode(recordPoint.Location.X, recordPoint.Location.Y);
                            int distance = Location.Distance(location, node);
                            if (distance > 1)
                            {
                                MovePath path = new MovePath
                                {
                                    Name = "雅可布督府執政官",
                                    StartNode = locationNode,
                                    EndNode = node
                                };
                                GeneratePath(map, path, true);
                            }
                        }
                        else
                        {
                            FindStairOrTransport(map, location, locationNode);
                        }
                    }
                    else
                    {
                        if (!VisitFog(map, locationNode))
                        {
                            FindStairOrTransport(map, location, locationNode);
                        }
                    }
                }

                return true;
            }

            else if (location.Name.Equals("城堡的地下迷宮"))
            {
                CGNode node = map.CGAStar.GetNode(146, 3);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        Name = location.Name,
                        StartNode = locationNode,
                        EndNode = node
                    };

                    GeneratePath(map, path, false);
                }

                return true;
            }

            else if (location.Name.Equals("王族用脫逃暗道 西方"))
            {
                CGNode node = map.CGAStar.GetNode(63, 18);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        Name = location.Name,
                        StartNode = locationNode,
                        EndNode = node
                    };

                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Equals("王族用脫逃暗道 東方"))
            {
                CGNode node = map.CGAStar.GetNode(43, 5);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        Name = location.Name,
                        StartNode = locationNode,
                        EndNode = node
                    };

                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Contains("大樹"))
            {
                CGNode node = null;
                if (location.Name.Contains("1樓"))
                {
                    if (inventory.FuzzySearch("誓約的羽毛") != null ||
                        inventory.FuzzySearch("紅色返魂珠") != null) 
                    {
                        node = map.CGAStar.GetNode(28, 23);
                    } 
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(41, 8);
                    }
                }
                else if (location.Name.Contains("2樓"))
                {
                    if (inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(11, 9);
                    }
                    else if(inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(29, 28);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") != null)
                    {
                        node = map.CGAStar.GetNode(38, 5);
                    }
                }
                else if (location.Name.Contains("3樓"))
                {
                    if (inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(9, 13);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(10, 37);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") != null)
                    {
                        node = map.CGAStar.GetNode(29, 20);
                    }
                }
                else if (location.Name.Contains("4樓"))
                {
                    if (inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(8, 23);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(24, 16);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") != null)
                    {
                        node = map.CGAStar.GetNode(24, 28);
                    }
                }
                else if (location.Name.Contains("5樓"))
                {
                    if (inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(7, 19);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(24, 24);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") != null)
                    {
                        node = map.CGAStar.GetNode(24, 36);
                    }
                }
                else if (location.Name.Equals("大樹 地下"))
                {
                    if (inventory.FuzzySearch("誓約的羽毛") != null ||
                        inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(9, 6);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(10, 20);
                    }
                }
                else if (location.Name.Contains("階"))
                {
                    return false;
                }
                else if (location.Name.Contains("最下層"))
                {
                    if (inventory.FuzzySearch("誓約的羽毛") != null ||
                        inventory.FuzzySearch("紅色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(21, 8);
                    }
                    else if (inventory.FuzzySearch("誓約的羽毛") == null ||
                        inventory.FuzzySearch("藍色返魂珠") != null)
                    {
                        node = map.CGAStar.GetNode(23, 21);
                    }
                }

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }

            else if (location.Name.Equals("湯瑪斯長老家"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.Member.Count > 0)
                {
                    CGNode node = map.CGAStar.GetNode(16, 11);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }
                else
                {
                    if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                    {
                        Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                    }
                }

                return true;
            }
            else if (location.Name.Contains("冰之神殿"))
            {
                CGNode node = null;
                if (location.Name.Equals("冰之神殿"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count > 0)
                    {
                        int itemNumber = inventory.GetItemNumber("公主日記");
                        if (itemNumber == 0)
                        {
                            node = map.CGAStar.GetNode(39, 42);
                        }
                        else if (itemNumber == 1)
                        {
                            node = map.CGAStar.GetNode(18, 36);
                        }
                    } 
                    else
                    {
                        int itemNumber = inventory.GetItemNumber("公主日記");
                        if (itemNumber > 0)
                        {
                            if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                            {
                                Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                            }
                        }
                    }
                }
                else if (location.Name.Contains("地下"))
                {
                    if (!CheckRecordLocationName(location))
                    {
                        SetRecordLocation(hProcess);
                        MazeMode = true;
                        MazeStairType = MapType.STAIR_DOWN;
                    }

                    return false;
                }
                else if (location.Name.Contains("底層"))
                {
                    //32226 13,15 巴達克手下
                    //33227 13,15 賢者露娜
                    node = map.CGAStar.GetNode(13, 15);
                }

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node) || node != null && Location.Distance(location, node) != 0)
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Equals("弗利德島"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.Member.Count > 0)
                {
                    bool changeMap = false;
                    ArrayList nodes = new ArrayList();
                    int itemNumber = inventory.GetItemNumber("公主日記");
                    if (itemNumber == 0)
                    {
                        nodes.Add(map.CGAStar.GetNode(71, 313));
                    }
                    else if (itemNumber == 1)
                    {
                        if (location.Code == 32202)
                        {
                            changeMap = true;
                            nodes.Add(map.CGAStar.GetNode(67, 84));//庫德洞窟
                            nodes.Add(map.CGAStar.GetNode(13, 10));//火炎之谷
                        }
                        else if (location.Code == 32201)
                        {
                            CGNode node = map.CGAStar.GetNode(142, 230);
                            if (node != null)
                            {
                                node.Walkable = false;
                            }
                            node = map.CGAStar.GetNode(142, 231);
                            if (node != null)
                            {
                                node.Walkable = false;
                            }
                            nodes.Add(map.CGAStar.GetNode(190, 207));//NPC
                        }
                    }
                    else if (itemNumber == 2)
                    {
                        changeMap = true;
                        if (location.Code == 32201)
                        {
                            nodes.Add(map.CGAStar.GetNode(173, 91));//威爾森酒吧
                            nodes.Add(map.CGAStar.GetNode(140, 230));//弗利德島
                        }
                        else if (location.Code == 32200)
                        {
                            nodes.Add(map.CGAStar.GetNode(9, 7));//砂之塔
                        }
                    }
                    else if (itemNumber == 3)
                    {
                        changeMap = true;
                        if (location.Code == 32200)
                        {
                            //32200
                            nodes.Add(map.CGAStar.GetNode(86, 157));//漢米頓村
                        }
                        else if (location.Code == 32201)
                        {
                            //32201
                            nodes.Add(map.CGAStar.GetNode(92, 101));//庫德洞窟
                        }
                            
                    }

                    GeneratePathFromList(map, locationNode, nodes, false, changeMap);
                }
                else
                {
                    int itemNumber = inventory.GetItemNumber("公主日記");
                    if (itemNumber == 1 && location.X == 190 && location.Y == 207)
                    {

                    }
                    else
                    {
                        if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                        }
                    }
                }

                return true;
            }
            else if (location.Name.Equals("庫德洞窟"))
            {
                CGNode node = null;
                if (location.Code == 32225)
                {
                    node = map.CGAStar.GetNode(35, 30);
                }
                else if (location.Code == 32224)
                {
                    node = map.CGAStar.GetNode(24, 27);
                }
                else if (location.Code == 32223)
                {
                    node = map.CGAStar.GetNode(13, 7);
                }
                else if (location.Code == 32222)
                {
                    node = map.CGAStar.GetNode(17, 21);
                }
                else if (location.Code == 32221)
                {
                    node = map.CGAStar.GetNode(41, 9);
                }
                else if (location.Code == 32220)
                {
                    node = map.CGAStar.GetNode(16, 37);
                }
                else if (location.Code == 32219)
                {
                    node = map.CGAStar.GetNode(20, 11);
                }
                else if (location.Code == 32218)
                {
                    node = map.CGAStar.GetNode(45, 18);
                }

                if (node != null && Path.EndNode == null || !Path.EndNode.IsSameLocation(node) &&
                    node.Type == (int)MapType.STAIR_UP || node.Type == (int)MapType.STAIR_DOWN)
                {
                    MovePath path = new MovePath
                    {
                        Name = location.Name,
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Contains("火炎之谷"))
            {
                if (location.Name.Contains("地下"))
                {
                    if (!CheckRecordLocationName(location))
                    {
                        SetRecordLocation(hProcess);
                        MazeMode = true;
                        MazeStairType = MapType.STAIR_DOWN;
                    }

                    return false;
                }
            }
            else if (location.Name.Equals("火之谷底"))
            {
                //32230 14,13 巴達克手下
                CGNode node = map.CGAStar.GetNode(14, 13);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node) || node != null && Location.Distance(location, node) != 0)
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Equals("威爾森酒吧"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.Member.Count > 0)
                {
                    CGNode node = null;

                    int itemNumber = inventory.GetItemNumber("公主日記");
                    if (itemNumber == 2)
                    {
                        node = map.CGAStar.GetNode(47, 33);
                    }
                    else if (itemNumber == 3)
                    {
                        node = map.CGAStar.GetNode(21, 28);
                    }

                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }
                else
                {
                    int itemNumber = inventory.GetItemNumber("公主日記");
                    if (itemNumber == 3)
                    {
                        if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                        }
                    }
                }
                return true;
            }
            else if (location.Name.Contains("砂之塔"))
            {
                if (location.Name.Contains("樓"))
                {
                    if (!CheckRecordLocationName(location))
                    {
                        SetRecordLocation(hProcess);
                        MazeMode = true;
                        MazeStairType = MapType.STAIR_UP;
                    }

                    return false;
                }
                else if (location.Name.Contains("頂"))
                {
                    CGNode node = map.CGAStar.GetNode(15, 13);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node) || node != null && Location.Distance(location, node) != 0)
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }

                    return true;
                }
            }
            else if (location.Name.Contains("漢米頓"))
            {
                CGNode node = null;
                if (location.Name.Equals("漢米頓村"))
                {
                    node = map.CGAStar.GetNode(124, 131);
                }
                else if (location.Name.Equals("漢米頓商城"))
                {
                    node = map.CGAStar.GetNode(115, 93);
                }

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Contains("商城"))
            {
                CGNode node = null;

                if (location.Name.Equals("商城二樓"))
                {
                    node = map.CGAStar.GetNode(29, 18);
                } 
                else if (location.Name.Equals("商城三樓"))
                {
                    node = map.CGAStar.GetNode(17, 32);
                }

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }
                
                return true;
            }
            else if (location.Name.Equals("亞諾曼城"))
            {
                Item item = inventory.Search("彩葉原通行證");
                if (item != null)
                {
                    CGNode node = map.CGAStar.GetNode(142, 227);

                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("德威特島"))
            {
                Item item = inventory.Search("彩葉原通行證");
                if (item != null)
                {
                    CGNode node = map.CGAStar.GetNode(440, 482);

                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Contains("殷紅的山谷"))
            {
                if (!CheckRecordLocationName(location))
                {
                    SetRecordLocation(hProcess);
                    MazeMode = true;
                    MazeStairType = MapType.STAIR_UP;
                }

                return false;
            }
            else if (location.Name.Contains("彩葉原"))
            {
                if (location.Name.Equals("彩葉原")) {
                    CGNode node = null;
                    if (location.Code == 32216)
                    {
                        node = map.CGAStar.GetNode(28, 58);
                    }
                    else if (location.Code == 32217)
                    {
                        node = map.CGAStar.GetNode(67, 28);
                    }

                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count > 0)
                    {
                        if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                        {
                            MovePath path = new MovePath
                            {
                                StartNode = locationNode,
                                EndNode = node
                            };

                            GeneratePath(map, path, false);
                        }
                    }
                    else
                    {
                        if (location.Code == 32217)
                        {
                            if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                            {
                                Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                            }
                        }
                    }

                    return true;
                } 
                else
                {
                    if (!CheckRecordLocationName(location))
                    {
                        SetRecordLocation(hProcess);
                        MazeMode = true;
                        MazeStairType = MapType.STAIR_UP;
                    }

                    return false;
                }
            }
            else if (location.Name.Equals("皇后陵寢"))
            {
                CGNode node = map.CGAStar.GetNode(62, 42);

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Equals("陵墓入口"))
            {
                CGNode node = map.CGAStar.GetNode(11, 6);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };

                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Contains("墓道"))
            {
                if (location.Name.Contains("地下"))
                {
                    if (!CheckRecordLocationName(location))
                    {
                        SetRecordLocation(hProcess);
                        MazeMode = true;
                        MazeStairType = MapType.STAIR_DOWN;
                    }

                    return false;
                }
            }
            else if (location.Name.Contains("墓室"))
            {
                CGNode node = map.CGAStar.GetNode(25, 16);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }

            else if (location.Name.Equals("過去的哥拉爾"))
            {
                CGNode node = map.CGAStar.GetNode(83, 65);

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Equals("伊姆爾森林"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.TeamLeader && teamInfo.Member.Count > 0)
                {
                    ArrayList nodes = new ArrayList
                    {
                        map.CGAStar.GetNode(31, 7),
                        map.CGAStar.GetNode(29, 11),
                        map.CGAStar.GetNode(36, 39),
                        map.CGAStar.GetNode(11, 35)
                    };

                    foreach (CGNode node in nodes)
                    {
                        if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                        {
                            if (node.Type == (int)MapType.TRANSPORT)
                            {
                                MovePath path = new MovePath
                                {
                                    StartNode = locationNode,
                                    EndNode = node
                                };
                                GeneratePath(map, path, false);
                            }
                        }
                    }
                }

                return true;
            }
            else if (location.Name.Equals("森之迷宮"))
            {
                if (!CheckRecordLocationName(location))
                {
                    SetRecordLocation(hProcess);
                    MazeMode = true;
                    MazeStairType = MapType.TRANSPORT;
                }

                return false;
            }

            

            else if (location.Name.Contains("訓練設施"))
            {
                bool findPath = false;

                ArrayList nodes = new ArrayList();
                if (location.Name.Contains("第1層"))
                {
                    nodes.Add(map.CGAStar.GetNode(40, 5));
                }
                if (location.Name.Contains("第2層"))
                {
                    nodes.Add(map.CGAStar.GetNode(36, 23));
                }
                if (location.Name.Contains("第3層"))
                {
                    nodes.Add(map.CGAStar.GetNode(12, 16));
                    nodes.Add(map.CGAStar.GetNode(39, 22));
                }
                if (location.Name.Contains("第4層"))
                {
                    nodes.Add(map.CGAStar.GetNode(7, 34));
                    nodes.Add(map.CGAStar.GetNode(81, 55));
                }
                if (location.Name.Contains("第5層"))
                {
                    nodes.Add(map.CGAStar.GetNode(16, 10));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        if (GeneratePath(map, path, false))
                        {
                            findPath = true;
                        }
                    }
                }

                return findPath;
            }

            else if (location.Name.Contains("黑迷宮"))
            {
                if (inventory.FuzzySearch("龍心") != null)
                {

                }
                else
                {
                    if (FindNPC(map, location, locationNode, "黑龍"))
                    {
                        return true;
                    }

                    if (location.Name.Equals("黑迷宮") && GetAdjacentFogNodes(map, locationNode).Count > 0)
                    {
                        Points.Remove("黑龍");
                    }

                    RecordPoint recordPoint = new RecordPoint();
                    if (Points.TryGetValue("黑龍", out recordPoint))
                    {
                        if (recordPoint.MapCode == location.Code)
                        {
                            CGNode node = map.CGAStar.GetNode(recordPoint.Location.X, recordPoint.Location.Y);
                            int distance = Location.Distance(location, node);
                            if (distance > 1)
                            {
                                MovePath path = new MovePath
                                {
                                    Name = "黑龍",
                                    StartNode = locationNode,
                                    EndNode = node
                                };
                                GeneratePath(map, path, true);
                            }
                        }
                        else
                        {
                            FindStairOrTransport(map, location, locationNode);
                        }
                    }
                    else
                    {
                        if (!VisitFog(map, locationNode))
                        {
                            FindStairOrTransport(map, location, locationNode);
                        }
                    }
                }

                return true;
            }

            else if (location.Name.Equals("地下王國"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("種子麵包") != null || 
                    inventory.FuzzySearch("大地之鈴") != null ||
                    inventory.FuzzySearch("結界通行證") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(361, 408));
                } 
                else if (inventory.FuzzySearch("鏡之曲的曲譜") != null ||
                    inventory.FuzzySearch("吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(160, 355));
                }
                else if (inventory.FuzzySearch("大地鼠三階徽章") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(265, 295));
                    nodes.Add(map.CGAStar.GetNode(142, 262));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("邊境"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("種子麵包") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(399, 403));
                }
                else if (inventory.FuzzySearch("大地之鈴") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(409, 360));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, true);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("聖拉魯卡村"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("給國王的信") != null ||
                    inventory.FuzzySearch("情報") != null ||
                    inventory.FuzzySearch("大地鼠四階徽章") != null ||
                    inventory.FuzzySearch("空的寶箱") != null ||
                    inventory.FuzzySearch("魔法殘留物") != null ||
                    inventory.FuzzySearch("大地魔杖") != null ||
                    inventory.FuzzySearch("吸水鏡") != null ||
                    inventory.FuzzySearch("裝滿水的吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(36, 98));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("洞穴"))
            {
                ArrayList nodes = new ArrayList();
                nodes.Add(map.CGAStar.GetNode(12, 8));

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("大地鼠村"))
            {
                ArrayList nodes = new ArrayList();
                if (inventory.FuzzySearch("鏡之曲的曲譜") != null ||
                    inventory.FuzzySearch("大地之鈴") != null ||
                    inventory.FuzzySearch("大地鼠三階徽章") != null ||
                    inventory.FuzzySearch("結界通行證") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(22, 476));
                }
                else if (inventory.FuzzySearch("給國王的信") != null ||
                    inventory.FuzzySearch("魔法石鏡") != null ||
                    inventory.FuzzySearch("空的寶箱") != null ||
                    inventory.FuzzySearch("魔法殘留物") != null ||
                    inventory.FuzzySearch("大地魔杖") != null ||
                    inventory.FuzzySearch("裝滿水的吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(49, 433));
                }
                else if (inventory.FuzzySearch("情報") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(38, 473));
                }
                else if (inventory.FuzzySearch("角笛") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(50, 483));
                }
                else if (inventory.FuzzySearch("大地鼠四階徽章") != null && inventory.FuzzySearch("大地之鈴") == null)
                {
                    nodes.Add(map.CGAStar.GetNode(46, 446));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("大地鼠王國覲見之間"))
            {
                ArrayList nodes = new ArrayList();
                if (inventory.FuzzySearch("結界通行證") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(128, 33));
                }
                else if (inventory.FuzzySearch("給國王的信") != null ||
                    inventory.FuzzySearch("魔法石鏡") != null ||
                    inventory.FuzzySearch("空的寶箱") != null ||
                    inventory.FuzzySearch("魔法殘留物") != null ||
                    inventory.FuzzySearch("大地魔杖") != null ||
                    inventory.FuzzySearch("裝滿水的吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(181, 49));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("貝克里地底湖"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("鏡之曲的曲譜") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(130, 343));
                }
                else if (inventory.FuzzySearch("吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(130, 348));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("亞留特村"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("筆記本") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(33, 82));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("亞留特遺跡"))
            {
                ArrayList nodes = new ArrayList();
                nodes.Add(map.CGAStar.GetNode(314, 55));

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("煉獄紅蓮北域"))
            {
                ArrayList nodes = new ArrayList();
                nodes.Add(map.CGAStar.GetNode(535, 219));

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("煉獄紅蓮西域"))
            {
                ArrayList nodes = new ArrayList();
                nodes.Add(map.CGAStar.GetNode(340, 325));

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("西北煉獄洞窟"))
            {
                ArrayList nodes = new ArrayList();
                nodes.Add(map.CGAStar.GetNode(180, 46));

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("火焰鼠王國"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("吸水鏡") == null ||
                    inventory.FuzzySearch("裝滿水的吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(50, 67));
                }
                else if (inventory.FuzzySearch("火的意志") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(52, 77));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("火焰鼠村長家"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("火的意志") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(10, 21));
                }
                else if (inventory.FuzzySearch("吸水鏡") == null ||
                    inventory.FuzzySearch("裝滿水的吸水鏡") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(14, 20));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("烈焰地獄"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("火的意志") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(78, 244));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("烈焰地獄(勝利)"))
            {
                ArrayList nodes = new ArrayList();

                if (inventory.FuzzySearch("赤色結晶之石") != null)
                {
                    nodes.Add(map.CGAStar.GetNode(211, 19));
                }

                foreach (CGNode node in nodes)
                {
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }

            else if (location.Name.Contains("第") && location.Name.Contains("道場"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.TeamLeader && teamInfo.Member.Count == 5)
                {
                    Location targetLocation = new Location(15, 10);
                    int distance = Location.Distance(location, targetLocation);
                    if (distance == 0)
                    {
                        Button.Inventory(hProcess, false);
                        if (!Common.ExpWindowShow(hWnd))
                        {
                            if (Common.RightButtonClickNPC(hWnd, new Location(targetLocation.X + 1, targetLocation.Y), 1000))
                            {
                                Log.WriteLine(Window.RoleName + " click NPC");
                            }
                        }
                    }
                    else
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = map.CGAStar.GetNode(targetLocation)
                        };
                        GeneratePath(map, path, false);
                    }
                }
                else
                {
                    if (teamInfo.Member.Count == 0)
                    {
                        if (TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F6);
                        }
                    }
                }

                return true;
            }
            else if (location.Name.Contains("第") && location.Name.Contains("組通過"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.TeamLeader)
                {
                    Location targetLocation = new Location(20, 12);
                    int distance = Location.Distance(location, targetLocation);
                    if (distance == 0)
                    {
                        if (!TeamInfo.TeamMemberSameLocation(hProcess, Window.RoleName))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                        }
                        else
                        {
                            if (Common.RightButtonClickNPC(hWnd, new Location(targetLocation.X + 1, targetLocation.Y), 2000))
                            {
                                Log.WriteLine(Window.RoleName + " click NPC");
                            }
                        }
                    }
                    else if (teamInfo.Member.Count == 5)
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = map.CGAStar.GetNode(targetLocation)
                        };
                        GeneratePath(map, path, false);
                    }
                }
                
                return true;
            }

            //沉默之龍
            if (inventory.FuzzySearch("阿薩姆的介紹信") != null)
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Equals("庫魯克斯島"))
                {
                    nodes.Add(map.CGAStar.GetNode(609, 775));
                }

                GeneratePathFromList(map, locationNode, nodes, false, false);

                return true;
            }
            /*
            else if (inventory.FuzzySearch("犧牲品之指輪") != null)
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Equals("庫魯克斯島"))
                {
                    nodes.Add(map.CGAStar.GetNode(530, 706));
                    nodes.Add(map.CGAStar.GetNode(546, 635));
                }

                GeneratePathFromList(map, locationNode, nodes, false, false);

                return true;
            }
            */
            else if (location.Name.Contains("貝尼恰斯火山"))
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Equals("貝尼恰斯火山"))
                {
                    //48015
                    nodes.Add(map.CGAStar.GetNode(30, 61));
                    //48018
                    nodes.Add(map.CGAStar.GetNode(42, 49));
                    //48021
                    if (location.Code == 48021)
                    {
                        nodes.Add(map.CGAStar.GetNode(66, 61));
                    }
                }
                else if (location.Name.Contains("最下層"))
                {
                    nodes.Add(map.CGAStar.GetNode(75, 38));
                }
                else if (location.Name.Contains("1樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(38, 38));
                }
                else if (location.Name.Contains("2樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(50, 24));
                }
                else if (location.Name.Contains("3樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(84, 75));
                    nodes.Add(map.CGAStar.GetNode(46, 47));
                }
                else if (location.Name.Contains("4樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(84, 75));
                    nodes.Add(map.CGAStar.GetNode(45, 26));
                    nodes.Add(map.CGAStar.GetNode(56, 75));
                    nodes.Add(map.CGAStar.GetNode(44, 44));
                }
                else if (location.Name.Contains("5樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(42, 10));
                    nodes.Add(map.CGAStar.GetNode(57, 80));
                    nodes.Add(map.CGAStar.GetNode(43, 47));
                }
                else if (location.Name.Contains("6樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(47, 36));
                    nodes.Add(map.CGAStar.GetNode(42, 48));
                }

                GeneratePathFromList(map, locationNode, nodes, false, true);

                return true;
            }
            else if (location.Name.Equals("艾兒卡絲之家 2樓"))
            {
                ArrayList nodes = new ArrayList
                {
                    map.CGAStar.GetNode(7, 30),
                };

                foreach (CGNode node in nodes)
                {
                    if (node.Type == (int)MapType.TRANSPORT)
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("艾兒卡絲之家 地下2樓"))
            {
                ArrayList nodes = new ArrayList
                {
                    map.CGAStar.GetNode(8, 5),
                    map.CGAStar.GetNode(56, 40)
                };

                foreach (CGNode node in nodes)
                {
                    if (node.Type == (int)MapType.STAIR_UP ||
                        node.Type == (int)MapType.STAIR_DOWN ||
                        node.Type == (int)MapType.TRANSPORT)
                    {
                        if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                        {
                            MovePath path = new MovePath
                            {
                                StartNode = locationNode,
                                EndNode = node
                            };
                            GeneratePath(map, path, false);
                        }
                    }
                }

                return true;
            }
            else if (location.Name.Equals("艾兒卡絲之家 地下1樓"))
            {
                ArrayList nodes = new ArrayList
                {
                    map.CGAStar.GetNode(48, 3)
                };

                foreach (CGNode node in nodes)
                {
                    if (node.Type == (int)MapType.STAIR_UP ||
                        node.Type == (int)MapType.STAIR_DOWN ||
                        node.Type == (int)MapType.TRANSPORT)
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }

            //失翼之龍
            else if (location.Name.Equals("冥府之道"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.Member.Count == 5)
                {
                    //47029
                    CGNode node = map.CGAStar.GetNode(90, 19);
                    if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                    {
                        MovePath path = new MovePath
                        {
                            StartNode = locationNode,
                            EndNode = node
                        };
                        GeneratePath(map, path, false);
                    }
                }

                return true;
            }
            else if (location.Name.Equals("諾斯菲拉特大地"))
            {
                CGNode node = map.CGAStar.GetNode(154, 101);
                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = locationNode,
                        EndNode = node
                    };
                    GeneratePath(map, path, false);
                }

                return true;
            }
            else if (location.Name.Contains("黑之宮殿"))
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Contains("入口"))
                {
                    nodes.Add(map.CGAStar.GetNode(18, 24));
                }
                else if (location.Name.Contains("1樓"))
                {
                    //51001
                    nodes.Add(map.CGAStar.GetNode(47, 24));
                }
                else if (location.Name.Contains("中庭"))
                {
                    nodes.Add(map.CGAStar.GetNode(47, 126));
                    nodes.Add(map.CGAStar.GetNode(71, 80));
                }
                else if (location.Name.Contains("墓場"))
                {
                    //51003
                    nodes.Add(map.CGAStar.GetNode(80, 76));
                    nodes.Add(map.CGAStar.GetNode(192, 95));
                }
                else if (location.Name.Contains("2樓"))
                {
                    //51004
                    nodes.Add(map.CGAStar.GetNode(23, 23));
                }
                else if (location.Name.Contains("地下1樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(21, 12));
                }
                else if (location.Name.Contains("地下2樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(27, 12));
                }
                else if (location.Name.Contains("地下3樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(21, 6));
                }
                else if (location.Name.Contains("地下4樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(20, 2));
                }
                else if (location.Name.Contains("地下5樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(20, 3));
                }
                else if (location.Name.Contains("地下6樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(26, 2));
                }
                else if (location.Name.Contains("地下7樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(20, 2));
                }
                else if (location.Name.Contains("地下8樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(5, 9));
                }
                else if (location.Name.Contains("地下9樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(5, 3));
                }
                else if (location.Name.Contains("地下10樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(2, 27));
                }
                else if (location.Name.Contains("最下層"))
                {
                    //51017
                    nodes.Add(map.CGAStar.GetNode(97, 20));
                    //51018
                    nodes.Add(map.CGAStar.GetNode(92, 9));
                }

                GeneratePathFromList(map, locationNode, nodes, false, true);

                return true;
            }
            else if (location.Name.Contains("貝尼恰斯火山") && location.Name.Contains("地下"))
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Contains("地下1樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(46, 4));
                }
                else if (location.Name.Contains("地下2樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(161, 62));
                }
                else if (location.Name.Contains("地下3樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(34, 47));
                }
                else if (location.Name.Contains("地下4樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(61, 24));
                }
                else if (location.Name.Contains("地下5樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(17, 50));
                }
                else if (location.Name.Contains("地下6樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(72, 47));
                    nodes.Add(map.CGAStar.GetNode(21, 13));
                    nodes.Add(map.CGAStar.GetNode(26, 81));
                }
                else if (location.Name.Contains("地下7樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(40, 11));
                    nodes.Add(map.CGAStar.GetNode(77, 57));
                    nodes.Add(map.CGAStar.GetNode(20, 56));
                }
                else if (location.Name.Contains("地下8樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(18, 70));
                }
                else if (location.Name.Contains("地下9樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(53, 56));
                }
                else if (location.Name.Contains("地下10樓"))
                {
                    nodes.Add(map.CGAStar.GetNode(50, 21));
                }

                GeneratePathFromList(map, locationNode, nodes, false, true);

                return true;
            }
            else if (location.Name.Contains("深海"))
            {
                ArrayList nodes = new ArrayList();
                if (location.Name.Contains("第2層"))
                {
                    nodes.Add(map.CGAStar.GetNode(76, 54));
                }
                else if (location.Name.Contains("第3層"))
                {
                    nodes.Add(map.CGAStar.GetNode(73, 45));
                }
                else if (location.Name.Contains("第4層"))
                {
                    nodes.Add(map.CGAStar.GetNode(54, 55));
                }
                else if (location.Name.Contains("第5層"))
                {
                    nodes.Add(map.CGAStar.GetNode(57, 79));
                    nodes.Add(map.CGAStar.GetNode(63, 52));
                }
                else if (location.Name.Contains("第6層"))
                {
                    nodes.Add(map.CGAStar.GetNode(77, 84));
                    nodes.Add(map.CGAStar.GetNode(53, 60));
                }
                else if (location.Name.Contains("第7層"))
                {
                    nodes.Add(map.CGAStar.GetNode(65, 79));
                    nodes.Add(map.CGAStar.GetNode(78, 79));
                    nodes.Add(map.CGAStar.GetNode(29, 79));
                    nodes.Add(map.CGAStar.GetNode(50, 49));
                }

                GeneratePathFromList(map, locationNode, nodes, false, true);

                return true;
            }

            return false;
        }

        private void CircleMove()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (CircleNextLocation == null)
            {
                RecordLocation.NextCount = new Random().Next(0, 4);
                CircleNextLocation = Location.Next(RecordLocation, 2);
                Location.Move(hProcess, CircleNextLocation);
            }
            else if (LastState != 0 && LastState != MapState.STATE_MAP)
            {
                Common.Delay(new Random().Next(1500, 2500));
                Log.WriteLine("Battle end continue auto move");
                Location.Move(hProcess, CircleNextLocation);
            }
            else
            {
                Location location = Location.GetLocation(hProcess);
                int distance = Location.Distance(location, CircleNextLocation);
                if (distance <= 1 || CircleNextLocation.ExecuteCount > 2)
                {
                    CircleNextLocation = Location.Next(RecordLocation, new Random().Next(2, 4));
                    Location.Move(hProcess, CircleNextLocation);
                }
            }
        }

        private void RandomMove()
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            Location location = Location.GetLocation(hProcess);
            if (MapChanged(location))
            {
                return;
            }

            if (LastState != 0 && LastState == MapState.STATE_BATTLE)
            {
                if (Path.Nodes.Count > 0 || WalkingNodes.Count > 0)
                {
                    Log.WriteLine("Battle end, clear path");
                    Path = new MovePath();
                    WalkingNodes = new Stack<CGNode>();
                    return;
                }
            }

            if (WalkingNodes.Count > 0)
            {
                int distance = Location.Distance(location, WalkingNodes.Peek());
                if (distance >= 10)
                {
                    Path = new MovePath();
                    WalkingNodes = new Stack<CGNode>();
                    return;
                }
                else if (distance >= 2)
                {
                    CGNode moveNode = WalkingNodes.Peek();
                    if (moveNode != null)
                    {
                        Location moveLocation = new Location(moveNode);
                        moveLocation.NextCount = distance;
                        Location.Move(hProcess, moveLocation);
                    }
                }
            }

            if (Path.Nodes.Count > 0)
            {
                if (!string.IsNullOrEmpty(Path.Name) && !string.IsNullOrEmpty(location.Name)) {

                    if (!Path.Name.Contains("寶箱") && !Path.Name.Equals(location.Name) && !ExceptionNames.Contains(Path.Name))
                    {
                        Log.WriteLine("xxxxxxxxxxxx clear path");
                        Path = new MovePath();
                        WalkingNodes = new Stack<CGNode>();
                        return;
                    }
                }

                WalkingNodes = new Stack<CGNode>();
                WalkingNodes.Push(Path.Nodes.Dequeue());
                
                while (Path.Nodes.Count > 0)
                {
                    CGNode lastNode = WalkingNodes.Peek();
                    CGNode nextMoveNode = Path.Nodes.Peek();

                    if (lastNode.ImmobileNodes.Count > 0 || nextMoveNode.ImmobileNodes.Count > 0)
                    {
                        if (lastNode.X == nextMoveNode.X || lastNode.Y == nextMoveNode.Y)
                        {
                            bool line = true;
                            foreach (CGNode node in WalkingNodes)
                            {
                                if (node.X != nextMoveNode.X && node.Y != nextMoveNode.Y)
                                {
                                    line = false;
                                    break;
                                }
                            }

                            if (!line)
                            {
                                break;
                            }
                        }

                        WalkingNodes.Push(Path.Nodes.Dequeue());
                        if (WalkingNodes.Count >= 2)
                        {
                            break;
                        }
                    }
                    else
                    {
                        WalkingNodes.Push(Path.Nodes.Dequeue());
                        if (WalkingNodes.Count >= 5)
                        {
                            break;
                        }
                    }
                }

                CGNode moveNode = WalkingNodes.Peek();
                if (moveNode != null)
                {
                    Location moveLocation = new Location(moveNode);
                    moveLocation.NextCount = WalkingNodes.Count;
                    Location.Move(hProcess, moveLocation);
                }
            } 
            else
            {
                if (Path.EndNode != null && !string.IsNullOrEmpty(Path.Name) && Path.Name.Contains(location.Name))
                {
                    Location moveLocation = new Location(Path.EndNode);
                    if (Location.Distance(location, moveLocation) == 0)
                    {
                        moveLocation = new Location(Path.EndNode.X + 2 * (new Random().Next(0, 2)) - 1, Path.EndNode.Y + 2 * (new Random().Next(0, 2)) - 1);
                    }
                    Location.Move(hProcess, moveLocation);
                }
            }
        }

        private bool GeneratePathFromList(Map map, CGNode startNode, ArrayList nodes, bool useAdjacentNodes, bool useLocationName, bool enqueue = false)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            Location location = Location.GetLocation(hProcess);

            bool findPath = false;
            foreach (CGNode node in nodes)
            {
                if (startNode.IsSameLocation(node) || 
                    (Path.Nodes.Count != 0 && Path.EndNode != null && Path.EndNode.IsSameLocation(node)))
                {
                    return true;
                }

                if (Path.EndNode == null || !Path.EndNode.IsSameLocation(node))
                {
                    MovePath path = new MovePath
                    {
                        StartNode = startNode,
                        EndNode = node
                    };

                    if (useLocationName)
                    {
                        path.Name = location.Name;
                    }

                    if (GeneratePath(map, path, useAdjacentNodes, enqueue))
                    {
                        findPath = true;
                        break;
                    }
                }
            }

            return findPath;
        }

        private bool GeneratePath(Map map, MovePath path, bool useAdjacentNodes, bool enqueue = false)
        {
            IntPtr hWnd = Window.HandleWindow;
            int hProcess = Window.HandleProcess;

            if (path.StartNode == null || path.EndNode == null)
            {
                return false;
            }

            if (useAdjacentNodes)
            {
                List<CGNode> adjacentNodes = map.CGAStar.GetAdjacentNodes(path.EndNode).OrderBy(n => Location.Distance(path.StartNode, n)).ToList();
                //AStar.Shuffle(adjacentNodes);
                foreach (CGNode adjacentNode in adjacentNodes)
                {
                    if (!adjacentNode.Walkable)
                    {
                        continue;
                    }

                    Queue<CGNode> pathNodes = map.CGAStar.FindPath(path.StartNode, adjacentNode);
                    if (pathNodes == null)
                    {
                        map.CGAStar.ResetNode();
                    }
                    else
                    {
                        path.Nodes = ReversePath(pathNodes);
                        path.Nodes.Dequeue();

                        Path = path;
                        Console.WriteLine("New path count = " + path.Nodes.Count + " " + path.Name + path.EndNode);
                        return true;
                    }
                }
            } 
            else
            {
                Queue<CGNode> pathNodes = map.CGAStar.FindPath(path.StartNode, path.EndNode);
                if (pathNodes == null)
                {
                    map.CGAStar.ResetNode();
                }
                else
                {
                    path.Nodes = ReversePath(pathNodes);
                    path.Nodes.Dequeue();

                    if (enqueue)
                    {
                        Path.StartNode = path.StartNode;
                        Path.EndNode = path.EndNode;
                        Console.WriteLine("Enqueue path count = " + pathNodes.Count);
                        while (pathNodes.Count > 0)
                        {
                            Path.Nodes.Enqueue(pathNodes.Dequeue());
                        }
                    } 
                    else
                    {
                        Path = path;
                        Console.WriteLine("New path count = " + path.Nodes.Count + " " + path.Name + path.EndNode);
                    }
                    return true;
                }
            }

            return false;
        }

        private Queue<CGNode> ReversePath(Queue<CGNode> path)
        {
            Queue<CGNode> reversePath = new Queue<CGNode>();
            Stack<CGNode> stack = new Stack<CGNode>();

            while(path.Count > 0)
            {
                stack.Push(path.Dequeue());
            }

            while(stack.Count > 0)
            {
                reversePath.Enqueue(stack.Pop());
            }

            return reversePath;
        }

        private bool MapChanged(Location location)
        {
            return LastLocation == null || Location.MapChanged(location, LastLocation);
        }

        public void SetMazeMode(bool mode, MapType type)
        {
            MazeMode = mode;
            if (MazeMode)
            {
                MazeStairType = type;
            }
        }

        public bool GetMazeMode()
        {
            return MazeMode;
        }

        public void SetCircleMoveMode(bool mode)
        {
            CircleMoveMode = mode;
        }
    }
}
