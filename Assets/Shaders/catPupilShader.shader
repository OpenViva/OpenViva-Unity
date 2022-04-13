Shader "Surface/CatPupilShader" {

     Properties {
		_MainTex ("Base", 2D) = "white" {}
		_PupilShrink ("Pupil Shrink",Range(1.,4.)) = 1.0
		_PupilRight ("Pupil Right",Range(-1.,1.)) = 0.0
		_PupilUp ("Pupil Up",Range(-1.,1.)) = 0.0
		_Emission ("Pupil Up",2D) = "black" {}
     }

     SubShader {
        
        Tags {
             "RenderType"="Opaque"
             "PhotoData"="Opaque"
        }

        LOD 200

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _Emission;
        uniform fixed _PupilShrink;
        uniform fixed _PupilRight;
        uniform fixed _PupilUp;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {       
            
            fixed2 uv = IN.uv_MainTex;
            uv.x += _PupilRight;
            uv.x = (uv.x-.5)*_PupilShrink+0.5;
            uv.y += _PupilUp;
            o.Albedo = tex2D(_MainTex, uv ).rgb;
            o.Smoothness = 1.0;
            o.Emission = tex2D(_Emission, uv ).rgb;
        }

        ENDCG
     }
 }