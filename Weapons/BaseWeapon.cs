using System;
using System.Collections.Generic;
using GameRules.Core.Runtime;
using GameRules.Core.Runtime.Modules;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Pool;
using Players;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.Weapons
{
    [CreateAssetMenu(fileName = "Weapon.asset", menuName = "GameRules/Weapon")]
    public class BaseWeapon : ScriptableObject, IWeapon
    {
        public const string BulletId = "Bullet";
        
        [SerializeField]
        private string _name;
        
        [SerializeField]
        private float _bulletSpeed;
        [SerializeField]
        private float _distance;
        [SerializeField]
        private float _cooldown;
        [SerializeField]
        private float _dispersion;
        [SerializeField]
        private int _bulletFireCount;
        
        
        [SerializeField]
        private int _ammo;
        [SerializeField]
        private float _ammoRegenerateInSecond;
        [SerializeField]
        private int _ammoCountRegenerate;

        public string Name => _name;

        public float Distance => _distance;
        public float Cooldown => _cooldown;
        public float Dispersion => _dispersion;
        public int BulletFireCount => _bulletFireCount;
        public int Ammo => _ammo;
        public float AmmoRegenerateInSecond => _ammoRegenerateInSecond;
        public int AmmoCountRegenerate => _ammoCountRegenerate;
        public float BulletSpeed => _bulletSpeed;


        private readonly ModuleProxy<IWeaponSystem> _weaponSystem = new ModuleProxy<IWeaponSystem>();

        [System.Serializable]
        private struct WeaponSettings
        {
            public int BulletSpeed;
            public float Distance;
            public float Cooldown;
            public int MaxAmmo;
            public int RegenerateInSecond;
            public int CountRegenerate;
        }
        
        public void Initialize()
        {
            var settingsJson = RemoteConfig.GetString("WeaponSettings");
            if (!string.IsNullOrWhiteSpace(settingsJson))
            {
                var settings = JsonUtility.FromJson<WeaponSettings>(settingsJson);
                _ammo = settings.MaxAmmo;
                _bulletSpeed = settings.BulletSpeed;
                _cooldown = settings.Cooldown;
                _distance = settings.Distance;
                _ammoCountRegenerate = settings.CountRegenerate;
                _ammoRegenerateInSecond = settings.RegenerateInSecond;
            }
        }

        public bool CanFire(IPlayerController player)
        {
            return _weaponSystem.Get().CanFire(player.TeamIndex);
        }

        public void Fire(Vector3 from, Vector3 dir, IPlayerController player)
        {
            if(!_weaponSystem.Get().TryFire(player.TeamIndex))
                return;

            var go = PoolGameObjects.GetNextObject(BulletId);
            var transform = go.transform;
            transform.position = from;
            transform.rotation = Quaternion.LookRotation(dir);
            go.SetActive(true);
            transform.GetComponent<BulletVisual>().Run(player, _bulletSpeed, _distance);
        }
    }
}