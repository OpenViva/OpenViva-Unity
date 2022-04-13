Shader "UI/BLACK"
{
	Properties {
    	_Alpha ("Alpha", Range(0,1)) = 1
	}
	SubShader
	{
		LOD 100

		Cull Back
		ZTest Off
		ZWrite on
		
		Tags {
			"Queue"="Transparent"
			"RenderType"="Transparent"
		}

		Blend One OneMinusSrcAlpha
		Cull front

		Pass
		{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
			};

			sampler2D _MainTex;
			uniform fixed _Alpha;

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0.,0.,0.,_Alpha);
			}
			ENDCG
		}
	}
}
