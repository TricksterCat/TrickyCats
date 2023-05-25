using GameRules.Scripts.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
#if ENABLE_HYBRID_RENDERER
using Unity.Rendering;
#endif
using UnityEngine;

namespace GameRules.Scripts.ECS.Render
{
    public class UnitRenderProxy : MonoBehaviour
    {
        [SerializeField]
        private bool _showBounds;
        
        public Bounds Bounds;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<UnitRenderComponent>(entity);
            dstManager.AddComponentData(entity, new RenderBounds
            {
                Value = Bounds.ToAABB()
            });
            
#if !ENABLE_HYBRID_RENDERER
            dstManager.AddComponent<WorldRenderBounds>(entity);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (_showBounds)
            {
                var l2w = transform.localToWorldMatrix;
                AABB aabb = AABB.Transform(l2w, new AABB
                {
                    Center = Bounds.center,
                    Extents = Bounds.extents
                });
                Gizmos.DrawWireCube(aabb.Center, aabb.Size);
            }
        }
    }
    
    public struct UnitRenderComponent : IComponentData
    {
        public float StartAnimation;
    }
    
    
#if !ENABLE_HYBRID_RENDERER
    [WriteGroup(typeof(WorldRenderBounds))]
    public struct RenderBounds : IComponentData
    {
        public AABB Value;
    }
    
    public struct WorldRenderBounds : IComponentData
    {
        public AABB Value;
    }
#endif
}