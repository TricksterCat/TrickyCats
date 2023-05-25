using GameRules.Scripts.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.WheelOfFortune
{
    public class WalletWheelItem : WheelItem
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TextMeshProUGUI _value;

        private string _type;
        
        public void Set(string type, WalletInfo model, string count)
        {
            IsVisible = true;
            
            _icon.sprite = model.Icon;
            _value.color = model.Color;
            _value.text = count;

            _type = type;
        }

        public override string Type => _type;
        public override string StringValue => _value.text;
    }
}