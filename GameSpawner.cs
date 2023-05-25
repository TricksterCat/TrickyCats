using System;
using System.Collections;
using GameRules;
using GameRules.Firebase.Runtime;
using GameRules.Scripts;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class GameSpawner : MonoBehaviour
{
    public static int MaxUnits { get; private set; }
    public static int StartSpawn { get; private set; }
    public static float2 SpawnPerSecond { get; private set; }
    public static float ScorePerSecond { get; private set; }
    public static float ScorePerHunt { get; private set; }

    [SerializeField]
    private int _spawnStartCount;
    [SerializeField]
    private int _maxUnit;
    [SerializeField, MinMaxSlider(0f, 20f, true)]
    private Vector2Int _spawnPerSecond = Vector2Int.zero;

    [SerializeField]
    private float _bonusHunt;
    [SerializeField]
    private float _bonusCrowdByTime;
    
    private void Awake()
    {
        MaxUnits = _maxUnit;
        StartSpawn = _spawnStartCount;
        SpawnPerSecond = new float2(_spawnPerSecond.x, _spawnPerSecond.y);

        ScorePerSecond = _bonusCrowdByTime;
        ScorePerHunt = _bonusHunt;
        
        if (GetOrPush.TryGetConfig(LoadingController.MapName, out var config))
        {
            try
            {
                if (config.TryGetValue("max_units", out var jMaxUnits))
                    MaxUnits = (int)jMaxUnits;
                if (config.TryGetValue("spawn_onStart", out var spawnOnStart))
                    StartSpawn = (int)spawnOnStart;
                if (config.TryGetValue("spawn_perSecond", out var spawnPerSecond))
                    SpawnPerSecond = new float2((int)spawnPerSecond["min"], (int)spawnPerSecond["max"]);
                
                if (config.TryGetValue("score_perHunt", out var scorePerHunt))
                    ScorePerHunt = (int)scorePerHunt;
                if (config.TryGetValue("score_perSecond", out var scorePerSecond))
                    ScorePerSecond = (int)scorePerSecond;
            }
            catch (Exception e)
            {
                FirebaseApplication.LogException(e);
            }
        }
        
        Resources.LoadAsync<GameObject>("Game").completed += operation =>
        {
            var go = Instantiate((GameObject) ((ResourceRequest) operation).asset);
            go.GetComponent<GameScene>().Initialize();
        };
        Destroy(gameObject);
    }
}
