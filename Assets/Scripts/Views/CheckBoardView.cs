using System;
using System.Collections.Generic;
using Enums;
using Structs;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class CheckBoardView : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private CellViewLogic cellPrefab;
        
        
        public event Action<int,int> CellClicked; // 再转发给 GameController
        
        
        private RectTransform _gridRoot;      // 挂着 GridLayoutGroup 的容器
        private GridLayoutGroup _gridLayoutGroup;        // 设定行列/间距
        
        private int _rows, _cols;
        private CellViewLogic[,] _cellsGrid;
        private readonly List<CellViewLogic> _cellsFlat = new();

        private void Awake()
        {
            _gridRoot = GetComponent<RectTransform>();
            _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        }

        public void BuildGrid(int rows, int cols)
        {
            _rows = rows; _cols = cols;
            ClearChildren();

            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = cols;

            _cellsGrid = new CellViewLogic[rows, cols];
            _cellsFlat.Clear();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    CellViewLogic cell = Instantiate(cellPrefab, _gridRoot);
                    cell.SetRowCol(r, c);
                    cell.Clear();
                    cell.SetInteractable(true);
                    cell.SetHighlight(false);

                    cell.Clicked += OnCellClickedInner;

                    _cellsGrid[r, c] = cell;
                    _cellsFlat.Add(cell);
                }
            }
        }
        
        private void OnCellClickedInner(int r, int c)
        {
            CellClicked?.Invoke(r, c);
        }
        public void RenderMove(int r, int c, ESide side)
        {
            if (!InRange(r, c)) return;
            _cellsGrid[r, c].SetMark(side);
        }
        public void Highlight(WinCell[] line)
        {
            // 先清掉所有高光
            foreach (var cell in _cellsFlat) cell.SetHighlight(false);
            if (line == null) return;
            foreach (var wc in line)
            {
                if (InRange(wc.Row, wc.Col))
                    _cellsGrid[wc.Row, wc.Col].SetHighlight(true);
            }
        }
        public void SetInteractable(bool canInteractable)
        {
            foreach (var cell in _cellsFlat) cell.SetInteractable(canInteractable);
        }
        public void ClearAll()
        {
            foreach (var cell in _cellsFlat) cell.Clear();
            Highlight(null);
        }
        private bool InRange(int r, int c) =>
            r >= 0 && r < _rows && c >= 0 && c < _cols;
        private void ClearChildren()
        {
            for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            {
                var t = _gridRoot.GetChild(i);
                Destroy(t.gameObject);
            }
        }
    }
}
