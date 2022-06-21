
Shader "Effects/SpeechBubble"
{
	Properties
	{
		_MainTex ("Background Bubble", 2D) = "white" {}
		_SymTex ("Symbol", 2D) = "white" {}
		_SymbolScale ("Symbol Scale",Range(0.5,2.) ) = 1.0
		_Alpha ("Alpha",Range(0.,1.) ) = 1.0
	}
	SubShader
	{
		
		Tags {
			"Queue" = "Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100

		Cull Off
		ZWrite Off
		ZTest Less

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
				float2 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float fade : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _SymTex;
			uniform fixed _SymbolScale;
			uniform fixed _Alpha;
			
			v2f vert (appdata v)
			{
				v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				fixed radian = v.color.r*3.14+_Time.x;
				fixed c = cos( radian );
				fixed s = sin( radian );
				
				o.uv.xy = ( v.uv-.5 )*_SymbolScale+.5;

				fixed2 centered = v.uv-.5;
				o.uv.z = centered.x*c-centered.y*s;
				o.uv.w = centered.x*s+centered.y*c;
				o.uv.zw += .5;

				o.fade = v.color.g;
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed4 bg = tex2D( _MainTex, i.uv.zw );
				fixed4 symbol = tex2D( _SymTex, i.uv.xy );
				fixed4 c = lerp( bg, symbol, symbol.a );
				c.a *= i.fade*_Alpha;
				return c;
			}
			ENDCG
		}
	}
}
