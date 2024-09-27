using CGHelper.CG.Enum;
using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace CGHelper.CG
{
    public class Map
    {
        private static Map INSTANCE { get; set; } = new Map();

        private static object LockObject { get; set; } = new object();

        public static Dictionary<int, Size> MapImmobile { get; set; } = new Dictionary<int, Size>();

        public byte[,] Data { get; set; }
        public Size Size { get; set; } = new Size();
        public string Name { get; set; }
        public int Code { get; set; }
        public bool RandomMap { get; set; }

        public int WalkableFlag { get; set; }

        public List<CGNode> ChangeMapNode { get; set; } = new List<CGNode>();

        public CGAStar CGAStar { get; set; } = new CGAStar();
        public List<CGNode> WalkableNodes { get; set; } = new List<CGNode>();

        public bool IsSame(Map map)
        {
            if (map == null || !Size.Equals(map.Size))
            {
                return false;
            }

            for (int x = 0; x < Size.Width; x++)
            {
                for (int y = 0; y < Size.Height; y++)
                {
                    if (Data[x,y] != map.Data[x,y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsRandomMap(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.RandomMapAddr, out int randomMap, 4, 0);

            return randomMap == 1 ? true : false;
        }

        public static int GetMapCode(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.MapCodeAddr, out int mapCode, 4, 0);
            return mapCode;
        }

        public static int GetMapWalkableFlag(int hProcess)
        {
            WinAPI.ReadProcessMemory(hProcess, CGAddr.MapWalkableFlagAddr, out int flag, 4, 0);
            return flag;
        }

        public static FileInfo GetMapFilePath(int hProcess)
        {
            string exeFilePath = Common.GetExeFilePath(hProcess);
            if (string.IsNullOrEmpty(exeFilePath))
            {
                return null;
            }

            string mapFilePath = "map\\";
            if (IsRandomMap(hProcess))
            {
                WinAPI.ReadProcessMemory(hProcess, CGAddr.RandomMapCodeAddr, out int randomMapCode, 4, 0);
                mapFilePath += "1\\" + randomMapCode + "\\";
            }
            else
            {
                mapFilePath += "0\\";
            }
            WinAPI.ReadProcessMemory(hProcess, CGAddr.MapCodeAddr, out int mapCode, 4, 0);

            mapFilePath += mapCode + ".dat";
            //Console.WriteLine(mapFilePath);

            return new FileInfo(Path.Combine(exeFilePath, mapFilePath));
        }

        public static void GetMapInfoFromBinFiles(string exeFilePath)
        {
            if (MapImmobile.Count != 0)
            {
                Console.WriteLine("GetMapInfoFromBinFiles already done");
                return;
            }

            IEnumerable<string> files = Directory.EnumerateFiles(exeFilePath, "GraphicInfo*.bin", SearchOption.AllDirectories);
            foreach (string filePath in files)
            {
                FileInfo infoFile = new FileInfo(filePath);
                if (!infoFile.Exists)
                {
                    continue;
                }

                Console.WriteLine(infoFile);
                using (FileStream fileStream = infoFile.OpenRead())
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        while (fileStream.Position < fileStream.Length)
                        {
                            int index = binaryReader.ReadInt32();
                            uint addr = binaryReader.ReadUInt32();
                            uint dataLength = binaryReader.ReadUInt32();
                            int offsetX = binaryReader.ReadInt32();
                            int offsetY = binaryReader.ReadInt32();
                            int sizeWidth = binaryReader.ReadInt32();
                            int sizeHeight = binaryReader.ReadInt32();
                            byte sizeX = binaryReader.ReadByte();
                            byte sizeY = binaryReader.ReadByte();
                            //fs.Seek(30, SeekOrigin.Current);
                            byte walkable = binaryReader.ReadByte();

                            byte unknow1 = binaryReader.ReadByte();
                            byte unknow2 = binaryReader.ReadByte();
                            byte unknow3 = binaryReader.ReadByte();
                            byte unknow4 = binaryReader.ReadByte();
                            byte unknow5 = binaryReader.ReadByte();
                            //fs.Seek(5, SeekOrigin.Current);
                            int mapId = binaryReader.ReadInt32();
                            if (mapId != 0)
                            {
                                /*
                                Console.WriteLine(index + " 0x" + addr.ToString("X") + " " + dataLength
                                    + " offsetX " + offsetX + " offsetY " + offsetY
                                    + " sizeWidth " + sizeWidth + " sizeHeight " + sizeHeight
                                    + " sizeX " + sizeX + " sizeY " + sizeY
                                    + " unknow " + unknow1.ToString("X") + " " + unknow2.ToString("X") + " " + unknow3.ToString("X") + " " + unknow4.ToString("X") + " " + unknow5.ToString("X")
                                    + " " + mapId + " " + walkable);
                                */
                            }

                            if (mapId == 0)
                            {
                                continue;
                            }

                            if (walkable == 1 || walkable == 2 || walkable == 45)
                            {
                                continue;
                            }

                            if (!MapImmobile.ContainsKey(mapId))
                            {
                                MapImmobile.Add(mapId, new Size(sizeX, sizeY));
                            } 
                            else
                            {
                                if (MapImmobile[mapId].Width != sizeX || MapImmobile[mapId].Height != sizeY)
                                {
                                    Size newSize = new Size(sizeX, sizeY);
                                    //Console.WriteLine(mapId + " " + MapImmobile[mapId] + " => new size " + newSize);
                                    MapImmobile.Remove(mapId);
                                    MapImmobile.Add(mapId, newSize);
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("GetMapInfoFromBinFiles done");
        }

        public static Map GetMap(int hProcess, bool useInstance = false)
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    FileInfo mapFile = GetMapFilePath(hProcess);
                    if (mapFile != null && mapFile.Exists)
                    {
                        Map map = new Map();
                        map.Code = GetMapCode(hProcess);
                        map.WalkableFlag = GetMapWalkableFlag(hProcess);

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (FileStream fileStream = mapFile.OpenRead())
                            {
                                fileStream.CopyTo(memoryStream);
                            }

                            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                            {
                                LoadMap(map, memoryStream, binaryReader);
                            }
                        }
                    }
                }
                catch
                {
                    Log.WriteLine("load map error!!!");
                }
                finally
                {
                    Monitor.Exit(LockObject);
                }
            }

            return INSTANCE;
        }

        public static Map GetMap(int hProcess)
        {
            Map map = new Map();
            map.Code = GetMapCode(hProcess);
            map.WalkableFlag = GetMapWalkableFlag(hProcess);

            FileInfo mapFile = GetMapFilePath(hProcess);
            if (mapFile != null && mapFile.Exists)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    try
                    {
                        using (FileStream fileStream = mapFile.OpenRead())
                        {
                            fileStream.CopyTo(memoryStream);
                        }

                        using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                        {
                            LoadMap(map, memoryStream, binaryReader);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("load map error!!!\n" + e);
                    }
                }
            }

            return map;
        }

        public static void LoadMap(Map map, MemoryStream memoryStream, BinaryReader binaryReader)
        {
            int width, height, sectionOffset;
            //首20字節為檔頭，檔頭的頭3字節為固定字符MAP，隨後9字節均為0/空白，
            //然後為分別2個DWORD(4字節)的數據，第1個表示地圖長度 - 東(W)，第2個表示地圖長度 - 南(H)。
            memoryStream.Seek(12, SeekOrigin.Begin);
            width = binaryReader.ReadInt32();
            height = binaryReader.ReadInt32();
            sectionOffset = width * height * 2;

            map.Data = new byte[width, height];
            map.Size = new Size(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    memoryStream.Seek(20, SeekOrigin.Begin);
                    //隨後W*H*2字節為地面數據，每2字節為1數據塊，表示地面的地圖編號，以製成基本地形。
                    memoryStream.Seek((x + y * width) * 2, SeekOrigin.Current);
                    ushort ground = binaryReader.ReadUInt16();

                    //再隨後W*H*2字節為地上物件/建築物數據，每2字節為1數據塊，表示該點上的物件/建築物地圖編號。
                    memoryStream.Seek(-2, SeekOrigin.Current);
                    memoryStream.Seek(sectionOffset, SeekOrigin.Current);
                    ushort buildingObject = binaryReader.ReadUInt16();

                    //再隨後 W*H*2 字節為地圖標誌，每 2 字節為 1 數據塊
                    memoryStream.Seek(-2, SeekOrigin.Current);
                    memoryStream.Seek(sectionOffset, SeekOrigin.Current);
                    byte changeMap = binaryReader.ReadByte();
                    byte explored = binaryReader.ReadByte();

                    if (explored == 0)
                    {
                        map.Data[x, y] = (byte)MapType.FOG;
                    }
                    else if (changeMap == 3 || changeMap == 10)
                    {
                        //Console.WriteLine(x + "," + y + " changeMap = " + changeMap + " ground = " + ground + " buildingObject = " + buildingObject);
                        map.Data[x, y] = (byte)GetStairType(buildingObject);
                    }
                    else if (changeMap != 0)
                    {
                        if (changeMap == 2 || changeMap == 5)
                        {
                            //怪物?
                        }
                        else if (changeMap == 11)
                        {
                            //待售房屋傳點?
                        }
                        else
                        {
                            Console.WriteLine(x + "," + y + " changeMap = " + changeMap + " buildingObject = " + buildingObject + " type =" + (byte)GetStairType(buildingObject));
                        }
                        map.Data[x, y] = (byte)MapType.IMMOBILE;
                    }
                    else if (map.WalkableFlag == 1)
                    {
                        if (explored == 0xC0)
                        {
                            map.Data[x, y] = (byte)MapType.WALKABLE;
                        }
                        else if (explored == 0xC1)
                        {
                            map.Data[x, y] = (byte)MapType.IMMOBILE;
                        }
                    }
                    else
                    {
                        if (ground == 0 && buildingObject == 0)
                        {
                            map.Data[x, y] = (byte)MapType.IMMOBILE;
                        }
                        else
                        {
                            int exGround = ground >= 20000 ? ground + 200000 : ground;
                            int exBuilding = buildingObject >= 30000 ? buildingObject + 200000 : buildingObject;

                            if (!CheckImmobileSize(map, exGround, x, y))
                            {
                                if (map.Data[x, y] != (byte)MapType.IMMOBILE)
                                {
                                    map.Data[x, y] = (byte)MapType.WALKABLE;
                                }
                            }
                            CheckImmobileSize(map, exBuilding, x, y);
                        }
                    }
                }
            }
            INSTANCE = map;
        }

        private static MapType GetStairType(ushort buildingObject)
        {
            switch (buildingObject)
            {
                case 12000:
                case 12001:
                case 13268:
                case 13270:
                case 13272:
                case 13274:
                case 13996:
                case 13998:
                case 15561:
                case 15887:
                case 15889:
                case 15891:
                case 17952:
                case 17954:
                case 17956:
                case 17958:
                case 17960:
                case 17962:
                case 17964:
                case 17966:
                case 17968:
                case 17970:
                case 17972:
                case 17974:
                case 17976:
                case 17978:
                case 17980:
                case 17982:
                case 17984:
                case 17986:
                case 17988:
                case 17990:
                case 17992:
                case 17994:
                case 17996:
                case 17998:
                case 16610:
                case 16611:
                case 16626:
                case 16627:
                case 16628:
                case 16629:
                    return MapType.STAIR_UP;

                case 12002:
                case 12003:
                case 13269:
                case 13271:
                case 13273:
                case 13275:
                case 13997:
                case 13999:
                case 15562:
                case 15888:
                case 15890:
                case 15892:
                case 17953:
                case 17955:
                case 17957:
                case 17959:
                case 17961:
                case 17963:
                case 17965:
                case 17967:
                case 17969:
                case 17971:
                case 17973:
                case 17975:
                case 17977:
                case 17979:
                case 17981:
                case 17983:
                case 17985:
                case 17987:
                case 17989:
                case 17991:
                case 17993:
                case 17995:
                case 17997:
                case 17999:
                case 16612:
                case 16613:
                case 16614:
                case 16615:
                    return MapType.STAIR_DOWN;

                case 0:
                    //return Type.MONSTER;
                case 14676:
                default:
                    return MapType.TRANSPORT;
            }
        }

        public void AnalyzeNodes()
        {
            if (Data == null)
            {
                return;
            }

            CGAStar.SetData(Data);

            for (int x = 0; x < Size.Width; x++)
            {
                for (int y = 0; y < Size.Height; y++)
                {
                    if (Data[x, y] == (byte)MapType.FOG || Data[x, y] == (byte)MapType.IMMOBILE)
                    {
                        continue;
                    }

                    CGNode node = CGAStar.GetNode(x, y);
                    if (node != null)
                    {
                        if (Data[x, y] == (byte)MapType.STAIR_UP || Data[x, y] == (byte)MapType.STAIR_DOWN || Data[x, y] == (byte)MapType.TRANSPORT)
                        {
                            ChangeMapNode.Add(node);
                        }

                        WalkableNodes.Add(node);
                    }
                }
            }
        }

        public bool AdjacentFog(int x, int y)
        {
            for (int i = -1; i <= 1; i++)
            {
                if (x + i < 0 || x + i >= Size.Width)
                {
                    continue;
                }

                for (int j = -1; j <= 1; j++)
                {
                    if (y + j < 0 || y + j >= Size.Height)
                    {
                        continue;
                    }

                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    if (Data[x + i, y + j] == (byte)MapType.FOG)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckImmobileSize(Map map, int key, int x, int y)
        {
            if (!MapImmobile.ContainsKey(key))
            {
                return false;
            }

            Size size = MapImmobile[key];
            for (int i = 0; i < size.Width; i++)
            {
                for (int j = 0; j < size.Height; j++)
                {
                    if (x + i >= map.Size.Width || y - j < 0)
                    {
                        continue;
                    }
                    map.Data[x + i, y - j] = (byte)MapType.IMMOBILE;
                }
            }

            return true;
        }
    }
}
