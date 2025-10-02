using System;
using Data;
using Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class GamePanel : PanelBase
    {
        Button _restartBtn;
        Button _exitBtn;
        CheckBoardView _boardView;
        
        Text _titleText;
        Text _roundInfoText;
        Text _sizeText;
        Text _winConditionText;
        Text _winnerText;
        
        public string XName = "X方";
        public string OName = "O方";
        
        [SerializeField] private Vector2 cellSizeClamp = new Vector2(1f, 300f); // 单元格边长的最小/最大夹取
        private int _cachedRows, _cachedCols;


        public CheckBoardView BoardView => _boardView;

        public event Action RestartClicked;
        public event Action ExitClicked;
        public void Init()
        {
            _boardView        = GetComponentInChildren<CheckBoardView>(true);
            _titleText        = GetElement<Text>("GameTitle");
            _roundInfoText    = GetElement<Text>("RoundText");
            _sizeText         = GetElement<Text>("SizeText");
            _winConditionText = GetElement<Text>("WinConditionText");
            _winnerText       = GetElement<Text>("WinnerText");
            _restartBtn       = GetElement<Button>("RestartBtn");
            _exitBtn          = GetElement<Button>("ExitBtn");

            
            
            _restartBtn?.onClick.AddListener(() => RestartClicked?.Invoke());
            _exitBtn?.onClick.AddListener(() => ExitClicked?.Invoke());

            
            SetRestartAndExitEnable(false);
        }
        
        /// <summary>
        /// 开局时一次性写入的静态信息（场地大小 & 连线数量）
        /// </summary>
        public void SetStaticInfo(BoardData data, int winLength)
        {
            if (_sizeText)         _sizeText.text         = $"棋盘：{data.edgeSize} × {data.edgeSize}";
            if (_winConditionText) _winConditionText.text = $"胜利条件：连线{winLength}格";
        }
        
        
        
        // 计算并设置 GridLayoutGroup 的 cellSize，使其铺满容器
        public void ResizeGridToBoard(int rows, int cols)
        {
            if (!_boardView) return;

            var grid = _boardView.GetComponent<GridLayoutGroup>();
            var rect = (RectTransform)_boardView.transform;
            if (!grid || !rect) return;

            // 先强制一次布局，确保 rect 尺寸有效
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            var padding = grid.padding;
            var spacing = grid.spacing;

            float availW = rect.rect.width  - padding.left - padding.right  - spacing.x * (cols - 1);
            float availH = rect.rect.height - padding.top  - padding.bottom - spacing.y * (rows - 1);

            // 避免负数
            availW = Mathf.Max(0, availW);
            availH = Mathf.Max(0, availH);

            float side = Mathf.Floor(Mathf.Min(availW / cols, availH / rows));
            side = Mathf.Clamp(side, cellSizeClamp.x, cellSizeClamp.y);

            grid.cellSize = new Vector2(side, side);
        }
        protected void OnRectTransformDimensionsChange()
        {
            if (_cachedRows > 0 && _cachedCols > 0)
                ResizeGridToBoard(_cachedRows, _cachedCols);
        }
        /// <summary>
        /// 当前回合提示
        /// </summary>
        public void SetRound(string inName,ESide side)
        {
            if (!_roundInfoText) return;
            string s = side == ESide.X ? "X" : side == ESide.O ? "O" : "-";
            _roundInfoText.text = $"{inName}回合：{s}";
        }
        public void SetRound(ESide side) => SetRound(GetName(side), side);
        public void ShowWinner(string winnerName, ESide side)
        {
            if (!_winnerText) return;
            string s = side == ESide.X ? "X" : side == ESide.O ? "O" : "-";
            _winnerText.text = $"{winnerName}胜！({s})";
            SetRestartAndExitEnable(true);

        }
        public void ShowDraw()
        {
            if (_winnerText) _winnerText.text = "平局！";
            SetRestartAndExitEnable(true);
        }
        public void ResetInGameUI()
        {
            if (_winnerText) _winnerText.text = "";
            SetRestartAndExitEnable(false);
        }
        
        public void SetRestartAndExitEnable(bool enable)
        {
            if(_winnerText)  _winnerText.gameObject.SetActive(enable);
            if (_restartBtn) _restartBtn.gameObject.SetActive(enable);
        }
        public void CacheBoardSize(int rows, int cols)
        {
            _cachedRows = rows;
            _cachedCols = cols;
        }
        

        
        private string GetName(ESide side) => side == ESide.X ? (string.IsNullOrEmpty(XName) ? "X方" : XName)
            : side == ESide.O ? (string.IsNullOrEmpty(OName) ? "O方" : OName)
            : "-";
        
        
    }
}