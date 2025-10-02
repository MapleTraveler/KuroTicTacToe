using Enums;
using Structs;
using UnityEngine;

namespace Data.WinRules
{
    public abstract class BaseWinRule : ScriptableObject
    {
        public abstract bool CheckWinCondition(ESide[,] board, MoveInformation lastMove, out GameResult resInfo);
    }
}