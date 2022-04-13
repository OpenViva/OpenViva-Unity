Shader "Surface/Surface_TexReceiveCutout" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Smoothness ("Smoothness", Range(0,1)) = 1.0
         _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
         _Metallic ("Metallic", 2D ) = "black" {}
         _Normal ("Normal", 2D ) = "bump" {}
         _Color ("Color", Color) = (1,1,1)
     }

     SubShader {
        
        Tags {
             "RenderType"="Transparent"
             "Queue"="AlphaTest"
             "PhotoData"="Opaque"
        }

        LOD 200
        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow

        sampler2D _MainTex;
        sampler2D _Metallic;
        sampler2D _Normal;
        fixed _Smoothness;
        fixed3 _Color;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            fixed2 data = tex2D(_Metallic, IN.uv_MainTex).ra;
            o.Albedo = c.rgb*_Color;
            o.Alpha = c.a;
            o.Metallic = data.r;
            o.Smoothness = data.g*_Smoothness;

            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex));
        }

        ENDCG
     }

     FallBack "Transparent/Cutout/Diffuse"

 }