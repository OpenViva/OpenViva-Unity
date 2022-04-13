Shader "Anime/UnlitTex2SOntop"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
	}
	SubShader
	{
		LOD 100
		Cull off
		ZTest off

		Tags {
			"LightMode" = "ForwardBase"
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}
		
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
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			uniform fixed3 _ToonProximityAmbience;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 col = tex2D(_MainTex, i.uv).rgb;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return fixed4(col*_ToonProximityAmbience,1.);
			}
			ENDCG
		}
	}
}
