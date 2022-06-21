Shader "Effects/bathtubWater"
{
	Properties
	{
        _HeightMap ("Bubble Heightmap", 2D) = "white" {}
        _NoiseMap ("Noise Map", 2D) = "white" {}
		_FillChoke("Fill Choke", Range(-2.,1.)) = 0.5
        _WaterColor ("Water Color", Color) = (0.,0.,1.,1.)
        _BubbleColor ("Bubble Color", Color) = (0.,0.,1.,1.)
        _Clarity ("Clarity", Range(0.,0.9)) = 0.9
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
				float choke : TEXCOORD1;
				float3 eye : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _HeightMap;
			sampler2D _NoiseMap;
			uniform fixed4 _WaterColor;
			uniform fixed4 _BubbleColor;
			uniform fixed _FillChoke;
			uniform fixed _Clarity;
			
			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos( v.vertex );
				o.uv = v.uv;
				o.choke = saturate(v.vertex.y*90.0-_FillChoke);
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				o.eye = normalize( UnityWorldSpaceViewDir(worldPos) );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed2 noise;
				noise.x = tex2D( _NoiseMap, i.uv+_Time.x*2.0f ).r;
				noise.y = tex2D( _NoiseMap, i.uv.yx+_Time.x*2.0f ).r;
				fixed height = saturate( tex2D( _HeightMap, i.uv+noise*0.05 ).r-_Clarity )/(1.-_Clarity);

				fixed3 refl = i.eye;
				refl.xy += noise*0.25;
				fixed3 env = UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, refl ).rgb;
				fixed4 color = lerp( _WaterColor, _BubbleColor, height );
				color.rgb += env*saturate(1.-height*2.0);
				color.a *= i.choke;
				return color;
			}
			ENDCG
		}
	}
}
