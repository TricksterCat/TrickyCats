using System;

namespace GameRules.Scripts.Weapons
{
    public class PlayerWeaponData
    {
        public PlayerWeaponData(IWeapon weapon)
        {
            _weapon = weapon;
            Ammo = weapon.Ammo;
        }

        private IWeapon _weapon;
        
        public float Cooldown { get; private set; }
        public int Ammo { get; private set; }
        public float NextRegen { get; private set; }

        public bool TryFire(float time)
        {
            if (Cooldown > time || Ammo < _weapon.BulletFireCount)
                return false;

            if (Ammo == _weapon.Ammo)
                NextRegen = time + _weapon.AmmoRegenerateInSecond;
            
            Cooldown = time + _weapon.Cooldown;
            Ammo -= _weapon.BulletFireCount;
            return true;
        }

        public void Regen(float time)
        {
            if(NextRegen > time || Ammo >= _weapon.Ammo)
                return;

            NextRegen = time + _weapon.AmmoRegenerateInSecond;
            Ammo = Math.Min(Ammo + _weapon.AmmoCountRegenerate, _weapon.Ammo);
        }

        public bool CanFire(float time)
        {
            return Cooldown < time && Ammo >= _weapon.BulletFireCount;
        }
    }
}