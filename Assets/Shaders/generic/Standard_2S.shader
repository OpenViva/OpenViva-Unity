Shader "Surface/TexReceiveCast2S_Opaque" {

     Properties {
        _FrontTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _BackTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Smoothness ("Smooth", Range(0,1)) = 0.0
        _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
     }

     SubShader {

        Tags {
             "RenderType"="Opaque"
             "PhotoData"="Opaque"
        }

        LOD 200
        Cull Off

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _FrontTex;
        sampler2D _BackTex;
        sampler2D _Normal;
        uniform fixed _Metallic;
        uniform fixed _Smoothness;

        struct Input {
            float2 uv_FrontTex;
            fixed facing : VFACE;
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {
			// o.Metallic = _Metallic;
            // o.Smoothness = _Smoothness;
            o.Normal = UnpackNormal( tex2D(_Normal, IN.uv_FrontTex) )*( IN.facing*2-1 );
            fixed side = step( 1., IN.facing );
            o.Albedo = tex2D( _FrontTex, IN.uv_FrontTex ).rgb*side+tex2D( _BackTex, IN.uv_FrontTex )*(1.-side);
        }

        ENDCG

     }
 }