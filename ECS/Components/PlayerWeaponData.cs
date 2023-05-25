using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    public struct PlayerWeaponData : IComponentData
    {
        public float CooldownTo;
        public float NextRegenerate;
        public int Ammo;
    }
}