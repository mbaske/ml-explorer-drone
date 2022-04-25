
Shader "Sensors/Depth"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Exponent ("Exponent", int) = 1
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
                float depth = 1 - Linear01Depth(
                        tex2Dproj(_CameraDepthTexture,
                            UNITY_PROJ_COORD(i.scrPos)).r);
				
				depth = pow(depth, _Exponent);
				return half4(depth, depth, depth, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}