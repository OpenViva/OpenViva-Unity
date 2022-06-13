Shader "Effects/NoScreenEffectDefault"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
        _CloudsRT ("Cloud Render Texture", 2DArray) = "" {}
	}
	SubShader
	{
		LOD 100

		Pass
		{
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
			}
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
            #pragma require 2darray
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			UNITY_DECLARE_TEX2DARRAY(_CloudsRT);

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TransformStereoScreenSpaceTex( v.uv, o.vertex.w );
				return o;
			}

			fixed3 applyClouds( fixed4 color, fixed4 clouds ){
				fixed cloudAlpha = step(color.a,0.0)*clouds.a*0.9;
				return color.rgb*(1.-cloudAlpha)+clouds.rgb*cloudAlpha;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    			fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				fixed4 clouds_col = UNITY_SAMPLE_TEX2DARRAY(_CloudsRT, float3(i.uv.xy, (float)unity_StereoEyeIndex));
#else
				fixed4 clouds_col = UNITY_SAMPLE_TEX2DARRAY(_CloudsRT, float3(i.uv.xy, 0.0));
#endif
				col.rgb = applyClouds( col, clouds_col );
				return col;
			}
			ENDCG
		}
    }
}
