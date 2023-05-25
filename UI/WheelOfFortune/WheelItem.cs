using UnityEngine;

namespace GameRules.Scripts.UI.WheelOfFortune
{
    public abstract class WheelItem : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;

        public float Chance { get; set; }
        public float BackAngle { get; set; }
        public float BackFillAmount { get; set; }
        
        public bool IsVisible
        {
            get => _canvasGroup.alpha > 0.5f;
            set => _canvasGroup.alpha = value ? 1f : 0f;
        }

        public abstract string Type { get; }
        public abstract string StringValue { get; }

        public bool IsWin(string type, string stringValue)
        {
            return Type ==  type && StringValue == stringValue;
        }
    }
}