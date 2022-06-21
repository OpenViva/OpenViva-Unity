Shader "SP2/TexCast"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"
	ENDCG

	SubShader
	{
		LOD 100

		Cull Back

		Pass
		{
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
			}

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
				float3 normal: NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
				fixed3 normDir: TEXCOORD1;
				LIGHTING_COORDS(2,3)

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;

			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				o.normDir = normalize( mul( fixed4(v.normal,0.), unity_WorldToObject ).xyz );
				o.uv = v.uv;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed worldRim = saturate(dot( _WorldSpaceLightPos0, i.normDir ));
				fixed atten = saturate( LIGHT_ATTENUATION(i)*worldRim );
				//return fixed4( atten, 0., 0., 1. );
				fixed3 color = tex2D( _MainTex, i.uv ).rgb;
				color *= .1+atten*.9;

				return fixed4( color.rgb, 1. );
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
