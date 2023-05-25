using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    public struct CollisionComponent : IComponentData
    {
        public int Force;
        public byte TeamNext;
    }
}