Shader "Unlit/ghostNearbyEffect"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
        _Distortion ("Distortion", Range(0.0,0.001)) = 0.0005
        _Strength ("Strength", Range(-1.0,1.0)) = 0.0
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
            uniform fixed _Distortion;
            uniform fixed _Strength;

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

            fixed luma( fixed3 rgb ){
                return dot( rgb, fixed3( 0.378, 0.599, 0.114 ) );
            }

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed2 offset;
				offset.x = sin( (i.uvKernel.x+_SinTime.y)*13. );
				offset.y = cos( (i.uvKernel.y+_CosTime.y)*11.1+offset.x );
				offset *= _Distortion*_Strength;
				fixed2 uv = i.uvKernel.xy+offset;
                
                fixed3 raw = UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, uv ).rgb;
				fixed3 col = luma( UNITY_SAMPLE_SCREENSPACE_TEXTURE( _MainTex, i.uvKernel.xy ).rgb )*_Strength+raw;

				return fixed4( col, 1. );
			}
			ENDCG
		}
    }
}
