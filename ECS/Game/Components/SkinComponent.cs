using Unity.Entities;

namespace GameRules.Scripts.ECS.Game.Components
{
    [GenerateAuthoringComponent]
    public struct SkinComponent : IComponentData
    {
        public Entity Link;
    }
}