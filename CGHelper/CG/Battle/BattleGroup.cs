using System.Collections;

namespace CGHelper.CG.Battle
{
    public class BattleGroup
    {
        public ArrayList List { get; set; } = new ArrayList();
        public int FrontNumber { get; set; }
        public int RearNumber { get; set; }

        public int RoleIndex { get; set; } = -1;

        public int PeyIndex { get; set; } = -1;

        public BattleTarget Role { get; set; }

        public ArrayList LV1List { get; set; } = new ArrayList();
    }
}
