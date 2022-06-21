Shader "Effects/HighlightMesh"
{
	Properties
	{
		_Color ("Color", Color) = (0,0,1,1)
		_Outline ("Outline", float) = 1.0
	}
	SubShader
	{
		
		Tags {
			"Queue" = "Geometry+1"
			"RenderType"="Transparent"
		}
		LOD 100

		Cull back
		ZWrite Off
		ZTest Less

		Blend One One


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

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _Color;
			float _Outline;
			
			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				worldPos += UnityObjectToWorldNormal( v.normal )*_Outline;
				o.vertex = mul( UNITY_MATRIX_VP, float4(worldPos,1.0) );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                return _Color;
			}
			ENDCG
		}
	}
}
