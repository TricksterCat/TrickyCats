using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Game.Components
{
    public struct GameSetting
    {
        public float MatchTime;
        
        public int MaxSpawnUnit;
        public int SpawnUnitsOnStartGame;
        public float2 SpawnUnitsPerSecond;
        
        public float UnitScorePerSecond;
        public float UnitScoreFromRecruit;
        
        public float UnitColliderSize;
        public float UnitRecruitForce;
        public float LockedIfRecruitTime;
        
        public float PlayerRecruitForce;
        public float BulletRecruitForce;
    }
}