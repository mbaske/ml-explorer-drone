
Shader "Sensors/StackedDepth"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Exponent ("Exponent", int) = 1
		_RenderTex("RenderTex", 2D) = "white" {}
		[Toggle] _Snapshot("Snapshot", Range(0, 1)) = 0
	}
    SubShader
	{
        Tags
		{
            "RenderType" = "Opaque"
        }

        Pass
		{
			// https://forum.unity.com/threads/problem-rendering-ui-to-rendertexture-on-linux.577768/#post-3859360
			ZWrite On 
			ZTest Always
			
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _RenderTex;
            float _Snapshot;
            int _Exponent;

            struct v2f
			{
                float4 pos: SV_POSITION;
                float4 scrPos: TEXCOORD1;
            };

            v2f vert(appdata_base v)
			{
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.scrPos = ComputeScreenPos(o.pos);
                return o;
            }

            half4 frag(v2f i): COLOR
			{
                float4 buffer = tex2D(_RenderTex, i.scrPos.xy);

                if (_Snapshot == 1)
				{
                    float depth = 1 - Linear01Depth(
                        tex2Dproj(_CameraDepthTexture,
                            UNITY_PROJ_COORD(i.scrPos)).r);

					buffer.b = buffer.g;
					buffer.g = buffer.r;
					buffer.r = pow(depth, _Exponent);
					buffer.a = 1;
				}
				
				return buffer;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}