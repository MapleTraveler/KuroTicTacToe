using System;
using System.Collections.Generic;
using Enums;
using Structs;

namespace Core
{
    /// <summary>
    /// 井字/五子类棋盘的简单 AI。
    /// 职责：在给定棋盘、当前执子方与难度下，返回一个可落子的 MoveInformation。
    /// 约束：不修改传入棋盘；若无可落子则返回 (-1,-1)。
    /// </summary>
    public class AIAgent
    {
        // 伪随机数：用于 Easy 难度的随机选择与候选打散
        private readonly Random _rng = new Random();
        
        private const int StandardTimeBudgetMs = 80;
        private int _deadlineTick;

        /// <summary>
        /// 决策入口：根据难度返回一个落子。
        /// </summary>
        /// <param name="board">棋盘状态，board[r,c] 表示格子归属</param>
        /// <param name="sideToPlay">当前行动方（X/O）</param>
        /// <param name="level">难度（Easy/Standard）</param>
        /// <param name="winLength">胜利所需连子数</param>
        /// <returns>建议落子（行、列、阵营）；若无空位则 (-1,-1)</returns>
        public MoveInformation Decide(ESide[,] board, ESide sideToPlay, EDifficulty level, int winLength = 3)
        {
            if (!HasEmptyCell(board))
                return new MoveInformation(-1, -1, sideToPlay);
            if (level == EDifficulty.Easy)
                return DecideEasy(board, sideToPlay, winLength);

            _deadlineTick = Environment.TickCount + StandardTimeBudgetMs;
            return DecideStandard(board, sideToPlay, winLength);
        }

        // -------------------- Easy：启发式（速度快，可被针对） --------------------

        /// <summary>
        /// Easy：一步取胜 > 及时堵截 > 中心 > 角落 > 随机空位。
        /// </summary>
        private MoveInformation DecideEasy(ESide[,] board, ESide me, int winLength)
        {
            var opp = Opp(me);

            // 1.一步取胜
            if (TryFindWinningMove(board, me, winLength, out var win))
                return win;

            // 2.及时堵截（对手一步取胜点）
            if (TryFindWinningMove(board, opp, winLength, out var block))
                return new MoveInformation(block.Row, block.Col, me);

            // 3.中心
            int cr = board.GetLength(0) / 2, cc = board.GetLength(1) / 2;
            if (IsInside(board, cr, cc) && board[cr, cc] == ESide.None)
                return new MoveInformation(cr, cc, me);

            // 4.角落（随机）
            var corners = new List<(int r, int c)>
            {
                (0, 0),
                (0, board.GetLength(1) - 1),
                (board.GetLength(0) - 1, 0),
                (board.GetLength(0) - 1, board.GetLength(1) - 1),
            };
            ShuffleInPlace(corners);
            foreach (var (r, c) in corners)
                if (IsInside(board, r, c) && board[r, c] == ESide.None)
                    return new MoveInformation(r, c, me);

            // 5) 兜底：所有空位中随机
            var moves = EnumerateEmptyMoves(board);
            var pick = moves[_rng.Next(moves.Count)];
            return new MoveInformation(pick.r, pick.c, me);
        }

        // -------------------- Standard：Minimax + Alpha-Beta --------------------

        /// <summary>
        /// Standard：小盘完全搜索，大盘有限深度 + 启发式评估。
        /// </summary>
        private MoveInformation DecideStandard(ESide[,] board, ESide me, int winLength)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            int cells = rows * cols;
            bool smallBoard = (cells <= 9) && winLength == 3;
            bool largeBoard = (cells > 81) || (winLength >= 5); // 10x10+ 或 五子棋
            
            // 先做快速“即赢/即堵”
            if (TryFindWinningMove(board, me, winLength, out var win))
                return win;
            if (TryFindWinningMove(board, Opp(me), winLength, out var block))
                return new MoveInformation(block.Row, block.Col, me);
            
            List<(int r, int c)> rootMoves;
            int maxDepth;
            
            if (smallBoard)
            {
                rootMoves = EnumerateEmptyMoves(board);
                maxDepth = Math.Min(9, CountEmptyCells(board));
            }
            else if (largeBoard)
            {
                // 仅搜索候选点（半径 2，限制数量）
                rootMoves = EnumerateCandidateMoves(board, radius: 2, maxCount: 48);
                // 根节点候选太多就降深
                maxDepth = rootMoves.Count <= 12 ? 4 : rootMoves.Count <= 24 ? 3 : 2;
            }
            else
            {
                rootMoves = EnumerateCandidateMoves(board, radius: 1, maxCount: 64);
                maxDepth = Math.Min(5, CountEmptyCells(board));
            }
            // 候选为空（例如开局）→ 放中心
            if (rootMoves.Count == 0)
            {
                int cr = rows / 2, cc = cols / 2;
                return new MoveInformation(cr, cc, me);
            }
            OrderCandidatesInPlace(rootMoves, rows, cols);
            int bestScore = int.MinValue;
            (int r, int c) best = (-1, -1);

            foreach (var (r, c) in rootMoves)
            {
                if (Environment.TickCount >= _deadlineTick) break;

                board[r, c] = me;
                int score = SearchMinimax(board, me, Opp(me), false, 1, maxDepth, winLength, int.MinValue + 1, int.MaxValue - 1, useCandidates: largeBoard);
                board[r, c] = ESide.None;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = (r, c);
                }
            }
            if (best.r < 0)
            {
                var mv = rootMoves[0];
                best = mv;
            }
            return new MoveInformation(best.r, best.c, me);
        }

        /// <summary>
        /// 极大极小搜索（含 Alpha-Beta 剪枝）。
        /// </summary>
        /// <param name="board">棋盘</param>
        /// <param name="me">我方阵营</param>
        /// <param name="sideToMove">当前层要行动的一方</param>
        /// <param name="isMax">当前层是否为最大化层（我方）</param>
        /// <param name="depth">当前深度（根为 1）</param>
        /// <param name="maxDepth">最大搜索深度</param>
        /// <param name="winLength">连线长度</param>
        /// <param name="alpha">Alpha 值</param>
        /// <param name="beta">Beta 值</param>
        /// <param name="useCandidates">是否仅搜索候选点（大盘时为 true）</param>
        private int SearchMinimax(ESide[,] board, ESide me, ESide sideToMove, bool isMax, int depth, int maxDepth, int winLength, int alpha, int beta, bool useCandidates)
        {
            if (Environment.TickCount >= _deadlineTick)
                return EvaluateHeuristic(board, me, winLength);

            var term = EvaluateTerminalState(board, winLength);
            if (term.ended)
            {
                if (term.winner == me) return 1000 - depth;
                if (term.winner == Opp(me)) return depth - 1000;
                return 0;
            }

            if (depth >= maxDepth)
                return EvaluateHeuristic(board, me, winLength);

            var moves = useCandidates ? EnumerateCandidateMoves(board, radius: 2, maxCount: 48)
                : EnumerateEmptyMoves(board);
            if (moves.Count == 0)
                return EvaluateHeuristic(board, me, winLength);
            OrderCandidatesInPlace(moves, board.GetLength(0), board.GetLength(1));

            int best = isMax ? int.MinValue : int.MaxValue;

            foreach (var (r, c) in moves)
            {
                if (Environment.TickCount >= _deadlineTick)
                    break;

                board[r, c] = sideToMove;
                int score = SearchMinimax(board, me, Opp(sideToMove), !isMax, depth + 1, maxDepth, winLength, alpha, beta, useCandidates);
                board[r, c] = ESide.None;

                if (isMax)
                {
                    if (score > best) best = score;
                    if (best > alpha) alpha = best;
                    if (beta <= alpha) break;
                }
                else
                {
                    if (score < best) best = score;
                    if (best < beta) beta = best;
                    if (beta <= alpha) break;
                }
            }
            return best;
        }
            

        /// <summary>
        /// 极大极小搜索（含 Alpha-Beta 剪枝）。
        /// </summary>
        /// <param name="board">棋盘</param>
        /// <param name="me">我方阵营</param>
        /// <param name="sideToMove">当前层要行动的一方</param>
        /// <param name="isMax">当前层是否为最大化层（我方）</param>
        /// <param name="depth">当前深度（根为 1）</param>
        /// <param name="maxDepth">最大搜索深度</param>
        /// <param name="winLength">连线长度</param>
        /// <param name="alpha">Alpha 值</param>
        /// <param name="beta">Beta 值</param>
        private int SearchMinimax(ESide[,] board, ESide me, ESide sideToMove, bool isMax, int depth, int maxDepth, int winLength, int alpha, int beta)
        {
            var term = EvaluateTerminalState(board, winLength);
            if (term.ended)
            {
                if (term.winner == me) return 1000 - depth;     // 越快赢分越高
                if (term.winner == Opp(me)) return depth - 1000; // 越晚输分越高
                return 0; // 和棋
            }

            if (depth >= maxDepth)
                return EvaluateHeuristic(board, me, winLength);

            int best = isMax ? int.MinValue : int.MaxValue;

            foreach (var (r, c) in EnumerateEmptyMoves(board))
            {
                board[r, c] = sideToMove;
                int score = SearchMinimax(board, me, Opp(sideToMove), !isMax, depth + 1, maxDepth, winLength, alpha, beta);
                board[r, c] = ESide.None;

                if (isMax)
                {
                    if (score > best) best = score;
                    if (best > alpha) alpha = best;
                    if (beta <= alpha) break; // 剪枝
                }
                else
                {
                    if (score < best) best = score;
                    if (best < beta) beta = best;
                    if (beta <= alpha) break; // 剪枝
                }
            }
            return best;
        }
        #region 评估与工具函数
        
        /// <summary>
        /// 终局判定：是否有人连成 winLength；无空位则和棋。
        /// </summary>
        private (bool ended, ESide winner) EvaluateTerminalState(ESide[,] board, int winLength)
        {
            if (HasWin(board, ESide.X, winLength)) return (true, ESide.X);
            if (HasWin(board, ESide.O, winLength)) return (true, ESide.O);
            if (!HasEmptyCell(board)) return (true, ESide.None);
            return (false, ESide.None);
        }

        /// <summary>
        /// 启发式评估：简单线势分。
        /// </summary>
        private int EvaluateHeuristic(ESide[,] board, ESide me, int winLength)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            bool largeBoard = (rows * cols > 81) || (winLength >= 5);
            


            if (largeBoard)
            {
                // 轻量评估：中心倾向 + 棋子密度与连势的粗略分
                int score = 0;
                int cr = rows / 2, cc = cols / 2;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var s = board[r, c];
                        if (s == ESide.None) continue;

                        int dCenter = Math.Max(Math.Abs(r - cr), Math.Abs(c - cc)); // Chebyshev
                        int baseV = 3 - dCenter; // 越靠中心越高
                        if (baseV < -3) baseV = -3;

                        int chain = LongestChainFrom(board, r, c, s);
                        int v = baseV + chain * 2;

                        score += (s == me) ? v : -v;
                    }
                }
                return score;
            }
            else
            {
               
                int score = 0;
                int cr = rows / 2, cc = cols / 2;

                if (IsInside(board, cr, cc))
                {
                    if (board[cr, cc] == me) score += 2;
                    else if (board[cr, cc] == Opp(me)) score -= 2;
                }

                var corners = new (int r, int c)[] { (0, 0), (0, cols - 1), (rows - 1, 0), (rows - 1, cols - 1) };
                foreach (var (r, c) in corners)
                {
                    if (!IsInside(board, r, c)) continue;
                    if (board[r, c] == me) score += 1;
                    else if (board[r, c] == Opp(me)) score -= 1;
                }
                return score;
            }
        }
        
        private int LongestChainFrom(ESide[,] board, int r, int c, ESide s)
        {
            if (board[r, c] != s) return 0;
            var dirs = new (int dr, int dc)[] { (0, 1), (1, 0), (1, 1), (1, -1) };
            int best = 1;
            foreach (var (dr, dc) in dirs)
            {
                int cnt = 1, cr = r + dr, cc = c + dc;
                while (IsInside(board, cr, cc) && board[cr, cc] == s)
                {
                    cnt++; cr += dr; cc += dc;
                }
                best = Math.Max(best, cnt);
            }
            return best;
        }
        
        

        /// <summary>
        /// 枚举全部空位。
        /// </summary>
        private List<(int r, int c)> EnumerateEmptyMoves(ESide[,] board)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            var list = new List<(int r, int c)>(rows * cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (board[r, c] == ESide.None)
                        list.Add((r, c));
            return list;
        }
        
        
        // 仅挑选距离已有棋子 <= radius 的空位，并限制返回数量
        private List<(int r, int c)> EnumerateCandidateMoves(ESide[,] board, int radius = 2, int maxCount = 64)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);

            // 找出已有棋子的包围盒
            int minR = rows, minC = cols, maxR = -1, maxC = -1;
            bool any = false;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (board[r, c] != ESide.None)
                    {
                        any = true;
                        if (r < minR) minR = r;
                        if (r > maxR) maxR = r;
                        if (c < minC) minC = c;
                        if (c > maxC) maxC = c;
                    }
                }
            }
            var list = new List<(int r, int c)>();

            if (!any)
            {
                // 开局：返回中心
                list.Add((rows / 2, cols / 2));
                return list;
            }

            int r0 = Math.Max(0, minR - radius);
            int r1 = Math.Min(rows - 1, maxR + radius);
            int c0 = Math.Max(0, minC - radius);
            int c1 = Math.Min(cols - 1, maxC + radius);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (board[r, c] != ESide.None) continue;
                    if (HasNeighborWithin(board, r, c, radius))
                        list.Add((r, c));
                }
            }
            if (list.Count == 0)
            {
                // 兜底：仍无候选则返回全部空位中的少量
                var all = EnumerateEmptyMoves(board);
                ShuffleInPlace(all);
                int take = Math.Min(maxCount, all.Count);
                return all.GetRange(0, take);
            }

            // 打散并截断
            ShuffleInPlace(list);
            if (list.Count > maxCount) list = list.GetRange(0, maxCount);
            return list;
        }
        private bool HasNeighborWithin(ESide[,] board, int r, int c, int radius)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            for (int dr = -radius; dr <= radius; dr++)
            {
                int rr = r + dr;
                if (rr < 0 || rr >= rows) continue;
                for (int dc = -radius; dc <= radius; dc++)
                {
                    int cc = c + dc;
                    if (cc < 0 || cc >= cols) continue;
                    if (dr == 0 && dc == 0) continue;
                    if (board[rr, cc] != ESide.None) return true;
                }
            }
            return false;
        }
        private void OrderCandidatesInPlace(List<(int r, int c)> moves, int rows, int cols)
        {
            int cr = rows / 2, cc = cols / 2;
            moves.Sort((a, b) =>
            {
                int da = Math.Max(Math.Abs(a.r - cr), Math.Abs(a.c - cc));
                int db = Math.Max(Math.Abs(b.r - cr), Math.Abs(b.c - cc));
                return da.CompareTo(db);
            });
        }

        /// <summary>
        /// 找“一步取胜”落点。
        /// </summary>
        private bool TryFindWinningMove(ESide[,] board, ESide side, int winLength, out MoveInformation move)
        {
            foreach (var (r, c) in EnumerateEmptyMoves(board))
            {
                board[r, c] = side;
                bool win = HasWin(board, side, winLength);
                board[r, c] = ESide.None;

                if (win)
                {
                    move = new MoveInformation(r, c, side);
                    return true;
                }
            }
            move = default;
            return false;
        }

        /// <summary>
        /// 是否有人达成 k 连线（水平/垂直/两对角）。
        /// </summary>
        private bool HasWin(ESide[,] board, ESide side, int k)
        {
            if (side == ESide.None) return false;
            int rows = board.GetLength(0), cols = board.GetLength(1);
            var dirs = new (int dr, int dc)[] { (0, 1), (1, 0), (1, 1), (1, -1) };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (board[r, c] != side) continue;

                    foreach (var (dr, dc) in dirs)
                    {
                        int cnt = 1, cr = r + dr, cc = c + dc;
                        while (IsInside(board, cr, cc) && board[cr, cc] == side)
                        {
                            cnt++; cr += dr; cc += dc;
                            if (cnt >= k) return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsInside(ESide[,] board, int r, int c) =>
            r >= 0 && c >= 0 && r < board.GetLength(0) && c < board.GetLength(1);

        private static ESide Opp(ESide s) => s == ESide.X ? ESide.O : ESide.X;

        private static int CountEmptyCells(ESide[,] board)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1), n = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (board[r, c] == ESide.None) n++;
            return n;
        }

        private static bool HasEmptyCell(ESide[,] board)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (board[r, c] == ESide.None) return true;
            return false;
        }
        // Fisher-Yates 洗牌
        private void ShuffleInPlace<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        

        #endregion 

        
    }
}
