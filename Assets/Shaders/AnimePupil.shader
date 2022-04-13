Shader "Anime/AnimePupilShader"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_ToonProximityAmbience ("Toon Proximity Ambience",Color) = (1.,1.,1.)
		_PupilShrink ("Pupil Shrink",Range(1.,2.)) = 1.0
		_SideMultiplier ("Side Multipler",Range(-1.,1.)) = 1.0
		_PupilRight ("Pupil Right",Range(-1.,1.)) = 0.0
		_PupilUp ("Pupil Up",Range(-1.,1.)) = 0.0
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
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
				"Queue"="Geometry"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "AnimeShading.cginc"

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
				LIGHTING_COORDS(1,2)
				float3 ambience: TEXCOORD3;
				UNITY_FOG_COORDS(4)
			};

			sampler2D _MainTex;	
			uniform fixed3 _ToonProximityAmbience;
			uniform float _PupilShrink;
			uniform float _PupilRight;
			uniform float _SideMultiplier;
			uniform float _PupilUp;
			
			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				fixed3 worldNorm = normalize( mul( fixed4(v.normal,0.), unity_WorldToObject ).xyz );
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex );
				o.uv = v.uv;
				// o.uv.x = ( o.uv.x-.5 )*_SideMultiplier+.5;
				
				o.uv = (o.uv-0.5)*_PupilShrink+0.5; 
				o.uv.x += _PupilRight*_SideMultiplier;
				o.uv.y -= _PupilUp;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				
				//point lights
				o.ambience = AnimeShade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb,
					unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos, worldNorm
				);
				return o;
			}
			
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 color = tex2D( _MainTex, i.uv );
				
				//shadows
				fixed atten = (LIGHT_ATTENUATION(i)-.5)*2.;
				fixed sun = saturate( atten );
				//composite
				color *= i.ambience.rgb+saturate( lerp( UNITY_LIGHTMODEL_AMBIENT, _LightColor0, sun )+_ToonProximityAmbience );
				UNITY_APPLY_FOG(i.fogCoord, color);
				return fixed4( color, 1. );
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
