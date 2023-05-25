using System.Collections;
using System.Collections.Generic;
using Players;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class ScoreLine : MonoBehaviour
{
    [SerializeField]
    private Image[] _colorImages;
    [SerializeField, ChildGameObjectsOnly]
    private TextMeshProUGUI _userNameLabel;
    [SerializeField, ChildGameObjectsOnly]
    private TextMeshProUGUI _scoreLabel;

    [SerializeField]
    private Image _avatar;
    [SerializeField]
    private Vector2 _minMaxWidth;

    private RectTransform _rectTransform;

    private IPlayerController _player;

    private MoveTask _moveTask;
    public IPlayerController Player => _player;

    private int _lastScore = -1;
    
    private void SetName(string name)
    {
        _userNameLabel.text = name;
    }

    private void SetScore(int score)
    {
        if(_lastScore == score)
            return;
        _lastScore = score;
        _scoreLabel.text = "x"+score;
    }

    public void SetActive(bool active, IPlayerController player)
    {
        gameObject.SetActive(active);
        _player = player;
        if(!active)
            return;

        _rectTransform = (RectTransform)transform;
        
        SetName(player.Team.Name);
        SetScore(player.Score);
        
        _rectTransform.sizeDelta = new Vector2(_minMaxWidth.x, _rectTransform.sizeDelta.y);
        _avatar.sprite = player.Team.Skin.Icon;

        var color = _player.Team.PlayerColor;
        foreach (var colorImage in _colorImages)
            colorImage.color = color;
        _scoreLabel.color = color;

        _userNameLabel.color = player.IsMain ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
    }

    public void UpdateScore(float deltaTime, float normalizeWidth)
    {
        SetScore(_player.Score);
        
        _rectTransform.sizeDelta = new Vector2(math.lerp(_minMaxWidth.x, _minMaxWidth.y, normalizeWidth), _rectTransform.sizeDelta.y);
        _moveTask?.Update(deltaTime);
    }
    
    public bool More(ScoreLine other)
    {
        var compare = _player.Score.CompareTo(other._player.Score);
        if (compare == 0)
            compare = _player.Team.Name.CompareTo(other._player.Team.Name);
        return compare > 0;
    }
    
    public void MoveTo(Vector3 target)
    {
        if(_moveTask == null)
            _moveTask = new MoveTask();
        _moveTask.Set(this, target);
    }
    
    private class MoveTask
    {
        private ScoreLine _line;
        private float _time;
        private Vector3 _to;
        
        private Vector3 _velocity;

        public void Set(ScoreLine line, Vector3 target)
        {
            _to = target;
            _line = line;
            _velocity = Vector3.zero;

            _time = 0.4f;
        }

        public void Update(float deltaTime)
        {
            if(_time < 0)
                return;
            
            _time -= deltaTime;
            _time = Mathf.Max(0, _time);

            _line.transform.localPosition = Vector3.SmoothDamp(_line.transform.localPosition, _to, ref _velocity, _time);
        }
    }

}
