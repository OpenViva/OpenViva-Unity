Shader "Effects/mirror"
{
    Properties 
    {
        _CubeTex ("Texture", CUBE) = "" {} 
		_Roughness ("Metallic Roughness Texture", 2D) = "white" {}
        _CubeCenter ("Cube center",Vector) = (0,0,0)
        _CubeMin ("Cube min",Vector) = (0,0,0)
        _CubeMax ("Cube max",Vector) = (0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Cull Back

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
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 eye: TEXCOORD1;
                float3 worldPos: TEXCOORD2;
                float3 worldNormal: TEXCOORD3;
            };

			sampler2D _Roughness;
            samplerCUBE _CubeTex;
            uniform float4 _CubeCenter;
            uniform float3 _CubeMin;
            uniform float3 _CubeMax;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
                o.worldNormal = UnityObjectToWorldNormal( v.normal );
                o.eye = o.worldPos-_WorldSpaceCameraPos.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float Roughness = 1.-tex2D(_Roughness, i.uv).a;
                
                fixed3 r = reflect( i.eye, i.worldNormal );
                fixed3 closest;
                fixed3 rMax = (_CubeMax-i.worldPos)/r;
                fixed3 rMin = (_CubeMin-i.worldPos)/r;
                fixed3 far = max( rMax, rMin );
                fixed dist = min( min( far.x, far.y ), far.z );
                fixed3 intersect = i.worldPos+r*dist;
                r = intersect-_CubeCenter;

                fixed3 color = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, r );
                color += fixed3(.2,.2,.2)*Roughness;
                return fixed4( color, 1. );
            }
            ENDCG
        }
    }
}