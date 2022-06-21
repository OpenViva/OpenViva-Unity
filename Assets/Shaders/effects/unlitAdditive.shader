Shader "Unlit/Additive"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
        
	}
	SubShader
	{

		Tags {
			"Queue"="Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100
		Blend One One
		Ztest off
		Zwrite off
		Cull Back

		Pass
		{
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
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed4 col = tex2D( _MainTex, i.uv );
				return fixed4( col.rgb, 1. );
			}
			ENDCG
		}
    }
}
