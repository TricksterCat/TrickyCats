using GameRules.Scripts.Modules.Database.Items;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.WheelOfFortune
{
    public class ItemWheelItem : WheelItem
    {
        [SerializeField]
        private Image _icon;

        private string _itemId;

        public void Set(BaseItem item)
        {
            IsVisible = true;
            _itemId = item.Id;
            _icon.sprite = item.Icon;
        }

        public override string Type => "item";
        public override string StringValue => _itemId;
    }
}