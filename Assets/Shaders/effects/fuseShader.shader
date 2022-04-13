Shader "Effects/Fuse" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Fuse ("Fuse", Range(0,1)) = 1.0
         _FuseGlowLength ("Fuse Glow Length", Range(1,16)) = 1.0
         _FuseGlow ("FuseGlow", Range(0,1)) = 1.0
     }

     SubShader {
        
        Tags {
             "RenderType"="Transparent"
             "Queue"="AlphaTest"
             "PhotoData"="Opaque"
        }

        LOD 200
        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows addshadow

        sampler2D _MainTex;
        fixed _Fuse;
        fixed _FuseGlowLength;
        fixed _FuseGlow;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {            
            o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
            o.Metallic = 0.;
            o.Smoothness = 0.;
			o.Emission = saturate( 1.-( _Fuse-IN.uv_MainTex.x )*_FuseGlowLength )*2.*_FuseGlow*fixed3( 1., 0.6, 0.3 );
			clip( _Fuse-IN.uv_MainTex.x );
        } 

        ENDCG
     }

     FallBack "Transparent/Cutout/Diffuse"

 }