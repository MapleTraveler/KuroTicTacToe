using Enums;

namespace Structs
{
    public readonly struct GameResult
    {
        public readonly EResultKind Kind;   // 结果类型
        public readonly ESide Winner;        // 若 Kind==Win，则为 X 或 O；否则为 None
        public readonly WinCell[] Line;     // 若 Kind==Win，则为三连的格子坐标

        public GameResult(EResultKind k, ESide w = ESide.None, WinCell[] line = null)
        {
            Kind = k;
            Winner = w;
            Line = line;
        }
    }
}