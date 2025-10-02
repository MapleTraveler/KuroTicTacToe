using System;
using Enums;
using Structs;
using UnityEngine.UI;

namespace Views
{
    public class AIDifficultyPanel : PanelBase
    {
        Dropdown _difficultyDropdown;
        Toggle _humanFirstToggle;
        Button _confirmBtn;
        Button _backBtn;
        
        public event Action<GameConfig> Confirmed;
        public event Action Canceled;
        
        protected override void Awake()
        {
            base.Awake();
            _difficultyDropdown = GetElement<Dropdown>("DifficultyDropdown");
            _humanFirstToggle   = GetElement<Toggle>("HumanFirstToggle");
            _confirmBtn         = GetElement<Button>("ConfirmBtn");
            _backBtn            = GetElement<Button>("BackBtn");

            _confirmBtn?.onClick.AddListener(OnConfirm);
            _backBtn?.onClick.AddListener(() => Canceled?.Invoke());
        }
        
        private void OnConfirm()
        {
            var diff = EDifficulty.Standard;
            if (_difficultyDropdown)
            {
                diff = _difficultyDropdown.value == 0 ? EDifficulty.Easy : EDifficulty.Standard;
            }

            var humanFirst = !_humanFirstToggle || _humanFirstToggle.isOn;
            var pName = "玩家";
            var aiName = "AI";

            // 先手恒为 X
            var firstName  = humanFirst ? pName : aiName;
            var secondName = humanFirst ? aiName : pName;

            var cfg = new GameConfig(EGameMode.PVE, humanFirst, firstName, secondName, diff);
            Confirmed?.Invoke(cfg);
        }

    }
}