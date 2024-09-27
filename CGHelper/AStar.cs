using CGHelper.CG.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CGHelper
{
    public class Node : Coordinate
    {
        public float G { get; set; }
        public float H { get; set; }

        public Node Parent { get; set; }

        public bool Walkable { get; set; }

        public static double GetDistance(Node current, Node target)
        {
            int dx = Math.Abs(current.X - target.X);
            int dy = Math.Abs(current.Y - target.Y);
            //Euclidean Distance
            return Math.Pow(Math.Pow(current.X - target.X, 2) + Math.Pow(current.Y - target.Y, 2), 0.5);
            //return 10 * Math.Sqrt(dx * dx + dy * dy);
            //Manhattan Distance
            //return Math.Abs(current.X - target.X) + Math.Abs(current.Y - target.Y);
            //return 10 * (dx + dy) + (14 - 2 * 10) * Math.Min(dx, dy);
            //Chebyshev distance
            //return Math.Max(Math.Abs(current.X - target.X), Math.Abs(current.Y - target.Y));
        }

        public Node(int x, int y) 
        {
            X = x;
            Y = y;
        }

        public bool IsSameLocation(Node node)
        {
            if (node == null)
            {
                return true;
            }

            return X == node.X && Y == node.Y;
        }

        public void Reset()
        {
            G = 0;
            H = 0;
            Parent = null;
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }

    public class AStar
    {
        List<List<Node>> Grid { get; set; } = new List<List<Node>>();

        public AStar()
        {

        }

        public AStar(List<List<Node>> grid)
        {
            Grid = grid;
        }

        public Node GetNode(int x, int y)
        {
            //Console.WriteLine(x + "," + y +  " GetNode => (" + Grid[y][x].X + ", " + Grid[y][x].Y + ")");
            if (y >= 0 && x >= 0 && y < Grid.Count && x < Grid[0].Count)
            {
                return Grid[y][x];
            }

            return null;
        }

        public void ResetNode()
        {
            foreach(List<Node> list in Grid)
            {
                foreach(Node node in list)
                {
                    node.Reset();
                }
            }
        }

        public Queue<Node> FindPath(Node start, Node end, bool sort = true)
        {
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            Node current = null;

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

                List<Node> adjacentNodes = GetAdjacentNodes(current);

                foreach (Node node in adjacentNodes)
                {
                    if (!closedList.Contains(node) && node.Walkable)
                    {
                        if (!openList.Contains(node))
                        {
                            node.Parent = current;
                            node.G = node.Parent.G + 1;
                            node.H = (float)Node.GetDistance(node, end);

                            openList.Add(node);
                            
                        } 
                        else
                        {
                            float G = node.Parent.G + 1;
                            float H = (float)Node.GetDistance(node, end);

                            if (node.G + node.H > G + H)
                            {
                                node.G = G;
                                node.H = H;
                            }
                        }
                    }
                }

                if (sort)
                {
                    float keySelector(Node n) => n.G + n.H;
                    openList = openList.OrderBy(keySelector).ToList();
                }
            }

            if (!closedList.Exists(node => node.IsSameLocation(end)))
            {
                return null;
            }

            Queue<Node> queue = new Queue<Node>();
            while (current != null)
            {
                queue.Enqueue(current);
                current = current.Parent;
            }

            return queue;
        }

        public List<Node> GetAdjacentNodes(Node node)
        {
            List<Node> adjacentNodes = new List<Node>();

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
                    
                    adjacentNodes.Add(Grid[node.Y + yShift][node.X + xShift]);
                }
            }

            return adjacentNodes;
        }

        public static void Shuffle(List<Node> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = new Random().Next(n + 1);
                Node value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
