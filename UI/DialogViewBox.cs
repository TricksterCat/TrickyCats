using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI
{
    public class DialogViewBox : MonoBehaviour
    {
        private static DialogViewBox _instance;

        public static DialogViewBox Instance => _instance;
        
        [SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField]
        private float _speedFade;
        [SerializeField]
        private Ease _showEase;
        [SerializeField]
        private Ease _hideEase;
        
        [SerializeField]
        private Localize _title;
        [SerializeField]
        private Localize _message;
        [SerializeField]
        private LocalizationParamsManager _params;

        [SerializeField]
        private ButtonInfo _positiveBtn;
        [SerializeField]
        private ButtonInfo _negativeBtn;

        [SerializeField]
        private RectTransform _globalContent;

        [SerializeField]
        private float _zeroGlobalSize;

        private TweenerCore<float, float, FloatOptions> _lastTween;

        public event Action OnNextShow;
        public event Action OnNextHide;
        
        [Serializable]
        public sealed class ButtonInfo
        {
            public GameObject BtnRoot;
            public Localize Label;
            [SerializeField]
            private Image _icon;

            public Action OnClick;

            public void SetActive(bool value)
            {
                BtnRoot.SetActive(value);
            }

            public ButtonInfo SetCallback(Action action)
            {
                OnClick = action;
                return this;
            }
            
            public ButtonInfo SetLabelValue(string key, Sprite sprite = null)
            {
                Label.SetTerm(key);
                
                if (_icon != null)
                {
                    _icon.gameObject.SetActive(sprite != null);
                    _icon.sprite = sprite;
                }
                
                return this;
            }

            public void Inject()
            {
                BtnRoot.GetComponent<Button>().onClick.AddListener(OnClickCallback);
            }

            private void OnClickCallback()
            {
                OnClick?.Invoke();
            }
        }

        public ButtonInfo PositiveBtn => _positiveBtn;
        public ButtonInfo NegativeBtn => _negativeBtn;

        private void Awake()
        {
            _instance = this;
            
            _positiveBtn.Inject();
            _negativeBtn.Inject();
        }

        [Button]
        private void ApplySize()
        {
            _globalContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _zeroGlobalSize + ((RectTransform)_message.transform).sizeDelta.y);
        }

        public void SetParameter(string key, string value)
        {
            _params.SetParameterValue(key, value);
        }

        public void Show(string titleKey, string messageKey, float messageSize)
        {
            _globalContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _zeroGlobalSize + messageSize);
            ((RectTransform)_message.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, messageSize);
            
            _title.SetTerm(titleKey);
            _message.SetTerm(messageKey);
            
            _canvasGroup.blocksRaycasts = true;
            
            if(_lastTween != null && !_lastTween.IsComplete())
                _lastTween.Complete();
            
            _lastTween = _canvasGroup
                .DOFade(1, (1 - _canvasGroup.alpha) / _speedFade)
                .SetEase(_showEase)
                .SetAutoKill(false)
                .OnComplete(() =>
                {
                    OnNextShow?.Invoke();
                    OnNextShow = null;
                });
            _lastTween.Play();
        }

        public void Hide()
        {
            _canvasGroup.blocksRaycasts = false;
            
            if(_lastTween != null && !_lastTween.IsComplete())
                _lastTween.Complete();
            
            _lastTween = _canvasGroup
                .DOFade(0, _canvasGroup.alpha / _speedFade)
                .SetEase(_hideEase)
                .SetAutoKill(false)
                .OnComplete(() =>
                {
                    OnNextHide?.Invoke();
                    OnNextHide = null;
                });
            _lastTween.Play();
        }
    }
}
