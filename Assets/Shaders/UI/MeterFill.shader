Shader "Unlit/MeterFill"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
    	_FillColor ("Fill Color", Color) = (0.5,0.0,1.0,1.0)
    	_Fill ("Fill", Range(0,1)) = 1
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100
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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform fixed _Fill;
			uniform fixed4 _FillColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				// billboard mesh towards camera

				float3 baseWorldPos = unity_ObjectToWorld._m30_m31_m32;
				float3 vpos = baseWorldPos+v.vertex.xyz;
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0.1); 
				
				o.vertex = mul(UNITY_MATRIX_P, viewPos);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 data = tex2D(_MainTex, i.uv);
				fixed4 col = fixed4( 1., 1., 1., data.r );

				col = lerp( col, _FillColor, step( ( i.uv.y-0.1)*1.175 ,_Fill)*data.b );

				return col;
			}
			ENDCG
		}
	}
}
