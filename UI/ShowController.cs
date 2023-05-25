using System;
using System.Collections;
using DG.Tweening;
using Firebase.Analytics;
using GameRules.Core.Runtime.Modules;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Modules;
using GameRules.Scripts.UI.Background;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ShowController : MonoBehaviour
    {
        [SerializeField]
        private ViewGroup _group;
        [SerializeField]
        private string _titleLocKey;
        
        [SerializeField, Required]    
        private CanvasGroup _canvasGroup;
        
        [SerializeField]
        private StateAnimation _playStateOnAwake;
        [SerializeField]
        private StateAnimation _currentState;
        
        [SerializeField, Range(0.1f, 2f)]
        private float _timeShow;
        [SerializeField, Range(0.1f, 2f)]
        private float _timeHide;
        
        private float _time;
        private float _target;
        private float _velocity;
        
        public CanvasGroup CanvasGroup => _canvasGroup;

        public StateAnimation CurrentState => _currentState;

        public event Action OnBeginOpen;
        public event Action OnBeginHide;

        private bool _activeAnalytic;

        [SerializeField, BoxGroup("TrackScreen")]
        private bool _trackScreen;
        [SerializeField, BoxGroup("TrackScreen")]
        private string _windowName;
        
        [SerializeField]
        private bool _isCanBackBtn;
        
        [SerializeField]
        private BackgroundPreset _backgroundPreset;
        
        public enum StateAnimation
        {
            None,
            Show, 
            Hide
        }

        public void SetBackground(BackgroundPreset backgroundPreset)
        {
            _backgroundPreset = backgroundPreset;
        }

        private void Awake()
        {
            if (CurrentState == StateAnimation.Show)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            switch (_playStateOnAwake)
            {
                case StateAnimation.Hide:
                    Hide();
                    break;
                case StateAnimation.Show:
                    Show();
                    break;
                case StateAnimation.None:
                    enabled = false;
                    break;
            }
            _activeAnalytic = true;
        }
        
        public void Show(bool isMoment = false)
        {
            if(CurrentState == StateAnimation.Show)
                return;

            if(_trackScreen && !string.IsNullOrWhiteSpace(_windowName))
                CrowdAnalyticsMediator.Instance.PushCurrentScreen(_windowName);
            
            OnBeginOpen?.Invoke();
            if(!string.IsNullOrEmpty(_windowName))
                WindowsManager.ActiveWindows.Add(_windowName);

            if (_backgroundPreset != null)
                BackgroundManager.Select(_backgroundPreset);
            
            if (!isMoment)
            {
                if (_group != null)
                    _group.Show(this, _titleLocKey);
                
                if (_group != null && _group.WaitShowTime > 0.01f)
                {
                    StartCoroutine(Wait(_group.WaitShowTime, () =>
                    {
                        BeginAnimation(StateAnimation.Show, 0, 1, _timeShow);
                        
                        if(_isCanBackBtn)
                            StartCoroutine(WaitBack());
                    }));
                }
                else
                {
                    BeginAnimation(StateAnimation.Show, 0, 1, _timeShow);
                    
                    if(_isCanBackBtn)
                        StartCoroutine(WaitBack());
                }
            }
            else
            {
                _currentState = StateAnimation.Show;
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                
                if(_isCanBackBtn)
                    StartCoroutine(WaitBack());
            }
        }

        private IEnumerator Wait(float time, Action onComplete)
        {
            time += Time.unscaledTime;
            while (Time.unscaledTime < time)
                yield return null;
            onComplete?.Invoke();
        }

        private IEnumerator WaitBack()
        {
            while (CurrentState != StateAnimation.Hide)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Hide();
                    yield break;
                }
                yield return null;
            }
        }

        public void Hide(bool isMoment = false)
        {
            if(CurrentState == StateAnimation.Hide)
                return;
            
            OnBeginHide?.Invoke();

            if (!isMoment)
            {
                if (_group != null && _group.CanHide)
                {
                    _group.Hide();
                    StartCoroutine(Wait(0.2f, () => BeginAnimation(StateAnimation.Hide, 1, 0, _timeHide)));
                }
                else
                    BeginAnimation(StateAnimation.Hide, 1, 0, _timeHide);
            }
            else
            {
                _currentState = StateAnimation.Hide;
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                
                WindowsManager.ActiveWindows.Remove(_windowName);
            }
        }

        private void BeginAnimation(StateAnimation state, float from, float to, float length)
        {
            enabled = true;
            
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = from;
            _target = to;
            _velocity = 0;
            
            _currentState = state;
            _time = length;
            
            //_canvasGroup.alpha = Mathf.SmoothDamp(_canvasGroup.alpha, _target, ref _velocity, _time);
        }

        // Update is called once per frame
        private void Update()
        {
            if (_time > 0)
            {
                _time -= Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.SmoothDamp(_canvasGroup.alpha, _target, ref _velocity, Mathf.Max(0, _time));
                return;
            }

            _canvasGroup.alpha = _target;
            if (CurrentState == StateAnimation.Hide)
            {
                _canvasGroup.blocksRaycasts = false;
                WindowsManager.ActiveWindows.Remove(_windowName);
            }
            enabled = false;
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
#endif
    }
}
