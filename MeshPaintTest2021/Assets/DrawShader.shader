Shader "Custom/DrawShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _BrushWidth ("BrushWidth", Float) = 0.05
        _ProjectedTex("ProjectedTexture", 2D) = "black" {}
        _Coordinate("Coordinate", Vector)=(0,0,0,0)
        _HitUV("HitUV", Vector)=(0,0,0,0)
        _Color("Ink Color", Color)=(1,0,0,1)
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
                float2 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _ProjectedTex;
            float4 _ProjectedTex_ST;
            fixed _BrushWidth;
            fixed4 _Coordinate;
            fixed4 _HitUV;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                // what you usually do: render into 3D clip space
                //o.vertex = UnityObjectToClipPos(v.vertex);
                // what we're going to do: render into uv space
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
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // use to avoid drawing on backside
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 prevColor = tex2D(_MainTex, i.uv);
                fixed4 projColor = tex2D(_ProjectedTex, _Coordinate.xy);
                //fixed draw = pow(saturate(1 - distance(i.worldPos, _Coordinate.xyz)), 50);
                fixed draw = (_BrushWidth - min(_BrushWidth, distance(_HitUV.xy, i.uv)))/_BrushWidth;

                //_Color.rg = i.uv;
                //_Color.b = 0.0f;
                _Color.rgb = projColor.rgb;
                _Color.a = 1.0f;
                fixed4 color = _Color * draw;

                // don't paint on backside
                //color *= dot(i.worldNormal, _Coordinate.xyz);

                // composite new brush strokes over previous
                return color + prevColor * (1 - color.a);
            }
            ENDCG
        }
    }
}
