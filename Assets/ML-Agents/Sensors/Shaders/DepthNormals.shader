Shader "Sensors/DepthNormals" 
{
	Properties
	{
		_MainTex("", 2D) = "white" {}
		_Exponent ("Exponent", int) = 1
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Pass
		{
			// https://forum.unity.com/threads/problem-rendering-ui-to-rendertexture-on-linux.577768/#post-3859360
			ZWrite On 
			ZTest Always
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _CameraDepthNormalsTexture;
			int _Exponent;

			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 scrPos: TEXCOORD1;
			};

			v2f vert(appdata_base v) 
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.scrPos = ComputeScreenPos(o.pos);
				o.scrPos.y = 1 - o.scrPos.y; // TODO
				return o;
			}

			sampler2D _MainTex;

			half4 frag(v2f i) : COLOR
			{
				float depth = 0;
				// Use 2D view space normals (xy), ignore decoded 3D normals.
				float3 normal;
				float4 sample = tex2D(_CameraDepthNormalsTexture, i.scrPos.xy);
				DecodeDepthNormal(sample, depth, normal);

				if (depth > 1)
				{
					// Don't show normals beyond max depth.
					sample.x = 0;
					sample.y = 0;
				}
				// Inverse decoded depth (closer is brighter).
				sample.z = pow(1 - depth, _Exponent);
				sample.w = 1;
				
				return sample;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
