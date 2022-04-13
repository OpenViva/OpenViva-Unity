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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float _Outline;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				worldPos += UnityObjectToWorldNormal( v.normal )*_Outline;
				o.vertex = mul( UNITY_MATRIX_VP, float4(worldPos,1.0) );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return _Color;
			}
			ENDCG
		}
	}
}
