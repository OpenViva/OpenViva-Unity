Shader "Effects/ModelPreviewMirror"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_BGA ("Color A",Color) = ( 0., 0., 0., 1.)
		_BGB ("Color B",Color) = ( 0., 0., 0., 1.)
	}

	SubShader
	{
		LOD 100

		Cull Off
		
		Tags {
			"RenderType"="Opaque"
		}

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
			uniform fixed3 _BGA;
			uniform fixed3 _BGB;

			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 gradient = lerp( _BGA, _BGB, i.uv.y );
				fixed4 c = tex2D( _MainTex, i.uv );
				return fixed4( lerp( gradient, c.rgb, c.a ), 1.0 );
			}
			ENDCG
		}
	}
}
