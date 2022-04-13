Shader "Effects/bubbles"
{
	Properties
	{
        _MainTex ("Main Texture", 2D) = "white" {}
        _BubbleSize ("Bubble Size", range(0.003, 0.04)) = 0.03
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent+1"
			"RenderType"="Transparent"
		}
		LOD 100

		Cull off
		ZWrite Off
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform fixed _BubbleSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.uv = v.uv;
				float3 local = v.vertex;
				local += UNITY_MATRIX_MV[0].xyz*(o.uv.x-.5)*_BubbleSize;
				local += UNITY_MATRIX_MV[1].xyz*(o.uv.y-.5)*_BubbleSize;

				//flip UV based on color
				o.uv = (o.uv-.5)*(v.color-.5)*2.+.5;

				o.vertex = UnityObjectToClipPos( local );
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D( _MainTex, i.uv );
				// col.a = pow(col.a,8.0)*.8; 
				return col;
			}
			ENDCG
		}
	}
}
