using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Components
{
    [GenerateAuthoringComponent]
    public struct MapInfo : ISystemStateComponentData
    {
        public int2 Start;
        public int2 Size;
    }
}