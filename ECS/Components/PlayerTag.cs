using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Components
{
    public struct PlayerTag : IComponentData
    {
        public bool IsBot;
        public float2 NextPosition;
    }
}