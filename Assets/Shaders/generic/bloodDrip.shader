Shader "Surface/BloodDrip" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_RoughnessMetallic ("Roughness", 2D) = "white" {}
		_Normal ("Normal", 2D) = "bump" {}
        _BloodColor ("Blood Color", Color) = (1,0,0,1)
		_BloodNormal ("Blood Normal", 2D) = "bump" {}
        _BloodRoughness ("Blood Smoothness", Range(0,1)) = 0.0
        _Blood ("Blood Amount", Range(0,1)) = 0.0
		_SolveColor ("Solve Normal", Color) = (0,0.7,1,1)
        _Solve ("Solve Amount", Range(0,1)) = 0.0
	}
	SubShader {
		Tags {
			"RenderType"="Opaque"
			"PhotoData"="Opaque"
		}
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _RoughnessMetallic;
		sampler2D _Normal;
		sampler2D _BloodMask;
		sampler2D _BloodNormal;
		fixed3 _BloodColor;
		fixed _BloodRoughness;
		fixed _Blood;
		fixed3 _SolveColor;
		fixed _Solve;

		struct Input {
			float2 uv_MainTex;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);

			fixed random = cos( _SinTime.w+(IN.uv_MainTex.y+sin( IN.uv_MainTex.x*4. ))*4.+_Time.w );
			fixed blood = smoothstep( _Blood*(0.9+random*0.2), 0., c.a );
			c.rgb = lerp( c.rgb, _BloodColor, blood );
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			fixed4 regularNormal = tex2D(_Normal, IN.uv_MainTex );
			fixed4 bloodNormal = tex2D(_BloodNormal, IN.uv_MainTex );
            o.Normal = UnpackNormal( regularNormal*(1.-blood)+bloodNormal*blood*(0.95+random*0.1) );
            fixed2 data = tex2D(_RoughnessMetallic, IN.uv_MainTex).ar;
			o.Metallic = data.g;
			o.Smoothness = lerp( data.r, _BloodRoughness, blood );

			o.Emission = lerp( fixed3(0.,0.,0.), _SolveColor, _Solve*blood );
		}
		ENDCG
	}
	FallBack "Diffuse"
}
