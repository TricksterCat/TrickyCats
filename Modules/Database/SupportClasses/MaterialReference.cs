using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameRules.Scripts.Modules.Database.SupportClasses
{
    [Serializable]
    public class MaterialReference : AssetReferenceT<Material>
    {
        public MaterialReference(string guid) : base(guid)
        {
            
        }
    }
}