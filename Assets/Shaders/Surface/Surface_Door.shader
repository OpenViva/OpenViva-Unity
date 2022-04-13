Shader "Surface/Surface_Door" {

     Properties {
         _MainTex ("Base", 2D) = "white" {}
         _Normal ("Normal", 2D ) = "bump" {}
         _RoughnessAndMask ("Roughness and Mask (RG)", 2D ) = "black" {}
         _Color ("Color", Color) = (0.6,0.5,1.0,1)
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
     }

     SubShader {
        
        Tags {
             "RenderType"="Opaque"
             "PhotoData"="Opaque"
        }

        LOD 200

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows addshadow

        sampler2D _MainTex;
        sampler2D _RoughnessAndMask;
        sampler2D _Normal;
        uniform fixed3 _Color;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {       
            
            fixed2 data = tex2D(_RoughnessAndMask, IN.uv_MainTex).rg;

            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*saturate( data.ggg+_Color );
            o.Smoothness = data.r; 
            o.Normal = UnpackNormal( tex2D(_Normal, IN.uv_MainTex) );
        }

        ENDCG
     }
 }