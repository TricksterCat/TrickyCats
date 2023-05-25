using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    [GenerateAuthoringComponent]
    public struct RotationSpeed : IComponentData
    {
        public float Value;
    }
}