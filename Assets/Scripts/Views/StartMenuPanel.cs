using System;
using UnityEngine.UI;

namespace Views
{
    public class StartMenuPanel : PanelBase
    {
        Button _pveButton;
        Button _exitButton;

        public event Action PVEClicked;
        public event Action ExitClicked;

        protected override void Awake()
        {
            base.Awake();
            _pveButton = GetElement<Button>("PVEBtn");
            _exitButton = GetElement<Button>("ExitBtn");

            _pveButton?.onClick.AddListener(() => PVEClicked?.Invoke());
            _exitButton?.onClick.AddListener(() => ExitClicked?.Invoke());
        }
    }
}