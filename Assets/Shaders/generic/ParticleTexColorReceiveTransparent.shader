Shader "SP2/ParticleTexColorReceiveTransparent"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"
	ENDCG

	SubShader
	{
		LOD 100
		Cull Off
		ZWrite Off

		Blend SrcAlpha OneMinusSrcAlpha
		Tags {"Queue"="Transparent" }


		Pass
		{
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal: NORMAL;
				float4 color: COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
				fixed alpha: TEXCOORD1;
				fixed3 env: TEXCOORD3;

                UNITY_VERTEX_OUTPUT_STEREO
			};	

			sampler2D _MainTex;

			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				o.alpha = v.color.a;
				o.uv = v.uv;
				o.env = saturate( UNITY_LIGHTMODEL_AMBIENT+_LightColor0 );
				fixed avg = (o.env.r+o.env.g+o.env.b)/3.0;
				o.env = lerp( o.env, avg, 0.5 );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                //return fixed4( atten, 0., 0., 1. );
				fixed4 color = tex2D( _MainTex, i.uv );
				color.rgb *= i.env;
				color.a *= i.alpha;

				return color;
			}
			ENDCG
		}

		Pass    //fix cloud render transparency
        {
            Cull off
            Blend One One
            Zwrite off

            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

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

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed alpha = tex2D( _MainTex, i.uv ).a;
				return fixed4(0.,0.,0.,alpha);
			}
			ENDCG
        }
	}

}
