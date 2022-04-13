Shader "Surface/Surface_LOD_Card"
{ 
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Scale ("Scale", Range(10,200)) = 10
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader
	{
		Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "PhotoData"="Opaque"
        }
        LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
        Zwrite off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#pragma multi_compile_fog

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            uniform float _Cutoff;
            uniform float _Scale;

            v2f vert (appdata v){
                v2f o;
				float3 baseWorldPos = unity_ObjectToWorld._m30_m31_m32;
				float3 vpos = baseWorldPos+v.vertex.xyz*_Scale;
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				
				o.vertex = mul(UNITY_MATRIX_P, viewPos);
                o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D (_MainTex, i.uv);
                col.rgb *= _LightColor0;
				UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
	}
     FallBack "Transparent/Cutout/Diffuse"
    // Fallback "Diffuse"
}