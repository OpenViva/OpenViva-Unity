Shader "Surface/WallStencil" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Smoothness ("Smoothness", Range(0,1)) = 1.0
         _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
         _AlphaMult ("Alpha Multiply", Range(0,1)) = 1.0
     }

     SubShader {
        
        Tags {
             "RenderType"="Transparent"
             "Queue"="AlphaTest"
             "PhotoData"="Opaque"
        }

        LOD 200
		Blend One OneMinusDstAlpha 

        CGPROGRAM

        #pragma surface surf Standard alpha

        sampler2D _MainTex;
        fixed _Smoothness;
		fixed _Cutoff;
		fixed _AlphaMult;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			if( c.a < _Cutoff ){
				discard;
			}
            o.Albedo = c.rgb;
			o.Alpha = c.a*_AlphaMult;
            o.Smoothness = _Smoothness;
        }

        ENDCG
     }
    //  FallBack "Transparent/Cutout/Diffuse"

 }