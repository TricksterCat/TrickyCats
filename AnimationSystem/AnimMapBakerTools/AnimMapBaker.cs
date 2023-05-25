/*
 * Created by jiadong chen
 * http://www.chenjd.me
 * 
 * 用来烘焙动作贴图。烘焙对象使用animation组件，并且在导入时设置Rig为Legacy
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using GameRules.Scripts.AnimationSystem.AnimMapBakerTools;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// 保存需要烘焙的动画的相关数据
/// </summary>
public struct AnimData
{
    #region 字段

    public int vertexCount;
    public int mapWidth;
    public List<AnimationState> animClips;
    public string name;

    private  Animation animation;
    private SkinnedMeshRenderer skin;

    #endregion

    public AnimData(Animation anim, SkinnedMeshRenderer smr, string goName)
    {
        vertexCount = smr.sharedMesh.vertexCount;
        mapWidth = Mathf.NextPowerOfTwo(vertexCount);
        animClips = new List<AnimationState>(anim.Cast<AnimationState>());
        animation = anim;
        skin = smr;
        name = goName;
    }

    #region 方法

    public void AnimationPlay(string animName)
    {
        this.animation.Play(animName);
    }

    public void SampleAnimAndBakeMesh(ref Mesh m)
    {
        this.SampleAnim();
        this.BakeMesh(ref m);
    }

    private void SampleAnim()
    {
        if (this.animation == null)
        {
            Debug.LogError("animation is null!!");
            return;
        }

        this.animation.Sample();
    }

    private void BakeMesh(ref Mesh m)
    {
        if (this.skin == null)
        {
            Debug.LogError("skin is null!!");
            return;
        }

        this.skin.BakeMesh(m);
    }


    #endregion

}

/// <summary>
/// 烘焙后的数据
/// </summary>
public struct BakedData
{
    #region 字段

    public string name;
    public float animLen;
    public byte[] rawAnimMap;
    public int animMapWidth;
    public int animMapHeight;

    public TextureFormat Format;
    public float2 RemapRange;

    #endregion

    public BakedData(string name, float animLen, Texture2D animMap, float2 remapRange)
    {
        this.name = name;
        this.animLen = animLen;
        this.animMapHeight = animMap.height;
        this.animMapWidth = animMap.width;
        this.rawAnimMap = animMap.GetRawTextureData();
        Format = animMap.format;
        RemapRange = remapRange;
    }
}

/// <summary>
/// 烘焙器
/// </summary>
public class AnimMapBaker{

    #region 字段

    private AnimData? animData = null;
    private List<Vector3> vertices = new List<Vector3>();
    private Mesh bakedMesh;

    private List<BakedData> bakedDataList = new List<BakedData>();

    private TextureFormat _textureFormat;

    #endregion

    #region 方法

    public void SetAnimData(GameObject go, TextureFormat textureFormat)
    {
        if(go == null)
        {
            Debug.LogError("go is null!!");
            return;
        }
        _textureFormat = textureFormat;

        Animation anim = go.GetComponent<Animation>();
        SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();

        if(anim == null || smr == null)
        {
            Debug.LogError("anim or smr is null!!");
            return;
        }
        this.bakedMesh = new Mesh();
        this.animData = new AnimData(anim, smr, go.name);
        
        
        bakedDataList.Clear();
    }

    public List<BakedData> Bake()
    {
        if(this.animData == null)
        {
            Debug.LogError("bake data is null!!");
            return this.bakedDataList;
        }
        
        for(int i = 0; i < this.animData.Value.animClips.Count; i++)
        {
            if(!this.animData.Value.animClips[i].clip.legacy)
            {
                Debug.LogError(string.Format("{0} is not legacy!!", this.animData.Value.animClips[i].clip.name));
                continue;
            }

            BakePerAnimClip(this.animData.Value.animClips[i]);
        }

        return this.bakedDataList;
    }

    float3 Unity_Remap_half3(float3 In, float2 InMinMax)
    {
        return (In - InMinMax.x) / (InMinMax.y - InMinMax.x);
    }
    
    private void BakePerAnimClip(AnimationState curAnim)
    {
        int curClipFrame = 0;
        float sampleTime = 0;
        float perFrameTime = 0;

        curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.clip.frameRate * curAnim.length));
        perFrameTime = curAnim.length / curClipFrame;
        
        int height = curClipFrame;
        int width = animData.Value.mapWidth;

        Texture2D animMap = new Texture2D(width, height, _textureFormat, false);
        animMap.name = string.Format("{0}_{1}.animMap", this.animData.Value.name, curAnim.name);
        this.animData.Value.AnimationPlay(curAnim.name);

        var useRemap = BakeAnimation.IsUseRemap(_textureFormat);
        var colors = new Color[width * height];
        
        float2 remapRange = new float2(float.MaxValue, float.MinValue);
        if (useRemap)
        {
            var nativeArray = new NativeArray<float3>(width * height, Allocator.Temp);
        
            var lines = new NativeArray<int>(height, Allocator.Temp);
            for (int i = 0; i < height; i++)
            {
                var line = i * width;
                curAnim.time = sampleTime;
                animData.Value.SampleAnimAndBakeMesh(ref bakedMesh);

                var vertices = bakedMesh.vertices;
                lines[i] = vertices.Length;
            
                for(int j = 0, jMax = vertices.Length; j < jMax; j++)
                {
                    float3 vertex = vertices[j];
                    nativeArray[line + j] = new float3(vertex.x, vertex.y, vertex.z);

                    var min = math.cmin(vertex);
                    var max = math.cmax(vertex);

                    remapRange.x = math.min(remapRange.x, min);
                    remapRange.y = math.max(remapRange.y, max);
                }
                sampleTime += perFrameTime;
            }
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = i * width;
                for (int j = 0, jMax = lines[i]; j < jMax; j++)
                {
                    var remap = Unity_Remap_half3(nativeArray[line + j], remapRange);
                    colors[line + j] = new Color(remap.x, remap.y, remap.z);
                }
            }
            
            nativeArray.Dispose();
            lines.Dispose();
        }
        else
        {
            for (int i = 0; i < height; i++)
            {
                var line = i * width;
                curAnim.time = sampleTime;
                animData.Value.SampleAnimAndBakeMesh(ref bakedMesh);

                var vertices = bakedMesh.vertices;
                for(int j = 0, jMax = vertices.Length; j < jMax; j++)
                {
                    float3 vertex = vertices[j];
                    colors[line + j] = new Color(vertex.x, vertex.y, vertex.z);
                }
                sampleTime += perFrameTime;
            }
        }

        
        animMap.SetPixels(colors);
        animMap.Apply();
        
        //colors.Dispose();
        
        this.bakedDataList.Add(new BakedData(animMap.name, curAnim.clip.length, animMap, remapRange));
    }

    #endregion


    #region 属性


    #endregion

}