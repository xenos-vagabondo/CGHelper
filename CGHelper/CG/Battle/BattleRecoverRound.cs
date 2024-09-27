namespace CGHelper.CG.Battle
{
    internal class BattleRecoverRound
    {
        public int Index { get; set; }
        public int Round { get; set; }

        public BattleRecoverRound(int index, int round)
        {
            Index = index;
            Round = round;
        }
    }
}
