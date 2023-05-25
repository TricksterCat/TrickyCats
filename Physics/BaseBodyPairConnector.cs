using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public abstract class BaseBodyPairConnector : MonoBehaviour
    {
        public PhysicsBodyAuthoring LocalBody
        {
            get
            {
                var body = GetComponent<PhysicsBodyAuthoring>();
                if(body == null)
                    body = GetComponentInParent<PhysicsBodyAuthoring>();
                return body;
            }
        }

        public PhysicsBodyAuthoring ConnectedBody;

        public RigidTransform worldFromA => LocalBody == null
            ? RigidTransform.identity
            : Math.DecomposeRigidBodyTransform(LocalBody.transform.localToWorldMatrix);

        public RigidTransform worldFromB => ConnectedBody == null
            ? RigidTransform.identity
            : Math.DecomposeRigidBodyTransform(ConnectedBody.transform.localToWorldMatrix);


        public Entity EntityA { get; set; }

        public Entity EntityB { get; set; }


        void OnEnable()
        {
            // included so tick box appears in Editor
        }

        public abstract void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem);
    }
}