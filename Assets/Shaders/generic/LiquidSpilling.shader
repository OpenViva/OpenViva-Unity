Shader "SP2/LiquidSpilling"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (0.7,0.7,1.,0.4)
	}

	SubShader
	{
		LOD 100

		Cull off
		//ZWrite Off
		
		Tags {
			"Queue"="Transparent-1"
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
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed2 uv : TEXCOORD0;
				fixed3 worldRefl : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float4 _Color;
			sampler2D _MainTex;
			
			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir(worldPos) );
				o.worldRefl = reflect( -worldViewDir, worldNormal );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed3 cubemap = UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, -i.worldRefl ).rgb;
				fixed tex = tex2D( _MainTex, i.uv ).r;
				fixed4 col = _Color;
				col.rgb += cubemap*3.;
				col.a *= tex;
				//return fixed4( 1.-i.uv.y, 0., 0., 1. );
				return col;
			}
			ENDCG
		}
	}
}
