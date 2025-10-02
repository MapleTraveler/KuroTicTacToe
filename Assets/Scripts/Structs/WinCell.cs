namespace Structs
{
    public readonly struct WinCell
    {
        public readonly int Row, Col;

        public WinCell(int r, int c)
        {
            Row = r;
            Col = c;
        }
    }
}