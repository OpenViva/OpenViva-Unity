Shader "SP2/playerSplashed"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_SplashTex ("Water Splash Texture", 2D) = "white" {}
		_Alpha ("Hurt Alpha",Range(0.,1.0)) = 1.0
        
	}
	SubShader
	{
		LOD 100

		Pass
		{
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
			}
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			uniform sampler2D _MainTex;
			uniform sampler2D _SplashTex;
			uniform float _Alpha;

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TransformStereoScreenSpaceTex( v.uv, o.vertex.w );
				return o;
			}
			
			fixed3 applyClouds( fixed4 color, fixed4 clouds ){
				fixed cloudAlpha = step(color.a,0.02)*clouds.a;
				return color.rgb*(1.-cloudAlpha)+clouds.rgb*cloudAlpha;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed aspect = _ScreenParams.y/_ScreenParams.x;
				fixed2 diff = i.uv-fixed2(.5,.5);
				fixed sqDist = (diff.x*diff.x+diff.y*diff.y);
				sqDist = saturate(0.1/sqDist);
				fixed2 normUV = i.uv;
				normUV.y += sqDist*(1.-_Alpha)*0.6;

				fixed3 normal = UnpackNormal(tex2D(_SplashTex, normUV*fixed2( 1., aspect)));
                fixed4 col = tex2D( _MainTex, i.uv-normal*0.1*_Alpha );

				return fixed4( col.rgb, 1. );
			}
			ENDCG
		}
    }
}
