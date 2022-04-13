Shader "Anime/AnimeTexShadowStraight"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_OutSizeMin ("Outline Size Min",Range(0.,0.0005)) = 0.0005
		_OutSizeMax ("Outline Size Max",Range(0.,0.0005)) = 0.0005
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"
	ENDCG

	SubShader
	{
		LOD 100

		Pass
		{
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm: NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float _OutSizeMin;
			uniform float _OutSizeMax;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 diff = _WorldSpaceCameraPos.xyz-worldPos;
				fixed dist = ( diff.x*diff.x+diff.y*diff.y+diff.z*diff.z )*0.002;
				fixed outlineScale = max( _OutSizeMin, min( _OutSizeMax, dist ) );
				o.vertex = UnityObjectToClipPos(v.vertex+v.norm*outlineScale);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed3 col = tex2D(_MainTex, i.uv).rgb;
				col = col*.5-.2;
				return fixed4( col, 1. );
			}
			ENDCG
		}
		
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
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
				fixed3 normDir: TEXCOORD1;
				LIGHTING_COORDS(2,3)
			};

			sampler2D _MainTex;
			sampler2D _ShadowTex;

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normDir = normalize( mul( fixed4(v.normal,0.), unity_WorldToObject ).xyz );
				o.uv = v.uv;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed worldRim = saturate(.5+dot( _WorldSpaceLightPos0, i.normDir ));
				//worldRim = 1.-pow(1.-worldRim,8.);
				fixed atten = saturate( (LIGHT_ATTENUATION(i)-.5)*2.*worldRim );
				//return fixed4( atten, 0., 0., 1. );
				fixed3 color = tex2D( _MainTex, i.uv ).rgb;
				color *= lerp( 0.5f, 1.0f, atten );

				return fixed4( color.rgb, 1. );
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
