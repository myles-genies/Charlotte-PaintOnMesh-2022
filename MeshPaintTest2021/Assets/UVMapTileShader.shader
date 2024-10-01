Shader "Custom/UVMapTileShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _ProjectorTex("ProjectorTexture", 2D) = "black" {}
        _ProjectorUV("ProjectorUV", Vector)=(0,0,0,0)    // min uv, max uv of Projector
        _TargetUV("TargetUV", Vector)=(0,0,0,0)          // min uv, max uv of Target
        _Color("Ink Color", Color)=(0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
                float3 normal : NORMAL;
            };

            struct v2f
            { 
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _ProjectorTex;
            float4 _ProjectorTex_ST;
            fixed4 _ProjectorUV;
            fixed4 _TargetUV;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                // Normal rendering: object to homogenous clip space
                //o.vertex = UnityObjectToClipPos(v.vertex);

                // UV space rendering
                float2 uv = v.uv.xy;
                if (_ProjectionParams.x < 0) {
                    // different graphics APIs have different vertical tex coord conventions
                    // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                    uv.y = 1 - uv.y;
                }
                // map uvs to NDC space and set the vertices to this
                uv = 2. * uv - 1.;
                o.vertex = float4(uv.xy, 0., 1.);

                // continue as usual
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 prevColor = tex2D(_MainTex, i.uv);

                fixed2 Pmin = _ProjectorUV.xy;
                fixed2 Pmax = _ProjectorUV.zw;
                fixed2 Tmin = _TargetUV.xy;
                fixed2 Tmax = _TargetUV.zw;

                fixed2 puv = Pmin + (i.uv - Tmin) / (Tmax - Tmin) * (Pmax - Pmin);
                fixed4 projColor = tex2D(_ProjectorTex, puv);

                fixed4 color = projColor;

                _Color.rg = puv;
                _Color.b = 0.0f;
                _Color.a = 1.0f;
                //color = _Color;

                fixed2 crop = step(Tmin, i.uv);
                crop *= step(i.uv, Tmax);
                color = color * crop.x * crop.y;

                // composite new brush strokes over previous
                return color + prevColor * (1 - color.a);
            }
            ENDCG
        }
    }
}
