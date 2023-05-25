using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Base.Attributes;
using Core.Base.Modules;
using DefaultNamespace;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.ModuleAdapters.Runtime;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.UI.Results;
using GameRules.Scripts.Weapons;
using GameRules.TaskManager.Runtime;
using GameRulez.Modules.PlayerSystems;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace GameRules.Scripts.Modules.Game
{
    public enum ScoreEvent
    {
        AddUnit
    }
    
    [BaseModule]
    public interface IMatchController : IModule
    {
        bool IsMatchActive { get; }
        
        void BeginMatch();

        event Action OnMathBegin;
        event Action OnMathEnd;
        
    }
    
    public class MatchController : IMatchController
    {
        private ModuleStatus _status;
        public ModuleStatus Status => _status;
        
        [SerializeField] 
        private int _hitScoreBonus = 100;
        [SerializeField] 
        private float _multiplicationScoreBonus = 1;

        private static GameMatchSystem _matchSystem;

        [ShowInInspector, HideInEditorMode]
        public bool IsMatchActive => _matchSystem != null && _matchSystem.TimeToEndMatch > 0;

        public event Action OnMathEnd;
        public event Action OnMathBegin;
        
        public void Initialize()
        {
            Debug.Log("MatchController:: BeginInitialize...");
            _status = ModuleStatus.Enable | ModuleStatus.ProcessingInitialize;

            App.GetModule<ITaskSystem>().Subscribe(CompleteInitialize());
        }

        private void OnMatchBegin()
        {
            
        }

        private IEnumerator CompleteInitialize()
        {
            while (FirebaseApplication.Status == StatusInitialize.Wait)
                yield return null;
            
            Debug.Log("MatchController:: CompleteInitialize...");
            _status.ReplaceFlags(ModuleStatus.ProcessingInitialize, ModuleStatus.CompleteInitialize);
            
            GameMatchSystem.OnMatchBegin += OnMatchBegin;
        }

        public void SetEnable(bool value)
        {
            ModuleExtension.SetEnable(ref _status, value);
        }

        public void GetDetails(StringBuilder detailsBuilder, out object rootGameObject)
        {
            rootGameObject = null;
        }

        public void BeginMatch()
        {
            OnMathBegin?.Invoke();
            
            var timeToEndMatch = RemoteConfig.GetInt("TimeMatch");

            var gameSetting = new GameSetting
            {
                MatchTime = timeToEndMatch,
                UnitScorePerSecond = GameSpawner.ScorePerSecond,
                UnitScoreFromRecruit = GameSpawner.ScorePerHunt,
                UnitColliderSize = 0.5f,
                UnitRecruitForce = 75,
                PlayerRecruitForce = 150,
                BulletRecruitForce = 150,
                LockedIfRecruitTime = 0.5f
            };
            App.GetModule<IWorldGenerator>().Fill(ref gameSetting);
            App.GetModule<IWeaponSystem>().Fill(ref gameSetting);

            _matchSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<GameMatchSystem>();
            _matchSystem.StartMatch(gameSetting);
            //App.GetModule<ITaskSystem>().AddUpdate(this);
        }
        

        public void Dispose()
        {
            
        }
    }
}
