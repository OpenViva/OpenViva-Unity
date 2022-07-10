// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Surface/TexReceiveCutout2S_leaves1" {
    Properties {
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.01,1)) = 0.5
        _WindSpeed("Wind Speed", Range(0.0,1)) = 1.0
        _WindStrength("Wind Strength", Range(0.0,0.3)) = 1.0
        _LocaleColorA("Locale Color A",Color) = (1.0,1.0,1.0,1.0)
        _LocaleColorB("Locale Color B",Color) = (1.0,1.0,1.0,1.0)
        _LocalSize("Locale Size",float) = 1.0
    }
    SubShader {
        LOD 200
        ZWrite On
        Cull Off
        
        Pass    //shadow pass
        {

            Tags {
                // "Queue"="AlphaTest"
                // "RenderType"="Opaque"
                "LightMode"="ShadowCaster"
			}
 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #include "Wind.cginc"
 
            struct v2f {
                fixed4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };
         
            sampler2D _MainTex;
            fixed _Cutoff;
            uniform fixed _WindSpeed;
            uniform fixed _WindStrength;
 
            v2f vert(appdata_base v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                fixed3 pos = ApplyWind( _Time.z*_WindSpeed, v.vertex, _WindStrength*saturate( v.texcoord.x+v.texcoord.y )  );
                v.vertex = float4(pos,v.vertex.w);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }
 
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }

        // Pass {
        //     Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" }
        //     LOD 200
        //     Cull Off Lighting Off

        //     CGPROGRAM
        //     #pragma surface surf Lambert alphatest:_Cutoff

        //     sampler2D _MainTex;

        //     struct Input {
        //         float2 uv_MainTex;
        //     };

        //     void surf (Input IN, inout SurfaceOutput o) {
        //         fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
        //         o.Albedo = c.rgb;
        //         o.Alpha = c.a;
        //     }
        //     ENDCG
        // }


        Pass    //color pass
		{

            Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
            #include "Wind.cginc"
 

			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed2 uv : TEXCOORD0;
				LIGHTING_COORDS(1,2)
				UNITY_FOG_COORDS(3)
                fixed3 viewDir : TEXCOORD4;
                fixed3 tint : TEXCOORD5;
                float3 lightAmbience : TEXCOORD6;
				float3 worldPos : TEXCOORD7;

                UNITY_VERTEX_OUTPUT_STEREO
			};

            sampler2D _MainTex;
            uniform fixed _Cutoff;
            uniform fixed _WindSpeed;
            uniform fixed _WindStrength;
            uniform fixed3 _LocaleColorA;
            uniform fixed3 _LocaleColorB;
            uniform fixed _LocalSize;
            
            // sampler2D unity_Lightmap;
            // float4 unity_LightmapST;
			
			v2f vert (appdata v)
			{
				v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                fixed3 pos = v.vertex;
                pos = ApplyWind( _Time.z*_WindSpeed, pos, _WindStrength*saturate( v.uv.x+v.uv.y )  );
				o.pos = UnityObjectToClipPos(pos);
                fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex );
                fixed3 worldNorm = UnityObjectToWorldNormal( v.normal );
                o.viewDir = normalize( worldPos-_WorldSpaceCameraPos.xyz );
                o.worldPos = worldPos;

                //calculate vface
                fixed vface = -dot( o.viewDir, worldNorm );
                worldNorm *= vface*2.-1.;
                o.tint = lerp( _LocaleColorA, _LocaleColorB, zigZagNoise( worldPos.xz*_LocalSize ) );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				UNITY_TRANSFER_FOG(o,o.pos);

				o.uv.xy = v.uv;

                o.lightAmbience = Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb,
					unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos, worldNorm
				);
				return o;
			}
			

			fixed4 frag (v2f i ) : SV_Target
			{
                fixed4 col = tex2D( _MainTex, i.uv.xy );
                clip(col.a-_Cutoff);

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                fixed sunRim = saturate( dot( i.viewDir, _WorldSpaceLightPos0.xyz ) )+atten;
                col.rgb *= lerp( UNITY_LIGHTMODEL_AMBIENT+i.lightAmbience, _LightColor0+i.lightAmbience, sunRim )*i.tint;
				UNITY_APPLY_FOG(i.fogCoord, col);

                // col.a = 1.;
				return col;
			}
			ENDCG
		}
    }
 }