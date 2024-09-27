using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CGHelper.CG
{
    public class CGNode : Node
    {
        public CGNode(int x, int y) : base(x, y)
        {
            X = x;
            Y = y;
        }

        public byte Type { get; set; }

        public ArrayList ImmobileNodes { get; set; } = new ArrayList();

        public new CGNode Parent { get; set; }
    }
    public class CGAStar : AStar
    {
        List<List<CGNode>> Grid { get; set; } = new List<List<CGNode>>();

        public void SetData(byte[,] map)
        {
            Grid = new List<List<CGNode>>();
            for (int y = 0; y < map.GetLength(1); y++)
            {
                List<CGNode> list = new List<CGNode>();
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    CGNode node = new CGNode(x, y)
                    {
                        Type = map[x, y],
                        Walkable = map[x, y] > 1
                    };
                    list.Add(node);
                }
                Grid.Add(list);
            }
        }

        public CGNode GetNode(Location location)
        {
            return GetNode(location.X, location.Y);
        }

        public new CGNode GetNode(int x, int y)
        {
            //Console.WriteLine(x + "," + y +  " GetNode => (" + Grid[y][x].X + ", " + Grid[y][x].Y + ")");
            if (y >= 0 && x >= 0 && y < Grid.Count && x < Grid[0].Count)
            {
                return Grid[y][x];
            }

            return null;
        }

        public new void ResetNode()
        {
            foreach (List<CGNode> list in Grid)
            {
                foreach (CGNode node in list)
                {
                    node.Reset();
                }
            }
        }

        public Queue<CGNode> FindPath(CGNode start, CGNode end, bool sort = true)
        {
            List<CGNode> openList = new List<CGNode>();
            List<CGNode> closedList = new List<CGNode>();
            CGNode current = null;

            openList.Add(start);

            while (openList.Count != 0)
            {
                if (closedList.Exists(node => node.IsSameLocation(end)))
                {
                    break;
                }

                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);

                List<CGNode> adjacentNodes = GetAdjacentNodes(current);

                foreach (CGNode node in adjacentNodes)
                {
                    if (!node.Walkable)
                    {
                        current.ImmobileNodes.Add(node);
                    }
                }

                if (current.ImmobileNodes.Count >= 4)
                {
                    current.G += (float)0.1;
                }

                foreach (CGNode node in adjacentNodes)
                {
                    if (!closedList.Contains(node) && node.Walkable)
                    {
                        if (!openList.Contains(node))
                        {
                            node.Parent = current;
                            node.G = node.Parent.G + 1;
                            node.H = (float)Node.GetDistance(node, end);

                            if (node.X != current.X && node.Y != current.Y)
                            {
                                node.H += 1;
                            }

                            openList.Add(node);

                        }
                        else
                        {
                            float G = node.Parent.G + 1;
                            float H = (float)Node.GetDistance(node, end);

                            if (node.X != current.X && node.Y != current.Y)
                            {
                                H += 1;
                            }

                            if (node.G + node.H > G + H)
                            {
                                //Console.WriteLine("update node" + node + " " + (node.G + node.H) + " , " + (G + H));
                                //node.G = G;
                                //node.H = H;
                            }
                        }

                        if (node.Type > 2)
                        {
                            node.G += 1;
                        }
                    }
                }

                if (sort)
                {
                    float keySelector(CGNode n) => n.G + n.H;
                    openList = openList.OrderBy(keySelector).ToList();
                }
            }

            if (!closedList.Exists(node => node.IsSameLocation(end)))
            {
                return null;
            }

            Queue<CGNode> queue = new Queue<CGNode>();
            while (current != null)
            {
                queue.Enqueue(current);
                current = current.Parent;
            }

            return queue;
        }

        public List<CGNode> GetAdjacentNodes(CGNode node)
        {
            List<CGNode> adjacentNodes = new List<CGNode>();

            /*
            if (node.Y - 1 >= 0)
            {
                adjacentNodes.Add(Grid[node.Y - 1][node.X]);
            }
            if (node.Y + 1 < Grid.Count)
            {
                adjacentNodes.Add(Grid[node.Y + 1][node.X]);
            }
            if (node.X - 1 >= 0)
            {
                adjacentNodes.Add(Grid[node.Y][node.X - 1]);
            }
            if (node.X + 1 < Grid[0].Count)
            {
                adjacentNodes.Add(Grid[node.Y][node.X + 1]);
            }
            */


            for (int yShift = -1; yShift <= 1; yShift++)
            {
                if (node.Y + yShift < 0 || node.Y + yShift >= Grid.Count)
                {
                    continue;
                }

                for (int xShift = -1; xShift <= 1; xShift++)
                {
                    if (node.X + xShift < 0 || node.X + xShift >= Grid[0].Count)
                    {
                        continue;
                    }

                    if (yShift == 0 && xShift == 0)
                    {
                        continue;
                    }

                    if (yShift != 0 && xShift != 0)
                    {
                        if (Grid[node.Y + yShift][node.X].Walkable && Grid[node.Y][node.X + xShift].Walkable) {
                            adjacentNodes.Add(Grid[node.Y + yShift][node.X + xShift]);
                        }
                    }
                    else
                    {
                        adjacentNodes.Add(Grid[node.Y + yShift][node.X + xShift]);
                    }
                }
            }

            return adjacentNodes;
        }
    }
}
