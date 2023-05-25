using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Components
{
    public struct VelocityComponent : IComponentData
    {
        public float3 Value;
    }
}