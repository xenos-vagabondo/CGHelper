using CGHelper.CG.Base;

namespace CGHelper.CG
{
    public class Mouse : Coordinate
    {
        public Mouse()
        {
        }

        public Mouse(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Changed(Mouse point)
        {
            if ((point.X == X && point.Y == Y))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}
