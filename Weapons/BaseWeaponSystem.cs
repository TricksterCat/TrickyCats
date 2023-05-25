using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Core.Base.Modules;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.ModuleAdapters.Runtime;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.TaskManager.Runtime;
using GameRulez.Modules.PlayerSystems;
using Players;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace GameRules.Scripts.Weapons
{
    public class BaseWeaponSystem : IWeaponSystem, IUpdateListener
    {
        private ModuleStatus _status;
        public ModuleStatus Status => _status;

        [OdinSerialize, AssetList(Path = "GameRules/Prefabs/Weapons")]
        private ScriptableObject _defaultWeapon;
        
        public IWeapon DefaultWeapon { get; private set; }
        
        
        [NonSerialized, ShowInInspector, ReadOnly, HideInEditorMode]
        private Dictionary<int, PlayerWeaponData> _playerParams;

        string IUpdateListener.UpdateListenerName => nameof(BaseWeaponSystem);
        bool IUpdateListener.IsRequiredUpdate => true;
        
        public void Initialize()
        {
            _status = ModuleStatus.Enable | ModuleStatus.CompleteInitialize;
            
            DefaultWeapon = _defaultWeapon == null ? null : _defaultWeapon as IWeapon;
            DefaultWeapon.Initialize();
            
            _playerParams = new Dictionary<int, PlayerWeaponData>();
            App.GetModule<ITaskSystem>().Subscribe(CompleteInitialize());
        }

        private IEnumerator CompleteInitialize()
        {
            while (FirebaseApplication.Status == StatusInitialize.Wait)
                yield return null;
            
            App.GetModule<ITaskSystem>().AddUpdate(this);
        }

        public IWeapon GetWeapon(string name)
        {
            return DefaultWeapon;
        }

        public void UpdateWeapon(IWeapon weapon, int player)
        {
            _playerParams[player] = new PlayerWeaponData(weapon);
        }

        public bool TryFire(int player)
        {
            if (!_playerParams.TryGetValue(player, out var data))
                return false;

            return data.TryFire(Time.time);
        }

        public PlayerWeaponData GetWeaponData(int playerTeam)
        {
            return _playerParams[playerTeam];
        }

        public bool CanFire(int playerTeam)
        {
            if (!_playerParams.TryGetValue(playerTeam, out var data))
                return false;

            return data.CanFire(Time.time);
        }

        public void Fill(ref GameSetting gameSetting)
        {
            
        }

        public void SetEnable(bool value)
        {
            _status.SetEnable(value);
        }
        
        public void Dispose()
        {
            
        }

        public void GetDetails(StringBuilder detailsBuilder, out object rootGameObject)
        {
            rootGameObject = null;
        }

        void IUpdateListener.Update()
        {
            var playerSystem = App.GetModule<IPlayerSystem>();
                
            if(playerSystem == null || playerSystem.CompareStatus(ModuleStatus.Disable))
                return;

            var time = Time.time;
            foreach (var playerData in _playerParams.Values)
                playerData.Regen(time);
        }
    }
}