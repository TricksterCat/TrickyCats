using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    public struct BulletPrefabComponent : IComponentData
    {
        public Entity Prefab;
    }
}