using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace GameRules.Scripts.ECS.Components
{
    [RequiresEntityConversion]
    public class BulletPrefab : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject Entity;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new BulletPrefabComponent
            {
                Prefab = conversionSystem.GetPrimaryEntity(Entity)
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Entity);
        }
    }
}