Shader "Surface/TexReceiveTransparentInterior" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Smoothness ("Smoothness", Range(0,2)) = 1.0
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
         _Metallic ("Metallic", 2D ) = "black" {}
     }

     SubShader {
        
        Tags {
             "Queue"="Transparent"
             "RenderType"="Transparent"
             "PhotoData"="Opaque"
        }

        Zwrite Off
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows alphatest:fade addshadow

        sampler2D _MainTex;
        sampler2D _Metallic;
        fixed _Smoothness;

        struct Input {
            float2 uv_MainTex;
        };
        
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)


        void surf (Input IN, inout SurfaceOutputStandard o) {            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            fixed2 data = tex2D(_Metallic, IN.uv_MainTex).ra;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = data.r;
            o.Smoothness = data.g*_Smoothness;
        }

        ENDCG
 
        Cull off
        Blend One One
        Zwrite off

        Pass {
            
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			uniform sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
                fixed alpha = tex2D( _MainTex, i.uv ).a;
				return fixed4(0.,0.,0.,alpha);
			}
			ENDCG
        }

     }

     FallBack "Transparent/Cutout/Diffuse"

 }