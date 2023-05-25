using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace GameRules.Scripts.ECS.Events
{
    [GenerateAuthoringComponent]
    public struct UnitPrefabs : IComponentData
    {
        public const int TeamCounts = 5;
        
        public Entity NonTeam;
        public Entity Team1;
        public Entity Team2;
        public Entity Team3;
        public Entity Team4;

        public unsafe Entity this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint) index >= UnitPrefabs.TeamCounts)
                    throw new System.ArgumentException($"index must be between[0...{UnitPrefabs.TeamCounts}]");
#endif
                
                fixed (UnitPrefabs* array = &this)
                {
                    return ((Entity*)array)[index];
                }
            }
        }
    }
}