Shader "Water/RiverUnderwater"
{
    Properties 
    {
		_MainTex ("Main Texture", 2D) = "white" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _FoamAmount ("Foam Choke", Range(0.,1.0)) = 0.05
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DirtTex ("Dirt Texture", 2D) = "normal" {}
        _NormalTex ("Normal Texture", 2D) = "normal" {}
        _NormalStrength ("Normal Strength Mult", Range(0.,0.5)) = 0.25
        _PrimaryWaterColor ("Primary Water Color", Color) = (0.,0.,1.,0.)
        _FlowSpeed ("Flow Speed", Range(0.,4.)) = 2.
        _DirtScale ("Dirt Scale", Range(0.,0.2)) = 0.15
        _DirtAmount ("Dirt Choke", Range(0.,1.0)) = 0.05
        _DirtDistortion ("Dirt Distortion", Range(0.,0.1)) = 0.02
    }

    SubShader 
    {
        Cull front
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        //PASS
		Pass
		{
			Tags {
			"Queue"="Transparent" 
			"RenderType"="Transparent"
            }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float3 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
                fixed3 worldPos : TEXCOORD1;
                fixed3 depthDirtFoam : TEXCOORD2;
				LIGHTING_COORDS(3,4)
				float3 screenPos : TEXCOORD5;

                UNITY_VERTEX_OUTPUT_STEREO
			};

            uniform sampler2D _MainTex;
            uniform fixed3 _PrimaryWaterColor;
            uniform fixed _DirtStrength;
            uniform fixed _FlowSpeed;
            uniform fixed4 _RimColor;
            uniform fixed _FoamAmount;
            uniform sampler2D _FoamTex;
            uniform sampler2D _NoiseTex;
            uniform sampler2D _NormalTex;
            uniform fixed _NormalStrength;
            uniform fixed _DirtScale;
            uniform fixed _DirtAmount;
            uniform fixed _DirtDistortion;

            uniform sampler2D _DirtTex;

			v2f vert (appdata v){
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos( v.vertex ); 
                o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
                o.uv = v.uv;
                o.uv.y += _Time.x*2.;
                o.depthDirtFoam = v.color;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				o.screenPos = ComputeScreenPos( o.vertex ).xyw;
				return o;
			}

            fixed noiseBlend( fixed f ){
                fixed p = step( 0.5, f );
                fixed r = f*2.*(1.-p)+(1.-(f-.5)*2.)*p;
                return smoothstep( 0., 1., r );

            }

            fixed2 pseudoRandomSample( sampler2D tex, fixed2 uv, fixed time ){
                fixed n1 = noiseBlend( frac( time ) );
                fixed n2 = noiseBlend( frac( time+0.3333 ) );
                fixed n3 = noiseBlend( frac( time+0.6666 ) );
                fixed flowSpeed = _Time.x*_FlowSpeed;
                return (tex2D( tex, uv+fixed2(-flowSpeed,0.0)+floor( time )*0.541 ).rg*n1+
                        tex2D( tex, uv*1.25+fixed2(-flowSpeed,-flowSpeed)-floor( time+0.3333 )*0.781 ).gb*n2+
                        tex2D( tex, uv*1.75+fixed2(-flowSpeed,flowSpeed)+floor( time+0.6666 )*0.367 ).rb*n3 )*0.6666;
            }
			
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                //generate pseudo random 2D noise
                fixed2 noise = pseudoRandomSample( _NoiseTex, i.uv, _Time.y );
                fixed3 view = normalize( _WorldSpaceCameraPos.xyz-i.worldPos );

                //shadows
                fixed spec = saturate( dot(-_WorldSpaceLightPos0.xyz,view)-0.4 );
                spec = pow( spec, 2.0 );
                fixed3 environment = UNITY_LIGHTMODEL_AMBIENT+_LightColor0*spec;

                //light functions
                fixed3 norm = fixed3( 0., 1., 0. );
                fixed2 distortion = (tex2D( _NormalTex, i.uv ).xy-1.+noise)*_NormalStrength;
                norm.xz += distortion;

                fixed wave = saturate(abs(distortion.x-distortion.y));

                //moss
                fixed4 moss = tex2D( _DirtTex, i.worldPos.xz*_DirtScale+distortion*_DirtDistortion );
                moss.rgb *= environment;
                fixed dirtAlpha = step(moss.a, i.depthDirtFoam.g-_DirtAmount);
                
                //foam
                fixed3 foam = tex2D( _FoamTex, i.uv+distortion ).rgb*environment;
                fixed randomFoam = abs(distortion.x+distortion.y);
                fixed foamAlpha = 1.-smoothstep( i.depthDirtFoam.b*randomFoam, i.depthDirtFoam.b*_FoamAmount, randomFoam );
                
                //screen texture
				fixed2 screenscreenPos = i.screenPos.xy/i.screenPos.z;
                fixed3 color = tex2D( _MainTex, screenscreenPos+distortion ).rgb+_LightColor0*spec;
 
                //composite
                color = lerp( color, foam, foamAlpha );
                color = lerp( color, moss.rgb, dirtAlpha );
                color += _PrimaryWaterColor*wave;
                color = lerp( color, moss.rgb, dirtAlpha );
                return fixed4( color, 1.0 );
			}
			ENDCG
		}
    }
}