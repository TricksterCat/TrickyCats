using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace GameRules.Scripts.ECS.Game.Components
{
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    public class LimitDOFJointV2 : MonoBehaviour, IConvertGameObjectToEntity
    {
        public bool3 LockLinearAxes;
        public bool3 LockAngularAxes;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            RigidTransform bFromA = math.mul(math.inverse(RigidTransform.identity), Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix));
            
            var physicsConstrainedBodyPair = new PhysicsConstrainedBodyPair(entity,Entity.Null, false);
            var limitJoint = LimitDOFJoint.CreateLimitDOFJoint(bFromA, LockLinearAxes, LockAngularAxes);

            dstManager.AddComponentData(entity, physicsConstrainedBodyPair);
            dstManager.AddComponentData(entity, limitJoint);
        }
    }
}