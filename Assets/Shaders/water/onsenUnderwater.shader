Shader "Surface/OnsenUnderwater" {

     Properties {
        _MainTex ("Main Texture", 2D ) = "white" {}
        _NoiseMap ("Noise Map", 2D ) = "white" {}
        _PrimaryWaterColor ("Primary Water Color", Color) = (0.,0.,1.,1.)
        _Smoothness ("Smoothness", Range(0.,1.0)) = 0.25
        _NormalStrength ("Normal Strength Mult", Range(0.,1.0)) = 0.5
        _NoiseSpeed ("Noise Speed", Range(0.,2.)) = 2.
        _FlowSpeed ("Flow Speed", Range(0.,4.)) = 2.
     }

     SubShader {
        
        Tags {
            "RenderType"="Transparent"
            "Queue"="AlphaTest"
        }

        Cull off
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 200

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows alphatest:fade

        sampler2D _MainTex;
        sampler2D _NoiseMap;
        uniform fixed _NoiseSpeed;
        uniform fixed4 _PrimaryWaterColor;
        uniform fixed _Smoothness;
        uniform fixed _FlowSpeed;
        uniform fixed _NormalStrength;

        struct Input {
            float2 uv_MainTex;
            float4 screenPos;
        };

        fixed noiseBlend( fixed f ){
            fixed p = step( 0.5, f );
            fixed r = f*2.*(1.-p)+(1.-(f-.5)*2.)*p;
            return smoothstep( 0., 1., r );
        }

        fixed2 pseudoRandomSample( sampler2D tex, fixed2 uv, fixed time ){
            fixed n1 = noiseBlend( frac( time ) );
            fixed n2 = noiseBlend( frac( time+0.3333 ) );
            fixed n3 = noiseBlend( frac( time+0.6666 ) );
            fixed flowSpeed = _Time.x*_FlowSpeed;
            return (tex2D( tex, uv+fixed2(flowSpeed,0.0)+floor( time )*0.541 ).rg*n1+
                    tex2D( tex, uv+fixed2(-0.5*flowSpeed,0.87*flowSpeed)-floor( time+0.3333 )*0.781 ).gr*n2+
                    tex2D( tex, uv+fixed2(-0.5*flowSpeed,-0.87*flowSpeed)+floor( time+0.6666 )*0.367 ).rg*n3 )*0.6666-.333;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) {       

            o.Smoothness = _Smoothness;
            o.Metallic = 0.0;
            fixed2 noise = pseudoRandomSample( _NoiseMap, IN.uv_MainTex, _Time.y*_NoiseSpeed )*_NormalStrength;
            o.Normal = fixed3( noise, 1.0 );

            fixed2 screenCoords = IN.screenPos.xy / IN.screenPos.w;
            // screenCoords.y = 1.-screenCoords.y;
            fixed3 camera = tex2D( _MainTex, screenCoords+noise ).rgb;

            o.Albedo = _PrimaryWaterColor.rgb*camera;
            o.Alpha = _PrimaryWaterColor.a;
        }

        ENDCG
     }
 }