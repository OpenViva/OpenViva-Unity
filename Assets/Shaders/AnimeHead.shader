
Shader "Anime/AnimeHead"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_ToonProximityAmbience ("Toon Proximity Ambience",Color) = (1.,1.,1.)
		_OutlineColor ("Outline color",Color) = (1.,1.,1.)
		_OutSizeMin ("Outline Size Min",Range(0.,0.003)) = 0.001
		_OutSizeMax ("Outline Size Max",Range(0.,0.003)) = 0.001
		_PhotoDataColor ("Photo Data Color",Color) = (0.,1.,0.,1.)
		_Dirt ("Dirt",Range(0.,1)) = 0
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"
	ENDCG

	SubShader
	{
		LOD 100

		Tags {
			"LightMode" = "ForwardBase"
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}

		Pass
		{
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNorm: NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float3 _OutlineColor;
			uniform float _OutSizeMin;
			uniform float _OutSizeMax;
			uniform float3 _ToonProximityAmbience;
			
			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 diff = _WorldSpaceCameraPos.xyz-worldPos;
				fixed dist = ( diff.x*diff.x+diff.y*diff.y+diff.z*diff.z );
				fixed outlineScale = max( _OutSizeMin, min( _OutSizeMax, dist ) );
				o.vertex = UnityObjectToClipPos(v.vertex+v.worldNorm*outlineScale);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                return fixed4( _OutlineColor*UNITY_LIGHTMODEL_AMBIENT, 1. );
			}
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "AnimeShading.cginc"
			#include "GradualDirt.cginc"

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
				fixed3 worldNorm: TEXCOORD1;
				fixed3 worldPos: TEXCOORD2;
				LIGHTING_COORDS(3,4)
				UNITY_FOG_COORDS(5)

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _GlobalDirtTex;
			uniform fixed3 _ToonProximityAmbience;
			uniform fixed _Dirt;

			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNorm = normalize( mul( fixed4(v.normal.x,v.normal.yz,0.), unity_WorldToObject ).xyz );
				o.worldPos = mul( unity_ObjectToWorld, v.vertex );
				o.uv = v.uv;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                //return fixed4( i.worldNorm,1.);
				fixed worldRim = saturate( dot( _WorldSpaceLightPos0, i.worldNorm ) );
				fixed3 color = tex2D( _MainTex, i.uv );
				
				//shadows
				fixed3 worldViewDir = normalize( _WorldSpaceCameraPos.xyz-i.worldPos );	//MOVE TO VERTEX
				fixed camRim = dot( worldViewDir, i.worldNorm );
				fixed atten = LIGHT_ATTENUATION(i);
				fixed sun = saturate( atten*worldRim )*0.4;

				//point lights
				fixed3 ambience = AnimeShade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb,
					unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, i.worldPos, i.worldNorm
				);
				ambience += lerp( UNITY_LIGHTMODEL_AMBIENT, _LightColor0, sun )+_ToonProximityAmbience;

				//composite
				APPLY_GRADUAL_DIRT(color.rgb,_GlobalDirtTex,_Dirt,i.uv)
				color *= saturate( ambience );
				color = ApplyColorFromLight( color, color, sun, camRim, worldRim );
				UNITY_APPLY_FOG(i.fogCoord, color);
				return fixed4( color, 1. );
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
