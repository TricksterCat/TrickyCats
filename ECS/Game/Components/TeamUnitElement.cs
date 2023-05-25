using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Game.Components
{
    [InternalBufferCapacity(0)]
    [GenerateAuthoringComponent]
    public struct TeamUnitElement : IBufferElementData
    {
        public float2 Position;
    }
}