Shader "UI/ChannelSwapTest"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Toggle] _SwapRG ("Swap R <-> G", Float) = 0
        [Toggle] _SwapRB ("Swap R <-> B", Float) = 0
        [Toggle] _SwapGB ("Swap G <-> B", Float) = 0

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;

            float _SwapRG;
            float _SwapRB;
            float _SwapGB;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                // ===== 通道交换测试区 =====
                // 优先级：RG > RB > GB
                if (_SwapRG > 0.5)
                {
                    c.rgb = c.grb;
                }
                else if (_SwapRB > 0.5)
                {
                    c.rgb = c.bgr;
                }
                else if (_SwapGB > 0.5)
                {
                    c.rgb = c.rbg;
                }
                // ===========================

                #ifdef UNITY_UI_CLIP_RECT
                    c.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(c.a - 0.001);
                #endif

                return c;
            }
            ENDCG
        }
    }
}
