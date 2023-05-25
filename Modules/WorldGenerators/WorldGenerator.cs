using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Core.Base;
using Core.Base.Modules;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules.Game;
using GameRules.TaskManager.Runtime;
using GameRulez.Units;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Random = UnityEngine.Random;

namespace GameRulez.Modules.WorldGenerators
{
    [Serializable]
    public class WorldGenerator : IWorldGenerator
    {
        private ModuleStatus _status;

        public ModuleStatus Status => _status;
        
        [SerializeField]
        private int _spawnStartCount;
        
        [SerializeField]
        private int _maxUnit;
        
        [SerializeField, MinMaxSlider(0f, 20f)]
        private Vector2 _spawnPerSecond;
        
        private float _nextSpawn;
        
        private bool _useFakeRandom;
        private Vector2 _randomResult;

        private float _waitSpawn;
       
        public void Initialize()
        {
            _status = ModuleStatus.Enable | ModuleStatus.ProcessingInitialize;
            _nextSpawn = -1;

            _status.ReplaceFlags(ModuleStatus.ProcessingInitialize, ModuleStatus.CompleteInitialize);
        }

        public void SetEnable(bool value)
        {
            _status.SetEnable(value);
        }

        public void GetDetails(StringBuilder detailsBuilder, out object rootGameObject)
        {
            rootGameObject = null;
        }
        
        public void Fill(ref GameSetting gameSetting)
        {
            gameSetting.MaxSpawnUnit = GameSpawner.MaxUnits;
            gameSetting.SpawnUnitsOnStartGame = GameSpawner.StartSpawn;
            gameSetting.SpawnUnitsPerSecond = GameSpawner.SpawnPerSecond;
        }

        public void Dispose()
        {
            
        }
    }
}