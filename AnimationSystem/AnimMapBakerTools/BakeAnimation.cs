using Unity.Mathematics;
using UnityEngine;

namespace GameRules.Scripts.AnimationSystem.AnimMapBakerTools
{
    public class BakeAnimation : ScriptableObject
    {
        public static bool IsUseRemap(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGB9e5Float:
                case TextureFormat.RGBAFloat:
                case TextureFormat.RGBAHalf:
                    return false;
                default:
                    return true;
            }
        }
        
        public Texture2D Texture;
        public float RAnimLength;
        public float2 RemapRange;
    }
}