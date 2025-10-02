using System.Collections.Generic;
using Enums;
using Structs;
using UnityEngine;

namespace Data.WinRules
{
    [CreateAssetMenu(fileName = "TicTacToeWinRule", menuName = "GamePlay/WinRule/TicTacToeWinRule", order = 0)]
    public class CustomTicTacToeWinRule : BaseWinRule
    {
        [SerializeField,Min(3)] private int winLength = 3; // 连成一线的数量
        public int WinLength => winLength;
        public override bool CheckWinCondition(ESide[,] board, MoveInformation lastMove, out GameResult resInfo)
        {
            resInfo = default;

            var rowCount = board.GetLength(0);
            var colCount = board.GetLength(1);
            var side = lastMove.ESide;
            if (side == ESide.None) return false;

            // 四个方向：水平、垂直、主对角、反对角
            var dirs = new (int dr, int dc)[] { (0, 1), (1, 0), (1, 1), (1, -1) };

            foreach (var (dr, dc) in dirs)
            {
                // 收集“包含 lastMove 的整条同色线”
                var line = CollectLine(board, lastMove.Row, lastMove.Col, dr, dc, side);

                if (line.Count >= winLength)
                {
                    // 选出长度刚好为 winLength 的一段（包含本手）
                    var segment = ExtractSegment(line, lastMove.Row, lastMove.Col, winLength);
                    resInfo = new GameResult(EResultKind.Win, side, segment.ToArray());
                    return true;
                }
            }

            // 无胜者 → 判断是否平局（是否还有空位）
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                    if (board[r, c] == ESide.None)
                        return false; // 还有空位 → 未结束
            }
            

            // 棋盘已满且没人获胜 → 平局
            resInfo = new GameResult(EResultKind.Draw, ESide.None, null);
            return true;
        }
        /// <summary>
        /// 以 (r,c) 为中心，沿 (±dr,±dc) 两个方向收集同色格，返回有序列表（从负方向到正方向，包含 (r,c)）
        /// </summary>
        private List<WinCell> CollectLine(ESide[,] board, int row, int col, int dRow, int dCol, ESide side)
        {
            var rows = board.GetLength(0);
            var cols = board.GetLength(1);
            var listNeg = new List<WinCell>();
            var cells = new List<WinCell>();

            // 负方向
            int currentRow = row - dRow;
            int currentCol = col - dCol;
            
            while (currentRow >= 0 && currentRow < rows && currentCol >= 0 && currentCol < cols && board[currentRow, currentCol] == side)
            {
                listNeg.Add(new WinCell(currentRow, currentCol));
                currentRow -= dRow; currentCol -= dCol;
            }
            listNeg.Reverse();       // 保证从远到近

            // 中心
            cells.AddRange(listNeg);
            cells.Add(new WinCell(row, col));

            // 正方向
            currentRow = row + dRow; 
            currentCol = col + dCol;
            while (currentRow >= 0 && currentRow < rows && currentCol >= 0 && currentCol < cols && board[currentRow, currentCol] == side)
            {
                cells.Add(new WinCell(currentRow, currentCol));
                currentRow += dRow; currentCol += dCol;
            }
            return cells;
        }

        /// <summary>
        /// 从整条线里截取一个长度为 k 的连续段，保证包含 (r,c)
        /// </summary>
        private List<WinCell> ExtractSegment(List<WinCell> alignedLine, int row, int col, int k)
        {
            if (alignedLine.Count == k) return alignedLine;

            int centerIndex = alignedLine.FindIndex(p => p.Row == row && p.Col == col);
            // 使 (r,c) 尽量靠段尾（也可以改成居中策略）
            int start = Mathf.Clamp(centerIndex - (k - 1), 0, alignedLine.Count - k);
            return alignedLine.GetRange(start, k);
        }
    }
}