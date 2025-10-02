using System;
using Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class CellViewLogic : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        
        public event Action<int, int> Clicked;
        [SerializeField] private Image bg;
        [SerializeField] private Image mark;
        [SerializeField] private Sprite spriteX;
        [SerializeField] private Sprite spriteO;
        
        [Header("Colors")]
        [SerializeField] private Color baseColor      = new Color(0.15f,0.15f,0.18f,1);
        [SerializeField] private Color hoverColor     = new Color(0.25f,0.25f,0.28f,1);
        [SerializeField] private Color highlightColor = new Color(0.95f,0.85f,0.25f,1);
        [SerializeField] private Color disabledColor  = new Color(0.12f,0.12f,0.12f,1);
        
        
        private Button _btn;
        private int _row, _col;
        private bool _isHighlighted;
        private bool _interactable = true;
        private ESide _current = ESide.None;
        
        public void SetRowCol(int r, int c)
        {
            _row = r;
            _col = c;
        }
        private void Awake()
        {
            _btn = GetComponent<Button>();
            if (!bg) bg = GetComponent<Image>();
            if (mark) mark.enabled = false;
            bg.color = baseColor;
            
        }




        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable) return;
            if (_current == ESide.None && !_isHighlighted)
                bg.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_interactable) return;
            if (!_isHighlighted)
                bg.color = baseColor;
        }
        public void SetMark(ESide side)
        {
            _current = side;
            if (!mark) return;

            switch (side)
            {
                case ESide.X:
                    mark.sprite = spriteX;
                    mark.enabled = true;
                    break;
                case ESide.O:
                    mark.sprite = spriteO;
                    mark.enabled = true;
                    break;
                default:
                    mark.enabled = false;
                    break;
            }

            // var color = mark.color;
            // color.a = 1f;
            // mark.color = color;
        }
        public void SetHighlight(bool highlightOn)
        {
            _isHighlighted = highlightOn;
            if (highlightOn) bg.color = highlightColor;
            else    bg.color = (_interactable ? baseColor : disabledColor);
        }
        public void Clear()
        {
            _current = ESide.None;
            if (mark) mark.enabled = false;
            _isHighlighted = false;
            bg.color = baseColor;
        }
        public void SetInteractable(bool canInteractable)
        {
            _interactable = canInteractable;
            _btn.interactable = canInteractable;
            if (!canInteractable) bg.color = disabledColor;
            else if (!_isHighlighted) bg.color = baseColor;
        }
        private void OnClick()
        {
            if (!_interactable) return;
            Clicked?.Invoke(_row, _col);
        }
        private void OnEnable()  => _btn.onClick.AddListener(OnClick);
        private void OnDisable() => _btn.onClick.RemoveListener(OnClick);
    }
}