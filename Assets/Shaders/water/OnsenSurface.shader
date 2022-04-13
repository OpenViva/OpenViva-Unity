Shader "Surface/OnsenSurface" {

     Properties {
        _PrimaryWaterColor ("Primary Water Color", Color) = (0.,0.,1.,0.)
        _EdgeMap ("EdgeMap", 2D ) = "black" {}
        _EdgeAlphaFalloff ("Edge Alpha Falloff", Range(0.,1.0)) = 0.25
        _EdgeColorFalloff ("Edge Color Falloff", Range(0.,1.0)) = 0.25
        _MainTex ("Normal", 2D ) = "bump" {}
        _NoiseScale ("Noise Scale", Range(0.,8.0)) = 1.0
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

        #pragma surface surf Standard fullforwardshadows alphatest:fade addshadow

        sampler2D _MainTex;
        sampler2D _EdgeMap;
        uniform fixed _NoiseScale;
        uniform fixed _EdgeAlphaFalloff;
        uniform fixed _EdgeColorFalloff;
        uniform fixed _NoiseSpeed;
        uniform fixed4 _PrimaryWaterColor;
        uniform fixed _Smoothness;
        uniform fixed _FlowSpeed;
        uniform fixed _NormalStrength;

        struct Input {
            float2 uv_MainTex;
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
                    tex2D( tex, uv+fixed2(-0.5*flowSpeed,-0.87*flowSpeed)+floor( time+0.6666 )*0.367 ).rg*n3 )*0.6666;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) {       
            fixed edge = tex2D( _EdgeMap, IN.uv_MainTex).r;
            o.Alpha = _PrimaryWaterColor.a-edge*_EdgeAlphaFalloff;
            o.Albedo = _PrimaryWaterColor-edge*_EdgeColorFalloff;
            o.Smoothness = _Smoothness;
            o.Metallic = 0.0;
            fixed2 noise = pseudoRandomSample( _MainTex, IN.uv_MainTex*_NoiseScale, _Time.y*_NoiseSpeed )*_NormalStrength;
            o.Normal = normalize( fixed3( noise.rg, 1.0 ) );
        }

        ENDCG
     }
 }