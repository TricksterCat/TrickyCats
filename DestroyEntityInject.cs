using Unity.Entities;
using UnityEngine;

public class DestroyEntityInject : MonoBehaviour
{
    private Entity _entity;
    
    private void OnDestroy()
    {
        if(World.DefaultGameObjectInjectionWorld != null)
            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(_entity);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _entity = entity;
    }
}
