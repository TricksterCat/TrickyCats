using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Components
{
    [GenerateAuthoringComponent]
    public struct BulletComponent : IComponentData
    {
        public bool RequestDestroy;
    }
}