using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.UI.Leaderboards
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField]
        private ShowController _window;
        [SerializeField]
        private ShowController _content;
        [SerializeField]
        private GameObject _loading;

        [SerializeField]
        private LederboardLine[] _lines;
        
        [SerializeField]
        private Toggle[] _toggles;
        [SerializeField]
        private CanvasGroup _modeCanvasGroup;
        
        [SerializeField]
        private ScrollRect _scroll;
        
        [ShowInInspector, ReadOnly]
        private Mode _currentMode;

        private bool _requestOnChangeMode;

        [SerializeField] 
        private TextMeshProUGUI _globalRank;

        
        public enum Mode
        {
            Week = 0,
            Month = 1,
            Total = 2
        }
    
        private async void Request()
        {
            _globalRank.enabled = false;
            
            _modeCanvasGroup.interactable = false;
            _loading.SetActive(true);
            _content.Hide(true);

            var result = await ServerRequest.Instance.GetLeaderboard(_currentMode);
            
            SetValues(result.Scores, out var playerIndex);

            if (result.Scores.Length > 0 && playerIndex > 0)
                SnapTo((RectTransform) _lines[playerIndex].transform);

            if (result.IsSuccess)
            {
                _globalRank.text = $"<b>{result.PlayerRank} /</b> <color=#00695C>{result.TotalPlayers}</color>";
                _globalRank.enabled = true;
            }
            
            _content.Show();
            _loading.SetActive(false);
            _modeCanvasGroup.interactable = true;
        }

        private void SnapTo(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();

            _scroll.content.anchoredPosition = (Vector2)_scroll.viewport.InverseTransformPoint(_scroll.content.position) - (Vector2)_scroll.viewport.InverseTransformPoint(target.position) - new Vector2(0, _scroll.viewport.rect.height / 2);
        }

        public void Awake()
        {
            for (int i = 0; i < _toggles.Length; i++)
            {
                var mode = (Mode)i;
                _toggles[i].onValueChanged.AddListener(isActive =>
                {
                    if (isActive && _currentMode != mode)
                    {
                        _currentMode = mode;
                        if(_requestOnChangeMode)
                            Request();
                    }
                });
            }

            _toggles[0].isOn = true;
        }

        public void Open()
        {
            _requestOnChangeMode = true;
            
            _window.Show();
            Request();
        }

        public struct UserScore
        {
            public bool Self;
            public string Name;
            public int Score;
        }

        private void SetValues(UserScore[] results, out int playerIndex)
        {
            playerIndex = 0;
            
            var count = Math.Min(results.Length, _lines.Length);
            for (int i = 0; i < count; i++)
            {
                var result = results[i];
                _lines[i].SetValues(result.Name, result.Score, result.Self);
                _lines[i].gameObject.SetActive(true);

                if (result.Self)
                    playerIndex = i;
            }

            for (int i = count; i < _lines.Length; i++)
                _lines[i].gameObject.SetActive(false);
        }
    }
}
