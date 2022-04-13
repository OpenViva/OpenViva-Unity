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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
            fixed _xStretch;
            fixed _InnerBias;
            fixed _OuterBias;
			 
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.x *= _xStretch;
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed outerRing = step( 1.-i.color.a, tex2D( _MainTex, i.uv ).r-_InnerBias );
				fixed innerRing = step( 1.-i.color.a, tex2D( _MainTex, i.uv ).r-_OuterBias );
				return outerRing*i.color+innerRing;
			}
			ENDCG
		}
	}
}
