
Shader "Effects/DeckActionSymbol"
{
	Properties
	{
		_MainTex ("Poker Symbol Texture", 2D) = "white" {}
	}
	SubShader
	{
		
		Tags {
			"Queue" = "Transparent"
			"RenderType"="Transparent"
		}
		LOD 100

		Cull Back
		ZWrite Off
		ZTest Less

		Blend SrcAlpha OneMinusSrcAlpha


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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				float scale = 1.+abs(sin(_Time.w*1.2)*0.15);
                o.vertex = UnityObjectToClipPos(v.vertex*fixed3(1.,scale,1.));
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D( _MainTex, i.uv );
			}
			ENDCG
		}
	}
}
