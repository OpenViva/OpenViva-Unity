

Shader "Anime/AnimeClothing"
{
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_ToonProximityAmbience ("Toon Proximity Ambience",Color) = (1.,1.,1.)
		_Cutoff("Cutout", Range(0,1)) = 0.5
		_OutSizeMin ("Outline Size Min",Range(0.,0.003)) = 0.001
		_OutSizeMax ("Outline Size Max",Range(0.,0.003)) = 0.001
		_PhotoDataColor ("Photo Data Color",Color) = (0.,1.,0.,1.)
		_Dirt ("Dirt",Range(0.,1)) = 0
	}


	SubShader{
		Tags { "Queue"="Transparent-1" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
        LOD 200
 
        Pass    //shadow pass
        {
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On
            Cull Off
 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};
 
            struct v2f {
                fixed4 pos : SV_POSITION;
                float2 uv : TEXCOORD1;
            };
         
            sampler2D _MainTex;
            fixed _Cutoff;
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }
 
            float4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
		
		Pass{
			Tags{
				"LightMode" = "ForwardBase"
				"RenderType"="Transparent"
				"Queue"="AlphaTest-1"
			}
			Cull off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
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
				float4 tangent: TANGENT;
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
				fixed3 worldPos: TEXCOORD1;
				LIGHTING_COORDS(2,3)
                fixed3 worldNorm: TEXCOORD4;
				fixed3 worldViewDir : TEXCOORD5;
			};
			

			sampler2D _MainTex;
			sampler2D _GlobalDirtTex;
			uniform fixed3 _ToonProximityAmbience;
			uniform fixed _Dirt;

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                fixed3 worldNorm = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul( unity_ObjectToWorld, v.vertex );
				o.worldViewDir = normalize( _WorldSpaceCameraPos.xyz-o.worldPos );	//MOVE TO VERTEX
				fixed vface = dot( o.worldViewDir, worldNorm );
				fixed vfaceSide = step(0.,vface)*2.-1.;
				worldNorm *= vfaceSide;

				fixed3 worldTang = UnityObjectToWorldDir(v.tangent.xyz)*vfaceSide;
                fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                fixed3 worldBitangent = cross(worldNorm, worldTang) * tangentSign;
				
				o.worldNorm = worldNorm;
				o.uv = v.uv;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D( _MainTex, i.uv );
				clip(color.a-.5);
				
				//calculate fragment outline
				fixed3 shadedColor = saturate(color*.7-.2);
				fixed edgeDetected = saturate(
					step( tex2D( _MainTex, i.uv+fixed2(-0.001,0.001) ).a, .5 )+
					step( tex2D( _MainTex, i.uv+fixed2(0.001,0.001) ).a, .5 )+
					step( tex2D( _MainTex, i.uv+fixed2(0.,-0.001) ).a, .5 )
				);
				color.rgb = shadedColor*edgeDetected+color.rgb*(1.-edgeDetected);

				//shadows
				fixed worldRim = saturate( dot( _WorldSpaceLightPos0, i.worldNorm ) );
				fixed atten = (LIGHT_ATTENUATION(i)-.5)*2.;
				fixed camRim = dot( i.worldViewDir, i.worldNorm );
				fixed lightRim = saturate(2.0-camRim*6.)*worldRim;
				fixed sun = saturate( atten*worldRim );

				//point lights
				fixed3 ambience = AnimeShade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb,
					unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, i.worldPos, i.worldNorm
				);
				//composite
				fixed3 final = color.rgb;
				APPLY_GRADUAL_DIRT(final,_GlobalDirtTex,_Dirt,i.uv*0.25)

				final *= ambience+saturate( lerp( UNITY_LIGHTMODEL_AMBIENT, _LightColor0, sun )+_ToonProximityAmbience );
				final = ApplyColorFromLight( final, color, sun, camRim, worldRim );
				return fixed4( final, 1. );
			}
			ENDCG
		}
	}
}
