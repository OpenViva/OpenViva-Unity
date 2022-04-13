Shader "Unlit/PickupIcon"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Flip ("Flip", Range(-1,1)) = 1
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		Ztest off

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
				float3 uvGrow : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			uniform fixed _Flip;
			
			v2f vert (appdata v)
			{
				v2f o;
				// billboard mesh towards camera

				float3 baseWorldPos = unity_ObjectToWorld._m30_m31_m32;
				float grow = 1.2+abs(_SinTime.w)*.35;
				float3 vpos = baseWorldPos+v.vertex.xyz*grow;
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				
				o.vertex = mul(UNITY_MATRIX_P, viewPos);
				o.uvGrow.x = (v.uv.x-0.5)*_Flip+0.5;
				o.uvGrow.y = v.uv.y;
				o.uvGrow.z = grow;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uvGrow.xy);
				col.rgb *= i.uvGrow.z;
				return col;
			}
			ENDCG
		}
	}
}
