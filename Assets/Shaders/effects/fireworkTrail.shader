Shader "Effects/Firework_trail"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _xStretch ("x Stretch", Range(0.0,4.0)) = 4.0
        _InnerBias ("Inner Bias", Range(0.0,1.0)) = 0.08
        _OuterBias ("Outer Bias", Range(0.0,1.0)) = 0.08
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100
		Blend One One
		Zwrite off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
            fixed _xStretch;
            fixed _InnerBias;
            fixed _OuterBias;
			 
			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.x *= _xStretch;
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed outerRing = step( 1.-i.color.a, tex2D( _MainTex, i.uv ).r-_InnerBias );
				fixed innerRing = step( 1.-i.color.a, tex2D( _MainTex, i.uv ).r-_OuterBias );
				return outerRing*i.color+innerRing;
			}
			ENDCG
		}
	}
}
