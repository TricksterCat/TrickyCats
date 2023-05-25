using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    [UpdateAfter(typeof(BeginJointConversionSystem))]
    [UpdateBefore(typeof(EndJointConversionSystem))]
    public class PhysicsJointConversionSystem : GameObjectConversionSystem
    {
        void CreateJoint(BaseJoint joint)
        {
            if (!joint.enabled)
                return;

            joint.EntityA = GetPrimaryEntity(joint.LocalBody);
            joint.EntityB = joint.ConnectedBody == null ? Entity.Null : GetPrimaryEntity(joint.ConnectedBody);

            joint.Create(DstEntityManager, this);
        }

        // Update is called once per frame
        protected override void OnUpdate()
        {
            Entities.ForEach((LimitDOFJoint joint) => { CreateJoint(joint); });
        }
    }
}