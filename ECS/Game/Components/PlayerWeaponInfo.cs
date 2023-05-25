using Unity.Entities;

namespace GameRules.Scripts.ECS.Game.Components
{
    public struct PlayerWeaponInfo : IComponentData
    {
        public int AmmoMax;
        
        public float CooldownTime;
        
        public int AmmoRegenerate;
        public float RegenerateTime;
        
        public float Range;
        public float Speed;
        
        public float Dispersion;
    }
}