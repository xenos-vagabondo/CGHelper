using CGHelper.CG.Base;
using CommonLibrary;
using System;

namespace CGHelper.CG
{
    public class Location : Coordinate
    {
        public string Name { get; set; }
        public int Code { get; set; }
        public int NextCount { get; set; }
        public int ReverseCount { get; set; }
        public bool Reverse { get; set; }
        public Location Last { get; set; }
        public int ExecuteCount { get; set; }

        public Location() { }

        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Location(Node node)
        {
            X = node.X;
            Y = node.Y;
        }

        public static string GetName(int hProcess)
        {
            return Common.GetNameFromAddr(hProcess, CGAddr.MapAddr);
        }

        public static Location GetLocation(int hProcess)
        {
            byte[] xBuffer = new byte[4];
            WinAPI.ReadProcessMemory(hProcess, CGAddr.XAddr, xBuffer, 4, 0);
            byte[] yBuffer = new byte[4];
            WinAPI.ReadProcessMemory(hProcess, CGAddr.YAddr, yBuffer, 4, 0);

            return new Location
            {
                Name = GetName(hProcess),
                Code = Map.GetMapCode(hProcess),
                X = (int)(BitConverter.ToSingle(xBuffer, 0) / 64),
                Y = (int)(BitConverter.ToSingle(yBuffer, 0) / 64)
            };
        }

        public static bool MapChanged(Location newLocation, Location oldLocation)
        {
            if (newLocation != null 
                && oldLocation != null 
                && !string.IsNullOrEmpty(newLocation.Name) 
                && !string.IsNullOrEmpty(oldLocation.Name)
                && newLocation.Name.Equals(oldLocation.Name)
                && newLocation.Code == oldLocation.Code)
            {
                return false;
            }

            return true;
        }

        public static void Move(int hProcess, int x, int y)
        {
            Move(hProcess, new Location(x, y));
        }

        public static void Move(int hProcess, Location moveLocation, int delayTime = 500)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.RoleNameAddr + 0x68, out int speed, 4, 0);

            Location location = GetLocation(hProcess);
            int distance = Distance(location, moveLocation);

            if (distance > 0)
            {
                WinAPI.WriteProcessMemory(hProcess, CGAddr.MoveXAddr + 0x4, BitConverter.GetBytes(moveLocation.X), 4, 0);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.MoveXAddr + 0x8, BitConverter.GetBytes(0), 4, 0);

                WinAPI.WriteProcessMemory(hProcess, CGAddr.MoveYAddr + 0x4, BitConverter.GetBytes(moveLocation.Y), 4, 0);
                WinAPI.WriteProcessMemory(hProcess, CGAddr.MoveYAddr + 0x8, BitConverter.GetBytes(0), 4, 0);

                WinAPI.WriteProcessMemory(hProcess, CGAddr.MoveAddr, BitConverter.GetBytes(0x1), 4, 0);

                moveLocation.ExecuteCount++;
                //int delayTime = Math.Min(new Random().Next(75, 100) * distance, 1000);
                delayTime = distance <= 2 ? (int)((float)25 / speed * 100) * distance : (int)((float)175 / speed * 100) * distance;

                /*
                if (moveLocation.NextCount == 0)
                {
                }
                else if (moveLocation.NextCount > 0)
                {
                    delayTime = (int)((float)200 / speed * 100) * moveLocation.NextCount - 100;
                }
                else
                {
                    delayTime = (int)((float)300 / speed * 100) * moveLocation.NextCount - 100;
                }
                */

                //Log.WriteLine("[" + moveLocation.NextCount + "] " + location.ToString() + " Move to " + moveLocation.ToString() + " distance = " + distance + " delay = " + delayTime);
                Log.WriteLine("[" + moveLocation.NextCount + "] " + location.ToString() + " Move to " + moveLocation.ToString() + " distance = " + distance + "(" + moveLocation.NextCount + ") delay = " + delayTime);
                Common.Delay(delayTime);
            }
        }

        public static Location Next(Location location, int max)
        {
            Location nextLocation = new Location();
            Random random = new Random();

            if (location.NextCount == 0 || location.ReverseCount == 0)
            {
                location.Reverse = false;
                location.ReverseCount = random.Next(100, 200);
            }

            int min = 1;
            if (location.NextCount % 4 == 0)
            {
                nextLocation.X = location.X + random.Next(min, max);
                nextLocation.Y = location.Y - random.Next(min, max);
            }
            else if (location.NextCount % 4 == 1)
            {
                nextLocation.X = location.X - random.Next(min, max);
                nextLocation.Y = location.Y - random.Next(min, max);
            }
            else if (location.NextCount % 4 == 2)
            {
                nextLocation.X = location.X - random.Next(min, max);
                nextLocation.Y = location.Y + random.Next(min, max);
            }
            else if (location.NextCount % 4 == 3)
            {
                nextLocation.X = location.X + random.Next(min, max);
                nextLocation.Y = location.Y + random.Next(min, max);
            }

            if (location.NextCount > location.ReverseCount)
            {
                location.Reverse = true;
            }

            if (location.Reverse)
            {
                --location.NextCount;
            } 
            else
            {
                ++location.NextCount;
            }

            location.Last = null;
            nextLocation.Last = location;
            nextLocation.NextCount = location.NextCount;

            return nextLocation;
        }

        public static int Distance(Node a, Node b)
        {
            return Distance(new Location(a.X, a.Y), new Location(b.X, b.Y));
        }

        public static int Distance(Location a, Node b)
        {
            return Distance(a, new Location(b.X, b.Y));
        }

        public static int Distance(Node a, Location b)
        {
            return Distance(new Location(a.X, a.Y), b);
        }

        public static int Distance(Location a, Location b)
        {
            //return (int)Math.Pow(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2), 0.5);
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        public override string ToString()
        {
            return Name + "(" + X + "," + Y + ")";
        }
    }
}
