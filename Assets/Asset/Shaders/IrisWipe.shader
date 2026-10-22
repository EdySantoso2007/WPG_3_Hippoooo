Shader "UI/IrisWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Center ("Pusat (viewport 0..1)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (tinggi layar = 1)", Float) = 0
        _Aspect ("Aspect Layar", Float) = 1.7778
        _Softness ("Kelembutan Tepi", Float) = 0.003
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _Center;
            float _Radius;
            float _Aspect;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Jarak dari pusat lingkaran (dikoreksi aspect supaya bulat, bukan lonjong)
                float2 p = (i.uv - _Center.xy) * float2(_Aspect, 1.0);
                float d = length(p);

                // Dalam lingkaran = transparan (scene terlihat), luar lingkaran = warna Raw Image (hitam)
                float a = smoothstep(_Radius - _Softness, _Radius, d);

                return fixed4(i.color.rgb, i.color.a * a);
            }
            ENDCG
        }
    }
}