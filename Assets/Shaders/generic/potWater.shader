Shader "Effects/potWater"
{
	Properties
	{
        _NoiseMap ("Noise Map", 2D) = "white" {}
        _WaterColor ("Water Color", Color) = (0.,0.,1.,1.)
        _Boil ("Boil", Range(0.,1.)) = 0.
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent-1" 
			"RenderType"="Transparent"
		}
		LOD 100

		Cull off
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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 eye : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _NoiseMap;
			uniform fixed4 _WaterColor;
			uniform fixed _Boil;
			
			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos( v.vertex );
				o.uv = v.uv;
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				o.eye = normalize( UnityWorldSpaceViewDir(worldPos) );
				o.eye.xz *= -1.0;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed2 noise;
				fixed speed = _Time.x*(1.0+_Boil*3.0);
				noise.x = tex2D( _NoiseMap, i.uv+speed ).r;
				noise.y = tex2D( _NoiseMap, i.uv.yx-speed ).g;

				fixed bubbles = saturate( tex2D( _NoiseMap, i.uv+noise*0.15 ).b-(1.-_Boil));

				fixed3 refl = i.eye;
				refl.xz += noise*0.5;

				fixed3 env = UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, refl ).rgb;
				fixed4 color = _WaterColor;
				color.rgb *= env;
				color += bubbles;
				return color;
			}
			ENDCG
		}
	}
}
