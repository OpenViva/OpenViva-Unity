Shader "Anime/AnimeMatcapTextured"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Matcap ("Matcap Texture", 2D) = "white" {}
		_OutlineColor ("Outline color",Color) = (1.,1.,1.)
		_OutSizeMin ("Outline Size Min",Range(0.,0.0005)) = 0.0005
		_OutSizeMax ("Outline Size Max",Range(0.,0.0005)) = 0.0005
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
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			
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
				UNITY_FOG_COORDS(1)
			};

			uniform float3 _OutlineColor;
			uniform float _OutSizeMin;
			uniform float _OutSizeMax;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 diff = _WorldSpaceCameraPos.xyz-worldPos;
				fixed dist = ( diff.x*diff.x+diff.y*diff.y+diff.z*diff.z )*0.000005;
				fixed outlineScale = max( _OutSizeMin, min( _OutSizeMax, dist ) );
				o.vertex = UnityObjectToClipPos(v.vertex+v.norm*outlineScale);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_APPLY_FOG(i.fogCoord, col);
				return fixed4( _OutlineColor, 1. );
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
				float3 norm: NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				float2 uv : TEXCOORD0;
				float3 norm: TEXCOORD1;
				float3 worldView: TEXCOORD2;
				LIGHTING_COORDS(3,4)
			};

			sampler2D _MainTex;
			sampler2D _Matcap;

			float2 getMatcapUV( float3 eye, float3 normal ){
				float3 refl = reflect(eye, normal);
				float m = 2.8284271247461903*sqrt( refl.z+1. );
				return .5+(refl.xy/m)*.5;
			}

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.norm = normalize( UnityObjectToWorldNormal( v.norm ) );
				o.worldView = normalize( WorldSpaceViewDir( v.vertex ) );
				o.uv = v.uv;
				TRANSFER_VERTEX_TO_FRAGMENT(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 matcapUV = getMatcapUV( normalize(i.worldView), normalize( i.norm ) );
				float shadow = LIGHT_ATTENUATION(i);
				fixed3 matcap = tex2D(_Matcap, matcapUV*shadow);
				fixed3 color = tex2D( _MainTex, i.uv );
				fixed3 finalColor = color*matcap;
				return fixed4( finalColor, 1. );
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
