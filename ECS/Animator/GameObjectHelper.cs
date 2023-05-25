using Core.Base;
using UnityEngine;

namespace GameRules.Scripts.ECS.Animator
{
    public static class GameObjectHelper
    {
        public static string GetPathInPrefab(GameObject root, GameObject target)
        {
            if (root == null)
                return null;
            var result = TmpList<string>.Get();

            var targetTransform = target.transform;
            while (targetTransform != null && targetTransform.gameObject != root)
            {
                result.Add(targetTransform.name);
                targetTransform = targetTransform.parent;
            }
            
            result.Reverse();
            return string.Join("/", TmpList<string>.ReleaseAndToArray(result));
        }

        #if UNITY_EDITOR
        public static string GetPathInPrefab(GameObject go)
        {
            if (go == null)
                return null;
            
            var prefabRoot = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return GetPathInPrefab(prefabRoot, go);
        }
        #endif
    }
}