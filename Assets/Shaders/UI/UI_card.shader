Shader "UI/card"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_BorderData ("Border Data Texture",2D) = "white" {}
		_BorderColor ("Border Color",Color) = (0,0,0)
		_AdditiveColor ("Additive Color",Color) = (0,0,0)

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
	}

	SubShader
	{
		LOD 100

		Cull Off
		ZTest On
		
		Tags {
			"Queue"="Overlay"
			"RenderType"="Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha

		Stencil{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		// ColorMask [_ColorMask]

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
				float2 uv : TEXCOORD0;
				float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed2 uv : TEXCOORD0;
				float4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _BorderData;
			fixed3 _BorderColor;
			fixed3 _AdditiveColor;

			v2f vert (appdata v){
				v2f o;
				
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                fixed3 c = tex2D( _MainTex, i.uv )*i.color;
				fixed2 borderData = tex2D( _BorderData, i.uv ).rg;
				c.rgb = lerp( c, _BorderColor, borderData.g )+_AdditiveColor; 
				return fixed4( c, borderData.r );
			}
			ENDCG
		}
	}
}
