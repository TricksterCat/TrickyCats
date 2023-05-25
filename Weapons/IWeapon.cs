using Players;
using UnityEngine;

namespace GameRules.Scripts.Weapons
{
    public interface IWeapon
    {
        string Name { get; }
        
        float Distance { get; }
        float Cooldown { get; }
        float Dispersion { get; }
        int BulletFireCount { get; }
        
        int Ammo { get; }
        float AmmoRegenerateInSecond { get; }
        int AmmoCountRegenerate { get; }
        float BulletSpeed { get; }

        void Initialize();

        bool CanFire(IPlayerController player);

        void Fire(Vector3 from, Vector3 dir, IPlayerController player);
    }
}