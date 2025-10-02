using System;
using Data;
using Data.WinRules;
using Enums;
using Structs;

namespace Core
{
    public class CheckerBoard
    {
        BoardData _boardData;
        BaseWinRule _winRule;
        ESide[,] _board; 
        public CheckerBoard(BoardData boardData,BaseWinRule winCondition)
        {
            _boardData = boardData;
            _board = new ESide[_boardData.edgeSize,_boardData.edgeSize];
            _winRule = winCondition;
        }
        /// <summary>
        /// 在棋盘上落子
        /// </summary>
        /// <param name="moveInformation">落子信息</param>
        /// <returns>落子结果</returns>
        public bool ApplyMove(MoveInformation moveInformation)
        {
            if (!CanPlace(moveInformation.Row, moveInformation.Col))
                return false;
            _board[moveInformation.Row, moveInformation.Col] = moveInformation.ESide;
            return true;
        }
        /// <summary>
        /// 是否可以落子
        /// </summary>
        /// <param name="r">行</param>
        /// <param name="c">列</param>
        /// <returns>当前位置是否可以落子</returns>
        public bool CanPlace(int r, int c)
        {
            if (r < 0 || r >= _boardData.edgeSize || c < 0 || c >= _boardData.edgeSize)
                return false;
            return _board[r, c] == ESide.None;
        }
        /// <summary>
        /// 判定胜负
        /// </summary>
        /// <param name="lastMove">上一步的落子信息</param>
        /// <param name="resInfo"></param>
        /// <returns>是否结束</returns>
        public bool JudgeEnd(MoveInformation lastMove,out GameResult resInfo)
        {
            return _winRule.CheckWinCondition(_board, lastMove, out resInfo);
        }
        /// <summary>
        /// 返回当前棋盘的浅拷贝
        /// </summary>
        public ESide[,] Snapshot()
        {
            int n = _boardData.edgeSize;
            var copy = new ESide[n, n];
            for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                copy[r, c] = _board[r, c];
            return copy;
        }
        
        
    }
}
