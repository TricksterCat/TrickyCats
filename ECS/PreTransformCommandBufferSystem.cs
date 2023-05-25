using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace DefaultNamespace
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(TRSToLocalToWorldSystem))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    public class PreTransformCommandBufferSystem : EntityCommandBufferSystem
    {
        
    }
}