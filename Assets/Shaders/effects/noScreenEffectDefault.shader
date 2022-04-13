Shader "Effects/NoScreenEffectDefault"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_CloudsRT ("Cloud Render Texture", 2D) = "white" {}
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
			uniform sampler2D _CloudsRT;

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
				fixed cloudAlpha = step(color.a,0.0)*clouds.a*0.9;
				return color.rgb*(1.-cloudAlpha)+clouds.rgb*cloudAlpha;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 col = tex2D( _MainTex, i.uv );
				col.rgb = applyClouds( col, tex2D( _CloudsRT, i.uv ) );
				return col;
			}
			ENDCG
		}
    }
}
