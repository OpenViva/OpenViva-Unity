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

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			uniform fixed _Alpha;

			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                return fixed4(0.,0.,0.,_Alpha);
			}
			ENDCG
		}
	}
}
