using UnityEditor;
using UnityEngine;

namespace GameRules.Scripts.AnimationSystem.AnimMapBakerTools.Editor
{
    public static class AnimationShaderShaderGUI 
    {
        private static Material _material;
        
        private static readonly int RAnimLen = Shader.PropertyToID("_RAnimLen");
        private static readonly int RemapRange = Shader.PropertyToID("_RemapRange");
        private static readonly int AnimMap = Shader.PropertyToID("_AnimMap");

        private static BakeAnimation _selectAnimation;
        private static int _id;
        
        private static GUIContent _selectBakeAnimation = new GUIContent("Select BakeAnimation");

        [MenuItem("CONTEXT/Material/Inject BakeAnimation...", priority = 10000)]
        public static void Create(MenuCommand command)
        {
            _material = (Material)command.context;
            _id = EditorGUIUtility.GetControlID(_selectBakeAnimation, FocusType.Keyboard);
            EditorGUIUtility.ShowObjectPicker<BakeAnimation>(null, false, "", _id);

            _selectAnimation = null;
            
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            UpdatePicker();
            if (_id == -1)
                EditorApplication.update -= OnUpdate;
        }

        private static void UpdatePicker()
        {
            if (_material == null || _material.Equals(null) || EditorGUIUtility.GetObjectPickerControlID() != _id)
            {
                _id = -1;
                return;
            }
            
            var result = EditorGUIUtility.GetObjectPickerObject() as BakeAnimation;
            if(result == null)
                return;
            if (_selectAnimation != result)
            {
                _selectAnimation = result;
                
                var animation = result;
                _material.SetFloat(RAnimLen, animation.RAnimLength);
                _material.SetVector(RemapRange, new Vector4(animation.RemapRange.x, animation.RemapRange.y));
                _material.SetTexture(AnimMap, animation.Texture);
            }
        }
    }
}