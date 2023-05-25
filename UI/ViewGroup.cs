using System.Collections;
using DG.Tweening;
using I2.Loc;
using UnityEngine;

namespace GameRules.UI
{
    public class ViewGroup : MonoBehaviour
    {
        [SerializeField]
        private bool _canHide;
        [SerializeField]
        private string _showAnimationId;
        [SerializeField]
        private float _waitShowTime = 0.35f;
        [SerializeField]
        private string _rootAnimationHide;
        
        private ShowController _lastController;
        [SerializeField]
        private Localize _localizeTitle;
        [SerializeField]
        private DOTweenAnimation _titleAnimation;

        [SerializeField]
        private bool _replaceControllers;

        private Coroutine _changeTitle;
        
        public bool CanHide => _canHide;
        public float WaitShowTime => _waitShowTime;

        private IEnumerator ChangeTitle(string titleKey)
        {
            _titleAnimation.DOPlayBackwards();
            if(_titleAnimation.tween.IsPlaying())
                yield return _titleAnimation.tween.WaitForCompletion();
            _localizeTitle.SetTerm(titleKey);
            _titleAnimation.DOPlayForward();
            
            _changeTitle = null;
        }
        
        public void Show(ShowController controller, string titleKey)
        {
            if (!string.IsNullOrEmpty(titleKey))
            {
                if(_titleAnimation == null)
                    _localizeTitle.SetTerm(titleKey);
                else
                {
                    if(_changeTitle != null)
                        StopCoroutine(_changeTitle);
                    _changeTitle = StartCoroutine(ChangeTitle(titleKey));
                }
            }
            else if (_titleAnimation != null)
            {
                if (_changeTitle != null)
                {
                    StopCoroutine(_changeTitle);
                    _changeTitle = null;
                }
                _titleAnimation.DOPlayBackwards();
            }
            
            if (_replaceControllers && _lastController != null)
                _lastController.Hide();

            _lastController = controller;
            if(_replaceControllers)
                controller.transform.SetAsLastSibling();

            if(!string.IsNullOrEmpty(_showAnimationId))
                DOTween.Restart(_showAnimationId);
        }

        public void Hide()
        {
            if(!CanHide)
                return;
            
            var controller = _lastController;
            if(controller == null)
                return;
            
            _lastController = null;
            
            if(!string.IsNullOrEmpty(_rootAnimationHide))
                DOTween.Restart(_rootAnimationHide);
            else if (!string.IsNullOrEmpty(_showAnimationId))
                DOTween.PlayBackwards(_showAnimationId);
            
            controller.Hide();
        }
    }
}