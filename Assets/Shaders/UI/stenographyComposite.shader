Shader "Tools/StenographyComposite"
{
	Properties
	{
		_Embed ("Embedded Texture", 2D) = "white" {}
		_CardBorder ("Card Border", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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

			sampler2D _Embed;
			sampler2D _CardBorder;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target 
			{
				fixed3 c = tex2D(_Embed, i.uv).rgb;	//embedded texture
				fixed4 cardBorder = tex2D(_CardBorder, i.uv);
				c.rgb += cardBorder.rgb*cardBorder.a;
				return fixed4( c, 1. );
			}
			ENDCG
		}
	}
}
