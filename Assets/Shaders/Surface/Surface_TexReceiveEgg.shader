Shader "Surface/TexReceiveEgg" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Screen("Screen", Range(-1.,2.)) = 0.0
     }

     SubShader {
        
        Tags {
             "Queue"="Transparent"
             "RenderType"="Transparent"
             "PhotoData"="Opaque"
        }
        
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 eye : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
            uniform fixed _Screen;

			v2f vert (appdata v)
			{
				v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				fixed3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				o.eye = normalize( UnityWorldSpaceViewDir(worldPos) );
				return o;
			}
			

			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
                fixed4 col = tex2D( _MainTex, i.uv );
				fixed3 env = fixed3(1.,1.,1.)-UNITY_SAMPLE_TEXCUBE( unity_SpecCube0, i.eye ).rgb;
                col.rgb *= lerp( fixed3(1.,1.,1.), fixed3(1.,1.,1.)-env*env, _Screen );
				return col;
			}
			ENDCG
        }

     }

     FallBack "Transparent/Cutout/Diffuse"

 }