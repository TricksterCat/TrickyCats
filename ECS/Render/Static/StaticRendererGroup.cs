using System;
using System.Collections.Generic;
using GameRules.RustyPool.Runtime;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using Object = UnityEngine.Object;

namespace GameRules.Scripts.ECS.Render.Static
{
    public class StaticRendererGroup : MonoBehaviour
    {
        [NonSerialized]
        private List<Object> _tmpAssets;
        [NonSerialized]
        private Dictionary<GroupInfoManagedLink, NativeList<(Matrix4x4 l2w, AABB bounds)>> _tmpDict;

        private void OnEnable()
        {
            
        }

        public bool IsReadyToConvert(out Hash128 hash)
        {
            var assets = _tmpAssets = TmpList<Object>.Get();
            
            var renderers = TmpList<MeshRenderer>.Get();
            var dic = _tmpDict = new Dictionary<GroupInfoManagedLink, NativeList<(Matrix4x4 l2w, AABB bounds)>>();
            
            gameObject.GetComponentsInChildren(renderers);
            
            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if(meshFilter == null || meshFilter.sharedMesh == null || renderer.sharedMaterial == null || !renderer.enabled)
                    continue;
                
                var l2w = renderer.localToWorldMatrix;
                var bounds = renderer.bounds.ToAABB();

                var meshIndex = assets.IndexOf(meshFilter.sharedMesh);
                if (meshIndex == -1)
                {
                    meshIndex = assets.Count;
                    assets.Add(meshFilter.sharedMesh);
                }

                var materialArray = renderer.sharedMaterials;
                for (int j = 0; j < materialArray.Length; j++)
                {
                    var materialIndex = assets.IndexOf(materialArray[j]);
                    if (materialIndex == -1)
                    {
                        materialIndex = assets.Count;
                        assets.Add(materialArray[j]);
                    }
                
                    var managedLink = new GroupInfoManagedLink
                    {
                        Mesh = meshIndex, 
                        Material = materialIndex,
                        SubMeshIndex = j
                    };

                    if (!dic.TryGetValue(managedLink, out var data))
                    {
                        data = new NativeList<(Matrix4x4 l2w, AABB bounds)>(Allocator.Temp);
                        dic[managedLink] = data;
                    }
                    
                    data.Add((l2w, bounds));
                }
            }
            TmpList<MeshRenderer>.Release(renderers);

            if (dic.Count == 0)
            {
                TmpList<Object>.Release(assets);
                
                hash = default;
                return false;
            }
            
            var instanceId = gameObject.GetInstanceID();
            var assetsHash = assets.GetHashCode();
            
            hash = new Hash128(new uint4(
                instanceId > 0 ? (uint) instanceId : 0u,
                instanceId < 0 ? (uint) math.abs(instanceId) : 0u, 
                assetsHash > 0 ? (uint) assetsHash : 0u,
                assetsHash < 0 ? (uint) math.abs(assetsHash) : 0u));

            return true;
        }

        public BlobAssetReference<GroupInfosArray> GetBlobReference()
        {
            var bb = new BlobBuilder(Allocator.Temp);
            ref var groupInfosRoot = ref bb.ConstructRoot<GroupInfosArray>();
            var dataArray = bb.Allocate(ref groupInfosRoot.Values, _tmpDict.Count);
            int index = 0;
            foreach (var kvp in _tmpDict)
            {
                var source = kvp.Value;
                ref var group = ref dataArray[index++];
                
                group.Managed = kvp.Key;
                
                var positions = bb.Allocate(ref group.Positions, source.Length);
                var bounds = bb.Allocate(ref group.Bounds, source.Length);

                for (int i = 0; i < source.Length; i++)
                {
                    positions[i] = source[i].l2w;
                    bounds[i] = source[i].bounds;
                }
            }

            var result = bb.CreateBlobAssetReference<GroupInfosArray>(Allocator.Persistent);
            bb.Dispose();
            
            foreach (var list in _tmpDict.Values)
                list.Dispose();

            _tmpDict = null;

            return result;
        }

        public void CompleteConvert(Entity entity, EntityManager dstManaget, BlobAssetReference<GroupInfosArray> reference)
        {
            dstManaget.AddComponentData(entity, new StaticMeshGroup
            {
                Data = reference
            });
            dstManaget.AddComponentData(entity, new StaticMeshGroupResources
            {
                Assets = TmpList<Object>.ReleaseAndToArray(_tmpAssets)
            });
        }
    }
}