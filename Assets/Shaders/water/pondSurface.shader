Shader "SP2/pondSurface"{
	Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Cube ("Cubemap", CUBE) = "" {}
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
       
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0
 
        sampler2D _MainTex;
		samplerCUBE _Cube;
 
        struct Input {
            float2 uv_MainTex;
			float3 worldRefl;
          	float3 worldPos;
        };

 
        void surf (Input IN, inout SurfaceOutputStandard o) {

			//wave
			fixed2 offset;
			offset.x = sin( IN.uv_MainTex.x*16.+_SinTime.w*7. );
			offset.y = cos( IN.uv_MainTex.y*16.+_CosTime.w*6.1+offset.x );
			offset *= 0.001;

			fixed4 tex = tex2D (_MainTex, IN.uv_MainTex+offset);
			o.Albedo = tex.rgb;

			fixed2 diff = _WorldSpaceCameraPos.xz-IN.worldPos.xz;
            
            fixed distVal = 1.-saturate( (diff.x*diff.x+diff.y*diff.y)*.02 )*.5;
            fixed alpha = .1+(1.-tex.a*distVal)*.9;
            o.Alpha = alpha;
			o.Emission = texCUBE (_Cube, IN.worldRefl+fixed3( offset.x, 0., offset.y ) ).rgb*tex.a*.5;
        }
        ENDCG
    }
    FallBack "Diffuse"
}