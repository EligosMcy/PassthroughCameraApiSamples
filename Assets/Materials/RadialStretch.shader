
Shader "UI/RadialStretch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        // 中心点（RawImage 内部坐标，0-1）
        _Center ("Center (UV 0-1)", Vector) = (0.5, 0.5, 0, 0)
        // 影响半径（0-1，指相对 RawImage 短边的归一化半径）
        _Radius ("Radius", Range(0.0, 1.5)) = 0.4
        // 拉伸强度（>0 向外拉伸，<0 向内收缩），一般 0~1
        _Strength ("Strength", Range(-1.0, 2.0)) = 0.5
        // 衰减平滑（0=线性，1=更平滑）
        _Falloff ("Falloff", Range(0.0, 1.0)) = 0.8

        // 处理宽高比以保证是“圆形”衰减而不是椭圆
        _Aspect ("Aspect (width/height)", Float) = 1.0

        // 兼容 RawImage.uvRect 的偏移/缩放（Unity 自动赋值）
        [HideInInspector] _MainTex_ST ("_MainTex_ST", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RadialStretch"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float2 _Center;     // (x, y) in 0..1
            float  _Radius;     // 0..1 normalized by short side
            float  _Strength;   // >0 outward stretch
            float  _Falloff;    // 0..1
            float  _Aspect;     // width / height of RawImage rect

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 平滑衰减：0 在中心最强，半径边界处为 0
            float smoothFalloff(float t, float falloff)
            {
                // t: 0..1 (0=中心,1=半径边界)
                // 使用 smoothstep 的可变形状：调整 falloff 控制曲线陡峭度
                float a = saturate(t);
                float s = smoothstep(0.0, 1.0, a);
                return lerp(1.0 - a, 1.0 - s, falloff); // falloff=0 线性，=1 平滑
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 原始 uv（已经包含 RawImage 的 uvRect 变换）
                float2 uv = i.uv;

                // 将 uv 转为 0..1 空间（去掉 _MainTex_ST 的平移缩放）
                // 这里我们需要**以 RawImage 内部 0..1**做变形。
                float2 baseUV = (uv - _MainTex_ST.zw) / _MainTex_ST.xy;

                // 处理宽高比，让半径是“短边归一化”
                // 把坐标缩放到等比空间：x/=aspect, 或 y*=aspect
                float2 p = baseUV;
                float2 c = _Center;
                float2 dir = p - c;

                // 让 x 按 1/aspect 压缩，使得以短边为单位的距离是圆形
                float2 dirNorm = float2(dir.x / _Aspect, dir.y);
                float  dist = length(dirNorm);

                if (dist > _Radius || _Radius <= 0.0001)
                {
                    // 半径外不影响：直接取原 uv
                    return tex2D(_MainTex, uv) * i.color;
                }

                // 计算衰减（中心=最强，半径边界=0）
                float t = dist / _Radius;               // 0..1
                float w = smoothFalloff(t, _Falloff);   // 0..1

                // 我们要实现视觉上的“向外拉伸”：片元位置保持不变，
                // 但采样时要从**更靠内**的地方取像素（逆向映射）
                // r' = r * (1 + k * w)  (屏幕上看到的距离)
                // 逆向采样：r_src = r / (1 + k * w)
                float k = _Strength;
                float scale = 1.0 + k * w;
                scale = max(scale, 0.0001); // 防止除零/负数

                float2 dirSrcNorm = dirNorm / scale;

                // 还原宽高比缩放
                float2 dirSrc = float2(dirSrcNorm.x * _Aspect, dirSrcNorm.y);
                float2 pSrc = c + dirSrc;

                // 把回到 0..1 空间的坐标重新应用 uvRect 的 ST
                float2 uvSrc = pSrc * _MainTex_ST.xy + _MainTex_ST.zw;

                fixed4 col = tex2D(_MainTex, uvSrc) * i.color;
                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Unlit/Transparent"
}
