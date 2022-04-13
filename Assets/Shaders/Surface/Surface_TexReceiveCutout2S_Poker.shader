Shader "Surface/TexReceiveCutout2S_Poker_front" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Smoothness ("Smoothness", Range(0,1)) = 1.0
         _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
         _Red ("Red", Range(0,1) ) = 0.0
         _Highlighted ("Highlighted", Range(0,1) ) = 0.0
     }

     SubShader {
		Tags {
			"RenderType"="Opaque"
			"PhotoData"="Opaque"
		}
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard alphatest:_Cutoff
		#pragma target 3.0
		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		fixed _Smoothness;
        fixed _Red;
        fixed _Highlighted;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            
            fixed withinRed = 1.-step( 0.6338, IN.uv_MainTex.x )*step(  IN.uv_MainTex.y, 0.5 );
            o.Albedo = c.rgb*fixed3( min(1.,_Red+withinRed), withinRed, withinRed );
			o.Metallic = 0.0;
			o.Smoothness = _Smoothness;
			o.Alpha = c.a;
            
            fixed s = sin( _Time.w*2. )*0.25+0.25;
            o.Emission = fixed3(s,s,s)*_Highlighted; 
		}
		ENDCG
	}
	FallBack "Diffuse"
 }