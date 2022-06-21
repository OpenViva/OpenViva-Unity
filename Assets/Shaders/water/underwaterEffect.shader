Shader "SP2/underwaterEffect"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Tint("Water Color",Color)=(1,1,1)
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
				float4 uvKernel : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			uniform fixed3 _Tint;

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvKernel.xy = TransformStereoScreenSpaceTex( v.uv, o.vertex.w );
				o.uvKernel.zw = float2(3.0,3.0)/_ScreenParams.xy;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				fixed2 offset;
				offset.x = sin( (i.uvKernel.x+_SinTime.w)*13. );
				offset.y = cos( (i.uvKernel.y+_CosTime.w)*11.1+offset.x );
				offset *= 0.0008;
				fixed2 uv = i.uvKernel.xy+offset;
				//round to nearest pixel
				uv.x = floor(uv.x*_ScreenParams.x)/_ScreenParams.x;
				uv.y = floor(uv.y*_ScreenParams.y)/_ScreenParams.y;
				fixed4 col;
				col.rgba = UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv ).rgba*2.0;
				col.rgb += UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv+fixed2( i.uvKernel.z, i.uvKernel.w ) ).rgb;
				col.rgb += UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv+fixed2( i.uvKernel.z, i.uvKernel.w ) ).rgb;
				col.rgb += UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv+fixed2( -i.uvKernel.z, -i.uvKernel.w ) ).rgb;
				col.rgb += UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv+fixed2( i.uvKernel.z, -i.uvKernel.w ) ).rgb;
				col.rgb *= _Tint/5.0;

				return fixed4( col.rgb, 1. );
			}
			ENDCG
		}
    }
}
