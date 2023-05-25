using Unity.Collections;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

namespace GameRules.Scripts.ECS.Render.Static
{
    public class StaticMeshGroupConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            using (var context = new BlobAssetComputationContext<Hash128, GroupInfosArray>(BlobAssetStore, 128, Allocator.Temp))
            {
                Entities.ForEach((StaticRendererGroup meshRenderGroup) =>
                {
                    if(!meshRenderGroup.IsReadyToConvert(out var hash))
                        return;
                    context.AssociateBlobAssetWithUnityObject(hash, meshRenderGroup.gameObject);

                    BlobAssetReference<GroupInfosArray> reference;
                    if (context.NeedToComputeBlobAsset(hash))
                    {
                        reference = meshRenderGroup.GetBlobReference();
                        context.AddBlobAssetToCompute(hash, hash);
                        context.AddComputedBlobAsset(hash, reference);
                    }
                    else
                        context.GetBlobAsset(hash, out reference);

                    meshRenderGroup.CompleteConvert(GetPrimaryEntity(meshRenderGroup), DstEntityManager, reference);
                });
            }
        }
    }
}