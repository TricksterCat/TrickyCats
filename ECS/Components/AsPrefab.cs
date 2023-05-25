using Unity.Entities;
using UnityEngine;

namespace GameRules.Scripts.ECS.Components
{
    public class AsPrefab : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Prefab>(entity);
        }
    }
}