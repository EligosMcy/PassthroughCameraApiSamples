
Shader "UI/RadialStretchXR"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        // —— 拉伸参数 ——
        _Center ("Center (UV 0-1)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (0-1, short side)", Range(0.0, 1.5)) = 0.4
        _Strength ("Strength (-1..2)", Range(-1.0, 2.0)) = 0.5
        _Falloff ("Falloff (0..1)", Range(0.0, 1.0)) = 0.8
        _Aspect ("Aspect (width/height)", Float) = 1.0

        // Unity 会自动传 RawImage.uvRect 的 ST
        [HideInInspector] _MainTex_ST ("_MainTex_ST", Vector) = (1,1,0,0)

        // —— UI Mask/Stencil 兼容（与 UI/Default 一致）——
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RadialStretchXR"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // —— XR/Instancing & UI Clip Keywords ——
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                float4 worldPosition : TEXCOORD1; // for RectMask2D clipping
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float2 _Center;
            float  _Radius;
            float  _Strength;
            float  _Falloff;
            float  _Aspect;

            float4 _ClipRect;
            float  _UseUIAlphaClip;

            UNITY_INSTANCING_BUFFER_START(PerMaterial)
            UNITY_INSTANCING_BUFFER_END(PerMaterial)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.worldPosition = v.vertex; // UI clip rect 使用
                return o;
            }

            // 0..1 -> 权重
            float smoothFalloff(float t, float falloff)
            {
                float a = saturate(t);
                float s = smoothstep(0.0, 1.0, a);
                return lerp(1.0 - a, 1.0 - s, falloff);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // RectMask2D 裁剪（与 UI/Default 一样）
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipUV = i.worldPosition.xy;
                float alphaClip = UnityGet2DClipping(clipUV, _ClipRect);
                if (alphaClip <= 0) discard;
                #endif

                float2 uv = i.uv;

                // 去除 ST，把 uv 映射回 RawImage 内部 0..1（用于计算）
                float2 baseUV = (uv - _MainTex_ST.zw) / _MainTex_ST.xy;

                float2 c = _Center;
                float2 dir = baseUV - c;

                // 宽高比修正，半径以“短边”归一化
                float2 dirNorm = float2(dir.x / _Aspect, dir.y);
                float dist = length(dirNorm);

                if (dist > _Radius || _Radius <= 1e-4)
                {
                    fixed4 col0 = tex2D(_MainTex, uv) * i.color;
                    #ifdef UNITY_UI_ALPHACLIP
                    if (_UseUIAlphaClip > 0.5) clip(col0.a - 0.001);
                    #endif
                    return col0;
                }

                float t = dist / _Radius;
                float w = smoothFalloff(t, _Falloff);
                float scale = 1.0 + _Strength * w;
                scale = max(scale, 1e-4);

                float2 dirSrcNorm = dirNorm / scale;
                float2 dirSrc = float2(dirSrcNorm.x * _Aspect, dirSrcNorm.y);
                float2 pSrc = c + dirSrc;

                float2 uvSrc = pSrc * _MainTex_ST.xy + _MainTex_ST.zw;

                fixed4 col = tex2D(_MainTex, uvSrc) * i.color;

                #ifdef UNITY_UI_ALPHACLIP
                if (_UseUIAlphaClip > 0.5) clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Unlit/Transparent"
}
