Shader "Custom/ScreenSpaceDecal"
{
	// This shader goes on decal projector objects
	Properties{
		_MainTex("Main Texture", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1, 1, 1, 1)
		_AngleCutoff("Angle CutOff", Range(0,90)) = 88
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
			Pass
			{
				ZWrite Off
				ZTest Off
				Lighting Off
				Cull Back

				Blend SrcAlpha OneMinusSrcAlpha

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				uniform sampler2D _MainTex;
				float4 _MainTex_ST;
				uniform half4 _Color;
				uniform float _AngleCutoff;

				// make sure Camera has DepthTexture ON
				uniform sampler2D _CameraDepthTexture;

				struct appdata
				{
					float4 vertex : POSITION;
				};

				struct v2f
				{
					float4 position :SV_POSITION;
					float4 screenPos : TEXCOORD0;
					float4 viewRay : TEXCOORD2;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.position = UnityObjectToClipPos(v.vertex);
					// used to get screen UVs for sampling depth texture
					// (ComputeScreenPos takes clip -w, w and gives 0, w)
					o.screenPos = ComputeScreenPos(o.position);

					// View ray: we want a positive ray from camera to vertex, so:
					// 1. start with view space position (camera at origin)
					float3 viewPos = UnityObjectToViewPos(v.vertex);

					// 2. negate z (because Unity view space is right-handed, meaning z is negative
					//  moving forward from camera, and it needs to be positive)
					o.viewRay.xyz = viewPos * float3(1, 1, -1);

					// In the next 3 steps, the view ray needs to be scaled by far/z, except the z division
					// must be done in the fragment shader (perspective divide)
					// 3. save the z value in the 4th component, so we can divide the vector in the frag shader
					o.viewRay.w = o.viewRay.z;

					// 4. scale the view ray by the far plane value
					o.viewRay.xyz *= _ProjectionParams.z;

					return o;
				}


				fixed4 frag(v2f i) : Color
				{
					// 5. divide by saved view space z which represents world-scale depth from camera (in
					//  fragment shader)
				    i.viewRay.xyz = i.viewRay.xyz / i.viewRay.w;

				    // 6. Lookup camera depth sample:
				    // normalize screenUV:  0,w -> 0,1 (again, perspective divide in fragment shader
				    // because this will vary by position across fragment)
				    float2 screenUV = i.screenPos.xy / i.screenPos.w;
					// Get linear depth in the current pixel (0->1)
				    float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
					//float val = depth;
					//return fixed4(val, val, val, 1.0);

					// Get new projected viewspace position, then convert to world then object space:
					// viewRay was first scaled by far/zOld, now it's scaled by sampled depth (zNew/far), so
					// it's like replacing the depth of this decal projector object with that of the covered objects
				    float4 prjPos = float4(i.viewRay.xyz * depth, 1);
				    float3 worldPos = mul(unity_CameraToWorld, prjPos).xyz;
				    float4 objPos = mul(unity_WorldToObject, float4(worldPos, 1));

					// Prevent applying to areas nearly perpendicular to projector plane where it would distort
					float3 objectSpaceNormal = normalize(cross(ddx(objPos), ddy(objPos)));
					float dot_nz = dot(objectSpaceNormal, float3(0, 0, 1));
					float angle = 1.0 - (_AngleCutoff / 90.0);
					float angle_cutoff = dot_nz > angle ? 1.0 : 0.0;
					float angle_fade = smoothstep(0, 0.05, dot_nz - angle);

				    clip(float3(0.5, 0.5, 0.5)*angle_cutoff - abs(objPos.xyz));
					// Generate UVs from new projected position, using xz-plane as the projector plane:
				    //half2 uv = _MainTex_ST.xy * (objPos.xz + 0.5);
					// Generate UVs from new projected position, using xy-plane as the projector plane:
				    half2 uv = _MainTex_ST.xy * (objPos.xy + 0.5);
				    return tex2D(_MainTex, uv) * _Color * angle_fade;
			    }
			ENDCG
		}
		}
			FallBack "Unlit/Transparent"
}