using System;
using System.Collections;
using Core;
using Data;
using Data.WinRules;
using Enums;
using Structs;
using UnityEngine;
using Views;

public class GameController : MonoBehaviour
{
    [Header("外部引用")]
    [SerializeField] BoardData boardData;
    [SerializeField] BaseWinRule winRule;
    [SerializeField] StartMenuPanel startMenuPanel;
    [SerializeField] AIDifficultyPanel aiDifficultyPanel;
    [SerializeField] GamePanel gamePanel;
    private readonly AIAgent _aiAgent = new AIAgent();
    
    private CheckerBoard _board;
    private CheckBoardView _boardView;
    private bool _gameOver;
    private ESide _currentSide = ESide.X;
    private GameConfig _cfg;        // 对局配置
    private Coroutine _aiCoroutine;
    

    void Start()
    {
        gamePanel.Init();
        _boardView = gamePanel.BoardView;
        _boardView.CellClicked += OnCellClicked;

        // 主菜单
        if (startMenuPanel)
        {
            startMenuPanel.PVEClicked += OnStartPVE;
            startMenuPanel.ExitClicked += OnExitClicked;
        }
        // 难度选择
        if (aiDifficultyPanel)
        {
            aiDifficultyPanel.Confirmed += OnAIDifficultyConfirmed;
            aiDifficultyPanel.Canceled += OnAIDifficultyCanceled;
        }

        // 游戏内
        gamePanel.RestartClicked += Restart;
        gamePanel.ExitClicked    += OnExitClicked;
        
        ShowMainMenu();
    }
    
    public void ApplyConfig(GameConfig cfg) => _cfg = cfg;
    
    // 重新开局：PVE 先回到“难度/先手选择”面板
    public void Restart()
    {
        if (_cfg.Mode == EGameMode.PVE)
        {
            gamePanel.Hide();
            aiDifficultyPanel.Show();
            return;
        }
        StartGame();
    }
    
    private void OnDestroy()
    {
        if (_boardView) _boardView.CellClicked -= OnCellClicked;

        if (startMenuPanel)
        {
            startMenuPanel.PVEClicked -= OnStartPVE;
            startMenuPanel.ExitClicked -= OnExitClicked;
        }
        if (aiDifficultyPanel)
        {
            aiDifficultyPanel.Confirmed -= OnAIDifficultyConfirmed;
            aiDifficultyPanel.Canceled -= OnAIDifficultyCanceled;
        }
        gamePanel.RestartClicked -= Restart;
        gamePanel.ExitClicked    -= OnExitClicked;
    }
    // 面板流转
    private void ShowMainMenu()
    {
        startMenuPanel.Show();
        aiDifficultyPanel.Hide();
        gamePanel.Hide();
    }
    private void OnStartPVE()
    {
        startMenuPanel.Hide();
        aiDifficultyPanel.Show();
    }
    private void OnAIDifficultyCanceled()
    {
        aiDifficultyPanel.Hide();
        startMenuPanel.Show();
    }
    private void OnAIDifficultyConfirmed(GameConfig cfg)
    {
        ApplyConfig(cfg);
        aiDifficultyPanel.Hide();
        StartGame();
    }
    private void OnExitClicked()
    {
        ShowMainMenu();
    }

    
    private void StartGame()
    {
        // 默认配置（若未设置）：PVE、人先
        startMenuPanel.Hide();
        aiDifficultyPanel.Hide();
        gamePanel.Show();
        if (_cfg.Mode == 0 && string.IsNullOrEmpty(_cfg.FirstName) && string.IsNullOrEmpty(_cfg.SecondName))
        {
            _cfg = new GameConfig(EGameMode.PVE, true, "玩家", "AI", EDifficulty.Standard);
        }

        // 初始化棋盘数据
        _board = new CheckerBoard(boardData, winRule);
        _gameOver = false;
        _currentSide = ESide.X;

        // 初始化视图
        _boardView.BuildGrid(boardData.edgeSize, boardData.edgeSize);
        _boardView.ClearAll();
        _boardView.SetInteractable(IsHumanTurn());
        
        // 让网格单元格自适应当前棋盘尺寸
        gamePanel.CacheBoardSize(boardData.edgeSize, boardData.edgeSize);
        gamePanel.ResizeGridToBoard(boardData.edgeSize, boardData.edgeSize);

        // 页面静态信息与名称
        gamePanel.XName = _cfg.FirstName;
        gamePanel.OName = _cfg.SecondName;
        gamePanel.ResetInGameUI();
        gamePanel.SetStaticInfo(boardData, GetWinLength(winRule));
        gamePanel.SetRound(_currentSide);
        
        // 若 AI 先手，则立即调度 AI 回合
        TryScheduleAITurn();
    }

    private void OnCellClicked(int r, int c)
    {
        if (_gameOver) return;
        if (!_board.CanPlace(r, c)) return;
        if (!IsHumanTurn()) return; // 仅允许人类回合点击

        var move = new MoveInformation(r, c, _currentSide);
        ApplyMoveAndProgress(move);
    }
    
    private void ApplyMoveAndProgress(MoveInformation move)
    {
        if (_gameOver) return;

        if (_board.ApplyMove(move))
        {
            _boardView.RenderMove(move.Row, move.Col, move.ESide);

            if (_board.JudgeEnd(move, out var result))
            {
                HandleEnd(result);
                return;
            }

            _currentSide = Opposite(_currentSide);
            gamePanel.SetRound(_currentSide);
            _boardView.SetInteractable(IsHumanTurn());

            TryScheduleAITurn();
        }
    }
    private void HandleEnd(GameResult result)
    {
        _gameOver = true;
        _boardView.SetInteractable(false);

        if (_aiCoroutine != null)
        {
            StopCoroutine(_aiCoroutine);
            _aiCoroutine = null;
        }

        if (result.Kind == EResultKind.Win)
        {
            if (result.Line is { Length: > 0 })
                _boardView.Highlight(result.Line);

            var winnerName = GetSideName(result.Winner);
            gamePanel.ShowWinner(winnerName, result.Winner);
        }
        else if (result.Kind == EResultKind.Draw)
        {
            gamePanel.ShowDraw();
        }
    }
    private void TryScheduleAITurn()
    {
        if (_gameOver) return;
        if (_cfg.Mode != EGameMode.PVE) return;
        if (IsHumanTurn()) return;

        _boardView.SetInteractable(false);

        if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
        _aiCoroutine = StartCoroutine(CoAIMove());
    }
    private IEnumerator CoAIMove()
    {
        yield return null;

        var boardArr = _board.Snapshot();
        int winLen = GetWinLength(winRule);

        var aiMove = _aiAgent.Decide(boardArr, _currentSide, _cfg.Difficulty, winLen);

        // 保护：AI 返回非法坐标时，兜底找第一个空位
        if (aiMove.Row < 0 || !_board.CanPlace(aiMove.Row, aiMove.Col))
        {
            bool placed = false;
            for (int r = 0; r < boardData.edgeSize && !placed; r++)
            {
                for (int c = 0; c < boardData.edgeSize && !placed; c++)
                {
                    if (_board.CanPlace(r, c))
                    {
                        aiMove = new MoveInformation(r, c, _currentSide);
                        placed = true;
                    }
                }
            }
        }

        ApplyMoveAndProgress(aiMove);
        _aiCoroutine = null;
    }
    private bool IsHumanTurn()
    {
        if (_cfg.Mode == EGameMode.PVP) return true;
        // PVE：先手恒为 X
        bool humanIsX = _cfg.FirstIsHuman;
        return (_currentSide == ESide.X && humanIsX) || (_currentSide == ESide.O && !humanIsX);
    }
    private string GetSideName(ESide side) => side == ESide.X ? gamePanel.XName : gamePanel.OName;
    private static int GetWinLength(BaseWinRule rule)
    {
        if (rule is CustomTicTacToeWinRule ttt) return ttt.WinLength;
        return 3;
    }

    private static ESide Opposite(ESide s) => s == ESide.X ? ESide.O : ESide.X;
    
    
}