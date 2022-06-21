Shader "Surface/TexReceiveCutout2S" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Smoothness ("Smoothness", Range(0,2)) = 1.0
         _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
         _Metallic ("Metallic", 2D ) = "black" {}
         _Normal ("Normal", 2D ) = "bump" {}
     }

     SubShader {
        
        Tags {
             "RenderType"="Transparent"
             "Queue"="Transparent"
             "PhotoData"="Opaque"
        }

        LOD 200
        Cull Off

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Metallic;
        sampler2D _Normal;
        fixed _Smoothness;

        struct Input {
            float2 uv_MainTex;
            fixed facing : VFACE;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            fixed2 data = tex2D(_Metallic, IN.uv_MainTex).ra;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = data.r;
            o.Smoothness = data.g*_Smoothness;

            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex))*(IN.facing*2-1);
        }

        ENDCG
 
        Cull off
        Blend One One
        Zwrite off

        Pass {  //fix transparency alpha
            
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                fixed alpha = tex2D( _MainTex, i.uv ).a;
				return fixed4(0.,0.,0.,alpha);
			}
			ENDCG
        }

     }

     FallBack "Transparent/Cutout/Diffuse"

 }