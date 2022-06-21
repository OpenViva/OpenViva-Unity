Shader "Unlit/InGameIndicator"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Range(0,2)) = 1.0
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

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float3 uvGrow : TEXCOORD0;
				float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			fixed _Scale;

			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				// billboard mesh towards camera

				float3 baseWorldPos = unity_ObjectToWorld._m30_m31_m32;
				float grow = 1.2+abs(_SinTime.w)*.35;
				float3 vpos = baseWorldPos+v.vertex.xyz*grow*_Scale;
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				
				
				o.vertex = mul(UNITY_MATRIX_P, viewPos);
				o.uvGrow.xy = v.uv;
				o.uvGrow.z = grow;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed4 col = tex2D(_MainTex, i.uvGrow.xy);
				col.rgb *= i.uvGrow.z;
				return col;
			}
			ENDCG
		}
	}
}
