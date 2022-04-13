Shader "Effects/weatherSimGenerate"
{
	Properties
	{
		_MainTex ("Noise Texture", 2D) = "white" {}
        _Multiplier ("Coverage Mult", range(0,4)) = 1.
        _Additive ("Coverage Additive", range(0,1)) = 1.
	}
	SubShader
	{
		LOD 100
		Cull Off
		ZTest Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 uv12 : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform fixed _Multiplier;
			uniform fixed _Additive;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = fixed4( v.vertex.xy-.5, 0., .5 );
				fixed time = _Time.x*0.05;
				o.uv12.xy = v.uv+time*.6;
				o.uv12.zw = v.uv+fixed2( time, -time);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed perlin = tex2D( _MainTex,  i.uv12.xy ).r*.5;
				perlin += tex2D( _MainTex, i.uv12.zw ).g*.5*_Multiplier+_Additive;
				return perlin.rrrr;
			}
			ENDCG
		}
	}
}
