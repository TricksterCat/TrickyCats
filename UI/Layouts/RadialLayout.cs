
/// Credit Danny Goodayle 
/// Sourced from - http://www.justapixel.co.uk/radial-layouts-nice-and-simple-in-unity3ds-ui-system/
/// Updated by ddreaper - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.
/// Chid Layout fix by John Hattan - enables an options 

/*
Radial Layout Group by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("Layout/Extensions/Radial Layout")]
    public class RadialLayout : LayoutGroup
    {
        private enum SizeAffect
        {
            None = -1, 
            X = 0, 
            Y = 1
        }
        
        public float fDistance;
        [Range(0f, 360f)]
        public float MinAngle, MaxAngle, StartAngle;
        public bool OnlyLayoutVisible = false;

        [SerializeField, BoxGroup("Rotation")]
        private bool _useRotation;
        [SerializeField, Range(0f, 360f), BoxGroup("Rotation")]
        private float _startRotation;

        [SerializeField]
        private SectorInfo[] _sectorInfos;

        private List<Image> _backs;

        [SerializeField]
        private RectTransform _backsRoot;

        [SerializeField]
        private Color[] _colors;
        
        [System.Serializable]
        private struct SectorInfo
        {
            public float Score;
        }

        protected override void OnEnable()
        {
            if(_backs == null)
                _backs = new List<Image>();
            _backs.Clear();
            _backsRoot.GetComponentsInChildren(_backs);
            base.OnEnable(); CalculateRadial();
        }
        public override void SetLayoutHorizontal()
        {
        }
        public override void SetLayoutVertical()
        {
        }
        public override void CalculateLayoutInputVertical()
        {
            CalculateRadial();
        }
        public override void CalculateLayoutInputHorizontal()
        {
            CalculateRadial();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateRadial();
        }
#endif
        void CalculateRadial()
        {
            m_Tracker.Clear();
            if (transform.childCount == 0 || _sectorInfos.Length < 2)
                return;

            var totalScore = _sectorInfos.Sum(sector => sector.Score);
            if(totalScore < 1)
                return;
            
            int ChildrenToFormat = 0;
            if (OnlyLayoutVisible)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    RectTransform child = (RectTransform)transform.GetChild(i);
                    if ((child != null) && child.gameObject.activeSelf)
                        ++ChildrenToFormat;
                }
            }
            else
            {
                ChildrenToFormat = transform.childCount;
            }

            var count = math.min(_backs.Count, _sectorInfos.Length);
            
            float fAngle = 0f;
            int index = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = (RectTransform)transform.GetChild(i);
                if (child != null && (!OnlyLayoutVisible || child.gameObject.activeSelf))
                {
                    if (index == count)
                        continue;

                    var back = _backs[index];
                    var sectorInfo = _sectorInfos[index];
                    var value = sectorInfo.Score / totalScore;
                    float fOffsetAngle = 360 * value;
                    index++;
                    
                    back.color = _colors[index % _colors.Length];
                    back.enabled = true;
                    back.fillAmount = value;
                    back.transform.localEulerAngles = new Vector3(0, 0, fAngle);
                    
                    fAngle += fOffsetAngle / 2f;
                    
                    //Adding the elements to the tracker stops the user from modifying their positions via the editor.
                    m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition);
                    Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
                    child.localPosition = vPos * fDistance;
                    //Force objects to be center aligned, this can be changed however I'd suggest you keep all of the objects with the same anchor points.
                    child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);

                    
                    if (_useRotation)
                    {
                        var angle = child.localEulerAngles;
                        angle.z = _startRotation + fAngle;
                        child.localEulerAngles = angle;
                    }
                    
                    fAngle += fOffsetAngle / 2f;
                }
            }
            
            for (int i = index; i < _backs.Count; i++)
                _backs[i].enabled = false;
        }
    }
}
