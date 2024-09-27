using CommonLibrary;
using System.Collections;
using System;

namespace CGHelper.CG
{
    public class Mission
    {
        public static string[] EventMap = new string[] {
            "酒吧", "馮奴的家", "黑之祈禱",


            //彩葉草之戀
            "湯瑪斯長老家", "冰之神殿", "冰之神殿底層", "弗利德島", "火之谷底", "威爾森酒吧", "砂之塔頂", "商城三樓", "彩葉原", "墓室",


            "戰勝異魔族之后", "民家", "民家地下", 
            //失翼之龍
            "冥府之道", "黑之宮殿 2樓", "龍之沙漏", "深海 第1層", "深海 第7層", "寶座",
            //八等勳章
            "督府執政官室",
            //七等勳章
            "培里坎號 甲板", "少女庫魯魯的家", "海賊基地", "海賊基地 入口", "海賊頭目的房間", "湖",
            //六等勳章
            "客房", "太古之研究所 入口", "研究管理室",
            //五等勳章
            "謁見之間", "偵訊調查室", "阿凱魯法城地下", "大樹 5樓", "大樹 最下層", "漆黑之穴",
            "哥拉爾鎮 兵舍2樓", "夜晚的哥拉爾鎮", "卡希菈的豪宅", "執政官瓦吉的公館 休息室", "阿魯巴斯的研究室", "漢尼伯的家",

            //三等勳章
            "醫務室", "麥尼村", "麥尼洞窟", "病房", "保管庫入口", "保管庫", "麥尼洞窟 最下層", "伊姆爾森林",
            //二等勳章
            "米諾基亞鎮", "席琳的秘密住所", "馬杜克的船 甲板", "地底湖", "始祖之墓所 入口", "始祖之墓", "？？？", "深遠的黑暗",
            "千水鎮鎮長的家", "千水鎮旅館",
            //牛鬼的逆襲
            "峽之洞窟", "牛之殿堂",

            "沒落的村莊", "神籬", "佛利波羅傳送點",
            "阿卡斯傳送點",
            "神殿", "巴洛斯傳送點",
            "光之路",
            "魔族地牢",
            "商城密室",

            //帕魯凱斯的亡靈
            "牢房", "過去的哥拉爾城地下牢", "勇者里雍的房間", "過去的哥拉爾", "扎營處", "長老之家2樓", "伊姆爾森林 入口", "森之墓場",

            "民家", "水精靈酒吧", "食堂", "貝尼恰斯山頂上", "貝尼恰斯火山 最下層", "艾兒卡絲之家", "艾兒卡絲之家 2樓",
            "懲戒之間", "災禍之間", "真理之間", "地下圣堂", "岬之神殿最下層",

            "紳士淑女養成所",
            
            "洞穴", "大地鼠村", "地下王國", "邊境", "大地鼠王國覲見之間", "圖書室", "貝克里地底湖", "火焰鼠村長家"
        };

        public static void Event(GameWindow window)
        {
            IntPtr hWnd = window.HandleWindow;
            int hProcess = window.HandleProcess;

            Location location = Location.GetLocation(hProcess);
            if (string.IsNullOrEmpty(location.Name))
            {
                return;
            }

            Inventory inventory = Inventory.GetInventoryInfo(hProcess);

            if (location.Name.Contains("法蘭城"))
            {
                ArrayList injuredList = TeamInfo.GetInjuredTeamMember(hProcess);
                if (injuredList.Count == 0)
                {
                    
                    Item item = inventory.FuzzySearch("道場記憶");
                    //800001 800002
                    if (item != null && item.Id % 10 < 5)
                    {
                        if (Inventory.UseItem(hWnd, item))
                        {
                           Log.WriteLine(window.RoleName + " 使用 " + item.Name);
                        }
                    }
                }
            }

            if (location.Name.Contains("第") && location.Name.Contains("組通過"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                Location targetLocation = new Location(20, 12);
                int distance = Location.Distance(location, targetLocation);
                if (distance == 0)
                {
                    if (teamInfo.Member.Count == 0)
                    {
                        if (Common.RightButtonClickNPC(hWnd, new Location(targetLocation.X + 1, targetLocation.Y), 2000))
                        {
                            Log.WriteLine(window.RoleName + " click NPC");
                        }
                    }
                }
            }

            else if (location.Name.Equals("黑之祈禱"))
            {
                if (location.Code == 16509)
                {
                    if (!Common.ExpWindowShow(hWnd))
                    {
                        if (string.IsNullOrEmpty(Common.GetNPCMessage(hProcess)))
                        {
                            Item item = inventory.FuzzySearch("藥劑師就職引導");
                            if (item != null)
                            {
                                if (Inventory.UseItem(hWnd, item))
                                {
                                    Log.WriteLine(window.RoleName + " 使用 " + item.Name);
                                }
                            }
                        }
                    }
                }
                else
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count == 0)
                    {
                        Location NPCLocation = new Location(24, 32);
                        int distance = Location.Distance(location, NPCLocation);
                        if (distance == 1)
                        {
                            if (inventory.FuzzySearch("恐怖旅團之證") != null)
                            {
                                if (Common.RightButtonClickNPC(hWnd, NPCLocation, 750))
                                {
                                    Log.WriteLine(window.RoleName + " click NPC");
                                }
                            }
                        }
                    }
                }
            }
            else if (location.Name.Contains("黑色的祈禱"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (!teamInfo.TeamLeader)
                {
                    if (inventory.FuzzySearch("恐怖旅團之證") == null)
                    {
                        ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                        foreach (ActiveObject ao in activeObjectList)
                        {
                            if (ao.NPC && !string.IsNullOrWhiteSpace(ao.Name) && ao.Name.Equals("萌子"))
                            {
                                Location NPCLocation = new Location(ao.X, ao.Y);
                                int distance = Location.Distance(location, NPCLocation);
                                if (distance == 1)
                                {
                                    if (Common.RightButtonClickNPC(hWnd, NPCLocation, 500))
                                    {
                                        Log.WriteLine(window.RoleName + " click NPC 萌子");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (location.Name.Equals("索奇亞海底洞窟 地下2樓"))
            {
                TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                if (teamInfo.Member.Count == 0)
                {
                    Location NPCLocation = new Location(35, 7);
                    int distance = Location.Distance(location, NPCLocation);
                    if (distance == 1)
                    {
                        if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                        {
                            Log.WriteLine(window.RoleName + " click NPC");
                        }
                    }
                }
            }
            else if (location.Name.Contains("六曜之塔"))
            {
                Common.ClickConfirm(hProcess);
            }

            string msg = Common.GetNPCMessage(hProcess);

            if (string.IsNullOrEmpty(msg))
            {
                if (location.Name.Equals("湯瑪斯長老家") && location.X == 16 && location.Y == 11)
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(17, 11));
                    }
                }
                else if (location.Name.Equals("冰之神殿") && location.X == 39 && location.Y == 42)
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
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
                    else
                    {
                        int itemNumber = inventory.GetItemNumber("公主日記");
                        if (itemNumber == 0)
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(40, 42), 2000);
                        }
                    }
                }
                else if (location.Name.Equals("冰之神殿底層") && location.X == 13 && location.Y == 15)
                {
                    //32226
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
                    {
                        ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                        if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                        }
                        else
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(13, 16), 2000);
                        }
                    }
                    else
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(13, 16), 2000);
                    }
                }
                else if (location.Name.Equals("弗利德島") && location.X == 190 && location.Y == 207)
                {
                    int itemNumber = inventory.GetItemNumber("公主日記");
                    if (itemNumber == 1)
                    {
                        TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                        if (teamInfo.TeamLeader)
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
                        else if (teamInfo.Member.Count == 0)
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(191, 207), 2000);
                        }
                    }
                }
                else if (location.Name.Equals("火之谷底") && location.X == 14 && location.Y == 13)
                {
                    //32226
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
                    {
                        ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                        if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                        }
                        else
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(14, 14), 2000);
                        }
                    }
                    else
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(14, 14), 2000);
                    }
                }
                else if (location.Name.Equals("威爾森酒吧") && location.X == 47 && location.Y == 33)
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
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
                    else
                    {
                        int itemNumber = inventory.GetItemNumber("公主日記");
                        if (itemNumber == 2)
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(47, 32), 2000);
                        }
                    }
                }
                else if (location.Name.Equals("砂之塔頂") && location.X == 15 && location.Y == 13)
                {
                    //32229
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
                    {
                        ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                        if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                        }
                        else
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(15, 14), 2000);
                        }
                    }
                    else
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(15, 14), 2000);
                    }
                }
                else if (location.Name.Equals("商城三樓") && location.X == 17 && location.Y == 32)
                {
                    Item item = inventory.Search("彩葉原通行證");

                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
                    {
                        ArrayList activeObjectList = ActiveObject.GetObject(hProcess);
                        if (!TeamInfo.TeamMemberSameLocation(activeObjectList, location))
                        {
                            Common.PressKey(hWnd, System.Windows.Forms.Keys.F5);
                        }
                        else if (item == null)
                        {
                            Common.RightButtonClickNPC(hWnd, new Location(18, 32), 2000);
                        }
                    }
                    else if (item == null)
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(18, 32), 2000);
                    }
                }
                else if (location.Name.Equals("彩葉原") && location.X == 28 && location.Y == 58)
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.TeamLeader)
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
                    else
                    {
                        Common.RightButtonClickNPC(hWnd, new Location(28, 59), 2000);
                    }
                }
                else if (location.Name.Equals("過去的哥拉爾城地下牢"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count == 0)
                    {
                        Location NPCLocation = new Location(27, 12);
                        int distance = Location.Distance(location, NPCLocation);
                        if (distance == 1)
                        {
                            if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                            {
                                Log.WriteLine(window.RoleName + " click NPC");
                            }
                        }
                    }
                }
                else if (location.Name.Equals("勇者里雍的房間"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count == 0)
                    {
                        Location NPCLocation = new Location(10, 7);
                        int distance = Location.Distance(location, NPCLocation);
                        if (distance == 1)
                        {
                            if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                            {
                                Log.WriteLine(window.RoleName + " click NPC");
                            }
                        }
                    }
                }
                else if (location.Name.Equals("扎營處"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count == 0)
                    {
                        Location NPCLocation = new Location(24, 14);
                        int distance = Location.Distance(location, NPCLocation);
                        if (distance == 1)
                        {
                            if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                            {
                                Log.WriteLine(window.RoleName + " click NPC");
                            }
                        }
                    }
                }

                else if (location.Name.Equals("雪山小屋"))
                {
                    Item item = inventory.FuzzySearch("菇");

                    if (item != null)
                    {
                        ActiveObject ob = ActiveObject.FindNPC(hProcess, "廚師沙杰弗");
                        if (ob != null)
                        {
                            Location NPCLocation = new Location(ob.X, ob.Y);
                            int distance = Location.Distance(location, NPCLocation);
                            if (distance == 1)
                            {
                                if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                                {
                                    Log.WriteLine(window.RoleName + " click NPC");
                                }
                            }
                        }
                    }
                }
                else if (location.Name.Equals("戰勝異魔族之后"))
                {
                    TeamInfo teamInfo = TeamInfo.GetTeamInfo(hProcess);
                    if (teamInfo.Member.Count == 0)
                    {
                        ActiveObject ob = ActiveObject.FindNPC(hProcess, "異魔族隊長");
                        if (ob != null)
                        {
                            Location NPCLocation = new Location(ob.X, ob.Y);
                            int distance = Location.Distance(location, NPCLocation);
                            if (distance == 1)
                            {
                                if (Common.RightButtonClickNPC(hWnd, NPCLocation, 1000))
                                {
                                    Log.WriteLine(window.RoleName + " click NPC");
                                }
                            }
                        }
                    }
                }

                return;
            }

            if (new ArrayList(EventMap).IndexOf(location.Name) != -1)
            {
                Common.ClickConfirm(hProcess);
            }

            if (msg.Contains("使用后可以提高人物等級1級，確定要使用嗎？") ||
                msg.Contains("等級成功提升"))
            {
                Common.ClickConfirm(hProcess);
            }
            else if ("法蘭城".Equals(location.Name) && location.X == 241 && location.Y == 57)
            {
                Common.ClickConfirm(hProcess);
            }
            //巫師就職
            else if (location.Name.Contains("索奇亞海底洞窟") || location.Name.Contains("黑色的祈禱地下"))
            {
                Common.ClickConfirm(hProcess);
            }
            //雪山小屋
            else if (msg.Contains("你發現莎蓮娜磨菇了是吧！") || msg.Contains("請接受我的謝禮吧！"))
            {
                Common.ClickConfirm(hProcess);
            }
            //百人
            else if (msg.Contains("恭喜你通過了本組挑戰") || msg.Contains("要使用記憶嗎？") || msg.Contains("你想要把") || msg.Contains("你要兌換的是"))
            {
                Common.ClickConfirm(hProcess);
            }
            //純水晶
            else if (msg.Contains("試著將純粹的地的水晶帶來洞窟里吧？") ||
                msg.Contains("獻給這個洞窟嗎？") ||
                msg.Contains("我會給你謝禮的。") ||
                msg.Contains("我會給你一點小東西當謝禮。"))
            {
                Common.ClickConfirm(hProcess);
            }
            //蘭國第七等勳章
            else if (msg.Contains("第八等勳章受勳者嗎...。") ||
                msg.Contains("在下是侍奉蘭國某位大人的屬下。") ||
                msg.Contains("現在蘭國國內有點兵荒馬亂") ||
                msg.Contains("答應在下的請求了嗎？") ||
                msg.Contains("ZZzZzzz...") ||
                msg.Contains("如同仰望悲傷的夜晚而變貌的花正綻放著。") ||
                msg.Contains("這麼說來，降臨在這世界上已經過幾天了呢...") ||
                msg.Contains("想著想著東方的天空升起了月亮。") ||
                msg.Contains("月之露水就是這個沒錯了。") ||
                msg.Contains("彈奏了仲時的琵琶。") ||
                msg.Contains("我等你來了。") ||
                msg.Contains("什麼！少主的對象是艾爾巴尼亞的王女！") ||
                msg.Contains("這次你們的努力，非常值得稱贊。") ||
                msg.Contains("抱歉，請把公主平安無事的消息告訴仲時大人好嗎？") ||
                msg.Contains("要使用錢袋嗎？"))
            {
                Common.ClickConfirm(hProcess);
            }
            //六等勳章
            else if (location.Name.Contains("地下遺跡") ||
                msg.Contains("你們也發現到這里了呀？"))
            {
                Common.ClickConfirm(hProcess);
            }
            //五等勳章
            else if (msg.Contains("你不是蘇之國的人吧？"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("跟我比劃一下比較快取得綠頭盔"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if ("利夏島".Equals(location.Name))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("辛苦你了。只要有這些就能讓時波的勾玉再次復蘇。") ||
                msg.Contains("既然如此我就只好用力量來奪取了"))
            {
                Common.ClickNegative(hProcess);
            }

            else if (msg.Contains("能通過這里的是只有接受過我們阿斯提亞神官的") ||
                msg.Contains("如果你有救國救民的意志，就去和神殿的祭司大人見個面吧！") ||
                msg.Contains("試煉之道上有很多魔族，你可要小心。") ||
                msg.Contains("等你好久了，布魯梅爾大人正在等你。"))
            {

                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("燃燒著不可思議的火。") ||
                msg.Contains("要對火把點火嗎？") ||
                msg.Contains("火把上燃燒著青白色的火焰。") ||
                msg.Contains("已經點過火了。") ||
                msg.Contains("冰壁上有個洞，還有個燭臺。") ||
                msg.Contains("要用手上的火把去燒看看嗎？") ||
                msg.Contains("火把上的火消失了。墻上的冰融化後出現了一條路。"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("前面是往魯米那斯村，但有不少兇暴的魔族出沒"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("請多加小心。"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("腥臭的氣味彌漫了整口井。要下去看看嗎？"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("你想要和我說話嗎？") ||
                msg.Contains("非常樂意呀。今天就來談一些事吧……") ||
                msg.Contains("真是奇怪，我明明就報告說發現魔族") ||
                msg.Contains("索拉梅大人并不是喝酒的那個人呀") ||
                msg.Contains("肚皮舞不行嗎？這樣的話……") ||
                msg.Contains("呵呵，首先我就先讓你看看什麼叫做「那個」。") ||
                msg.Contains("我先去準備一下再過去。等等喔"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("成為了執政官，工作一定會更加疲憊") ||
                msg.Contains("這樣沒錯吧。比起用布偶玩腹語"))
            {
                Common.ClickNegative(hProcess);
            }

            //三等勳章
            else if (msg.Contains("最近常常有罹患怪病的患者被送過來。") ||
                msg.Contains("謝謝你，醫生他正在醫務室。醫務室往這邊走。") ||
                msg.Contains("你回來了呀，找到麥麥草了對吧！") ||
                msg.Contains("席琳小姐的話，馬上就可以出院羅。"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("喔喔！這不是開啟者嗎？") ||
                msg.Contains("太棒了！這樣一來時空之門一定能被打開了吧！"))
            {
                Common.ClickConfirm(hProcess);
            }

            else if (msg.Contains("嗚嗚嗚嗚嗚嗚!我的羽衣不見了......") ||
                msg.Contains("橋對面的那個人好象很可疑...") ||
                msg.Contains("河對岸的肯定是個仙女") ||
                msg.Contains("羽衣在他手上,他不愿給你") ||
                msg.Contains("一切就拜托你了") ||
                msg.Contains("織女與牛郎的這段姻緣前世就已經注定了,只是后路坎坷啊") ||
                msg.Contains("感謝你幫我去向月老進行求證") ||
                msg.Contains("我早就知道了,她想家了") ||
                msg.Contains("那么一切就拜托你了") ||
                msg.Contains("你來晚了!牛郎與織女被處罰天各一方") ||
                msg.Contains("感謝你幫助我和織女的相聚") ||
                msg.Contains("感謝你幫助我和牛郎的相聚"))
            {
                Common.ClickConfirm(hProcess);
            }
        }
    }
}
