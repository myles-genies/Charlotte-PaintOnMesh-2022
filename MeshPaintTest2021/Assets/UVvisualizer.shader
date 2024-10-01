Shader "Custom/UVvisualizer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ProjectorUV("ProjectorUV", Vector) = (0,0,0,0)    // min uv, max uv of Projector
        _Alpha("Alpha", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ProjectorUV;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed2 Pmin = _ProjectorUV.xy;
                fixed2 Pmax = _ProjectorUV.zw;
                fixed2 crop = step(Pmin, i.uv);
                crop *= step(i.uv, Pmax);

                fixed4 col;
                col.rg = i.uv;
                col.b = 0.0f;

                col.rgb = lerp(col.rgb, texColor.rgb, crop.x * crop.y);
                col.a = _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
