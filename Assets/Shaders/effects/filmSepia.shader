Shader "SP2/filmSepia"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_FilmDirt ("Film Dirt", 2D) = "white" {}
		_FilmColor ("Skin base color",Color) = (1.,0.86,1.0)
        
	}
	SubShader
	{
		LOD 100

		Offset -1, -1

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
 
			uniform sampler2D _MainTex;
			uniform sampler2D _FilmDirt;
			uniform fixed3 _FilmColor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed screen( fixed a, fixed b ){
				return 1.-(1.-a)*(1.-b);
			}

			fixed3 screen( fixed3 a, fixed3 b ){
				return fixed3( screen(a.r,b.r), screen(a.g,b.g), screen(a.b,b.b) );
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 col = tex2D( _MainTex, i.uv ).rgb;
				col = screen( col, _FilmColor );
				col *= tex2D( _FilmDirt, i.uv ).rgb;
				return fixed4( col, 1. );
			}
			ENDCG
		}
    }
}
