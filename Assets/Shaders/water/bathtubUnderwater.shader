Shader "water/bathtubUnderwater"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_SurfaceTex ("Surface Texture", 2D) = "white" {}
        
	}
	SubShader
	{
		LOD 100
		Tags {
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}
		Cull front
		Blend One OneMinusSrcAlpha

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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 screenPos : TEXCOORD0;
				fixed2 uv : TEXCOORD1;
			};

			uniform sampler2D _MainTex;
			uniform sampler2D _SurfaceTex;

			v2f vert (appdata v)
			{
				v2f o;
				fixed4 clipPos = UnityObjectToClipPos(v.vertex);
				o.vertex = clipPos;
				o.screenPos = ComputeScreenPos( clipPos ).xyw;
				o.uv = v.uv;
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
				fixed2 screenscreenPos = i.screenPos.xy/i.screenPos.z;

				fixed warpScale = 128.;
				fixed2 offset;
				offset.x = sin( i.uv.x*warpScale+_SinTime.w*7. );
				offset.y = cos( i.uv.y*warpScale+_CosTime.w*6.1+offset.x );
				offset *= 0.01; 

                fixed3 col = tex2D( _MainTex, screenscreenPos+offset ).rgb;
                fixed surf = tex2D( _SurfaceTex, i.uv+offset*0.1 ).r;
				col.rgb = lerp( col.rgb, surf.rrr, surf*0.5 );
				return fixed4( col, 1.0 );
			}
			ENDCG
		}
    }
}
