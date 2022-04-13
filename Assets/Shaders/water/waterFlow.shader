Shader "Effects/waterFlow"
{
	Properties
	{
        _MainTex ("Main Texture", 2D) = "white" {}
        _FlowSpeed ("Flow Speed", range(1,10)) = 1.0
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent" 
			"RenderType"="Transparent"
		}
		LOD 100

		Cull off
		// Blend One OneMinusSrcAlpha

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
			uniform fixed _FlowSpeed;
			
			v2f vert (appdata v)
			{
				v2f o;
				float flowSpeed = _Time.w*_FlowSpeed;
				float3 local = v.vertex;
				const float distortion = 0.02;
				local.x += sin((local.z+v.uv.x)*787.0+flowSpeed)*local.z*distortion;
				local.y += sin((local.z+v.uv.y)*1976.0+flowSpeed*0.723)*local.z*distortion;
				o.vertex = UnityObjectToClipPos( local );
				o.uv = v.uv;
				o.uv.y += _Time.w*_FlowSpeed;
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
