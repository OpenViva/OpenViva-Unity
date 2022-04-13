Shader "SP2/GestureDisplay"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Alpha ("Alpha", float) = 1.0
		_ClockProgress ("Clock Progress", Range(0,1)) = 1
	}

	SubShader
	{
		LOD 100

		Cull Off
		Zwrite Off
		ZTest Off
		
		Tags {
			"Queue"="Transparent-10"
			"RenderType"="Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{

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
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float _Alpha;
			uniform float _ClockProgress;

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D( _MainTex, i.uv );
				col.a *= _Alpha;
				fixed radian = atan2( ( i.uv.y-.5 ), (i.uv.x-.5) )/6.283185316+.5;
				col.a *= step( radian, _ClockProgress );
				return col;
			}
			ENDCG
		}
	}
}
