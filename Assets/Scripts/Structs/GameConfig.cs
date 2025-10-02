using Enums;

namespace Structs
{
    public struct GameConfig
    {
        public EGameMode Mode;          // PVE 或 PVP
        public bool FirstIsHuman;      // 仅 PVE 有意义：true=人先手，false=AI先手
        public string FirstName;       // 先手（=X）的显示名
        public string SecondName;      // 后手（=O）的显示名
        public EDifficulty Difficulty; // PVE 难度（可选）
    
        public GameConfig(EGameMode mode, bool firstIsHuman, string firstName, string secondName, EDifficulty diff = EDifficulty.Standard)
        { Mode = mode; FirstIsHuman = firstIsHuman; FirstName = firstName; SecondName = secondName; Difficulty = diff; }
    }
}