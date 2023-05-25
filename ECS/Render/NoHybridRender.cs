using UnityEngine;

#if ENABLE_HYBRID_RENDERER
using System.Collections;
using Unity.Entities;
using Unity.Rendering;
#endif

namespace GameRules.Scripts.ECS.Render
{
    public class NoHybridRender : MonoBehaviour
#if ENABLE_HYBRID_RENDERER
        , IConvertGameObjectToEntity
#endif
    {
#if ENABLE_HYBRID_RENDERER
        private Entity _entity;

        private IEnumerator Start()
        {
            yield return null;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            if (!entityManager.HasComponent<DisableRendering>(_entity))
                entityManager.AddComponent<DisableRendering>(_entity);
            
            if (entityManager.HasComponent<RenderMesh>(_entity))
                entityManager.RemoveComponent<RenderMesh>(_entity);
        }
        
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _entity = entity;
            
            dstManager.AddComponent<FrozenRenderSceneTag>(entity);
            dstManager.AddComponent<DisableRendering>(entity);
            
            if (dstManager.HasComponent<RenderMesh>(entity))
                dstManager.RemoveComponent<RenderMesh>(entity);
        }
#endif
    }
}