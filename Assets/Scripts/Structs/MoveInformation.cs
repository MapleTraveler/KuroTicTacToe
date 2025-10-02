using Enums;

namespace Structs
{
    public readonly struct MoveInformation
    {
        public readonly int Row, Col;
        public readonly ESide ESide;

        public MoveInformation(int r, int c, ESide s)
        {
            Row = r;
            Col = c;
            ESide = s;
        }
    }
}