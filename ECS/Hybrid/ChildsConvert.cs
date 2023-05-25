using Unity.Entities;
using UnityEngine;

namespace GameRules.Scripts.ECS.Hybrid
{
    public class ChildsConvert : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            conversionSystem.DeclareLinkedEntityGroup(gameObject);
        }
    }
}