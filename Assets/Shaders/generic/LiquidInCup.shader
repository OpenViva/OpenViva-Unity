Shader "SP2/LiquidInCup"
{
	Properties
	{
		_Color ("Color", Color) = (0.7,0.7,1.,0.4)
		_BottomRadius ("Bottom Radius", float) = 0.1
		_GrowRadius ("Grow Radius", float) = 0.1
		_MaxHeight ("Max Height", float) = 0.1
	}

	SubShader
	{
		LOD 100

		Cull Off
		//ZWrite Off
		
		Tags {
			"Queue"="Transparent-1"
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
				float3 normal : NORMAL;
			};

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				fixed3 localVertex: TEXCOORD0;
				fixed3 worldRefl : TEXCOORD1;
				fixed maxRadius : TEXCOORD2;
			};

			uniform float4 _Color;
			uniform float _BottomRadius;
			uniform float _GrowRadius;
			uniform float _MaxHeight;
			
			v2f vert (appdata v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.localVertex = v.vertex.xyz;
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir(worldPos) );
				o.worldRefl = reflect( -worldViewDir, worldNormal );
				o.maxRadius = _BottomRadius+( v.vertex.y/_MaxHeight )*_GrowRadius;
				o.maxRadius *= o.maxRadius;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 cubemap = UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, -i.worldRefl ).rgb;
				fixed4 col = _Color;
				col.rgb *= cubemap*2.;
				//clip height
				fixed inside = step(0.,i.localVertex.y)*step( i.localVertex.y, _MaxHeight );
				//clip radius
				inside *= step( i.localVertex.x*i.localVertex.x+i.localVertex.z*i.localVertex.z, i.maxRadius );
				if( inside < 0.5 ){
					discard;
				}
				return col;
			}
			ENDCG
		}
	}
}
