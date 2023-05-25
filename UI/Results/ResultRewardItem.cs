using TMPro;
using UnityEngine;

namespace GameRules.Scripts.UI.Results
{
    public class ResultRewardItem : MonoBehaviour
    {
        [SerializeField]
        private GameObject _separatorNext;

        [SerializeField]
        private string _mask;
        [SerializeField]
        private TextMeshProUGUI _value;
        
        [SerializeField]
        private I2.Loc.LocalizationParamsManager _valueParams;
        [SerializeField]
        private string _paramKey;
        
        public bool Visible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public bool HaveNext
        {
            get => _separatorNext != null && _separatorNext.activeSelf;
            set
            {
                if(_separatorNext != null)
                    _separatorNext.SetActive(value);
            }
        }

        public void SetValue(string value)
        {
            if (!string.IsNullOrWhiteSpace(_paramKey))
                _valueParams.SetParameterValue(_paramKey, value);
            else
            {
                if (!string.IsNullOrWhiteSpace(_mask))
                    value = string.Format(_mask, value);
                _value.text = value;
            }
        }
    }
}