Shader "SP2/playerHurt"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Overlay ("Overlay Texture", 2D) = "white" {}
		_Alpha ("Hurt Alpha",Range(0.,1.0)) = 1.0
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

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			uniform sampler2D _Overlay;
			uniform sampler2D _CloudsRT;
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
			

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
    			fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
				col.rgb += tex2D( _CloudsRT, i.uv ).rgb*saturate(1.-col.a);
				fixed overlay = tex2D( _Overlay, i.uv ).r*_Alpha;
                col.bg -= overlay;
				col.r *= 1.-overlay;
				return fixed4( col.rgb, 1. );
			}
			ENDCG
		}
    }
}
