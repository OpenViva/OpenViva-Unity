// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "SP2/Glass"
{
	Properties
	{
		_Color ("Color", Color) = (0.7,0.7,1.,0.4)
	}

	SubShader
	{
		LOD 100

		Cull Off
		
		Tags {
			"Queue"="Transparent"
			"RenderType"="Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed3 worldRefl : TEXCOORD0;
				fixed3 toCamDir : TEXCOORD1;
			};

			uniform float4 _Color;
			
			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir(worldPos) );
				o.worldRefl = reflect( -worldViewDir, worldNormal );
				o.toCamDir = normalize( _WorldSpaceCameraPos-worldPos );
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed rim = 1.-saturate( (dot( i.toCamDir, i.worldRefl )+.8)*2. );
				//return fixed4( rim, rim, 0., 1. );
				fixed3 cubemap = UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, i.worldRefl ).rgb;
				fixed3 col = cubemap*_Color;
				col += col*rim;
				return fixed4( col, saturate(0.25+rim*2.) );
			}
			ENDCG
		}
	}
}
