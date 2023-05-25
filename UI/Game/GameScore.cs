using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Base.Modules;
using GameRules.Core.Runtime;
using GameRules.Core.Runtime.Modules;
using GameRules.Scripts.Modules.Game;
using GameRulez.Modules.PlayerSystems;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class GameScore : MonoBehaviour
{
    private const float UpdateRate = 0.3f;
    
    [ShowInInspector, ReadOnly]
    private int _playersCount;
    [SerializeField]
    private int _scoreMaxNormal;
    
    [SerializeField]
    private ScoreLine[] _scores;

    private ModuleProxy<IPlayerSystem> _playerSystem;

    private Vector3[] ZeroPosition;
    private float _nextUpdateScoreTable;

    private int[] _positions;
    
    private IEnumerator Start()
    {
        _playerSystem = new ModuleProxy<IPlayerSystem>();
        ZeroPosition = new Vector3[_scores.Length];
        for (var index = 0; index < _scores.Length; index++)
        {
            var score = _scores[index];
            ZeroPosition[index] = score.transform.localPosition;
            score.SetActive(false, null);
        }

        var playerSystem = _playerSystem.Get();
        while (!playerSystem.CompareStatus(ModuleStatus.CompleteInitialize))
            yield return null;
        
        
        var matchController = App.GetModule<IMatchController>();
        while (!(matchController.CompareStatus(ModuleStatus.CompleteInitialize) && matchController.IsMatchActive))
            yield return null;

        var players = playerSystem.GetPlayers();
        _positions = new int[players.Count()];
        foreach (var player in players)
        {
            _positions[_playersCount] = _playersCount + 1;
            var scoreLine = _scores[_playersCount++];
            scoreLine.SetActive(true, player);
        }
    }

    private void Update()
    {
        var delta = Time.deltaTime;
        float totalScore = 0f;
        for (int i = 0; i < _playersCount; i++)
            totalScore += _scores[i].Player.Score;

        totalScore = math.max(totalScore, _scoreMaxNormal);
        
        for (int i = 0; i < _playersCount; i++)
        {
            var score = _scores[i];
            score.UpdateScore(delta, score.Player.Score / totalScore);
        }
        

        var time = Time.time;
        if(time < _nextUpdateScoreTable)
            return;
        _nextUpdateScoreTable = time + UpdateRate;

        for (int index = 0; index < _playersCount; index++)
        {
            var line = _scores[index];
            int position = 1;
            for (int i = 0; i < _playersCount; i++)
            {
                if(i == index)
                    continue;

                var other = _scores[i];
                if (!line.More(other))
                    position++;
            }
            
            var old = _positions[index];
            if (position != old)
            {
                line.MoveTo(ZeroPosition[position - 1]);
                _positions[index] = position;
            }
        }
    }
}
