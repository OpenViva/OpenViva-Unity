Shader "Surface/Surface_HouseWallWear" {

     Properties {
         _MainTex ("Base", 2D) = "white" {}
         _Normal ("Normal", 2D ) = "bump" {}
         _MainTex2 ("Wear Base", 2D) = "white" {}
         _RoughnessAndMask ("Roughness and Mask (RG)", 2D ) = "black" {}
         _Normal2 ("Normal", 2D ) = "bump" {}
         _MaskSize("Mask Size", Range(0.2,4.0)) = 2
         _ShadowWidth("Shadow width", Range(0.0,0.004)) = 0.001
         _ShadowStrength("Shadow Strength", Range(0.0,1)) = 0.5
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
        sampler2D _Normal2;
        sampler2D _MainTex2;
        uniform fixed3 _Color;
        uniform fixed _MaskSize;
        uniform fixed _ShadowWidth;
        uniform fixed _ShadowStrength;

        struct Input {
            float2 uv_MainTex;
            float vertColor : COLOR;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {       
            
            fixed data1 = tex2D(_RoughnessAndMask, IN.uv_MainTex*_MaskSize).g;
            fixed data2 = tex2D(_RoughnessAndMask, IN.uv_MainTex*_MaskSize+_ShadowWidth).g;
            fixed mask = smoothstep( 0., IN.vertColor, data1 );
            fixed mask2 = smoothstep( 0., IN.vertColor, data2 );

            fixed maskShadow = saturate( mask2-mask );

            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*_Color*mask+tex2D(_MainTex2, IN.uv_MainTex).rgb*(1.-mask)-maskShadow*_ShadowStrength;
            o.Smoothness = tex2D(_RoughnessAndMask, IN.uv_MainTex).r*mask;
            o.Normal = UnpackNormal( tex2D(_Normal, IN.uv_MainTex)*mask+tex2D(_Normal2, IN.uv_MainTex)*(1.-mask) );
        }

        ENDCG
     }
 }