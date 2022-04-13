Shader "HUD/photoshootCard"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Background ("Photoshoot Background", 2D) = "white" {}
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
			// make fog work
			#pragma multi_compile_fog
			
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
			sampler2D _Background;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 bg = tex2D(_Background, i.uv).rgb;
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb = lerp( bg, col.rgb, col.a );
				return col;
			}
			ENDCG
		}
	}
}
