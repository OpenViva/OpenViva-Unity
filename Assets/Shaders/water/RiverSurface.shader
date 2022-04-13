Shader "Water/RiverSurface"
{
    Properties 
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _FoamAmount ("Foam Choke", Range(0.,1.0)) = 0.05
        _DirtTex ("Dirt Texture", 2D) = "normal" {}
        _NormalTex ("Normal Texture", 2D) = "normal" {}
        _NormalStrength ("Normal Strength Mult", Range(0.,0.5)) = 0.25
        _PrimaryWaterColor ("Primary Water Color", Color) = (0.,0.,1.,0.)
        _FlowSpeed ("Flow Speed", Range(0.,4.)) = 2.
        _DirtScale ("Dirt Scale", Range(0.,0.2)) = 0.15
        _DirtAmount ("Dirt Choke", Range(0.,1.0)) = 0.05
        _SpecStrength ("Spec Strength", Range(2.0,64.0)) = 0.9
        _DirtDistortion ("Dirt Distortion", Range(0.,0.1)) = 0.02
    }

    SubShader 
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Cull back
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha



		Pass
		{
			Tags {
				"LightMode" = "ForwardBase"
				"RenderType"="Opaque"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile_fog
			// #pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float3 color : COLOR;
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;	//must use 'pos' for TRANSFER_VERTEX_TO_FRAGMENT
				fixed2 uv : TEXCOORD0;
                fixed3 worldPos : TEXCOORD1;
                fixed3 depthDirtFoam : TEXCOORD2;
				LIGHTING_COORDS(3,4)
				UNITY_FOG_COORDS(5)
			};

            uniform fixed3 _PrimaryWaterColor;
            uniform fixed _DirtStrength;
            uniform fixed _FlowSpeed;
            uniform fixed _FoamAmount;
            uniform sampler2D _FoamTex;
            uniform sampler2D _NoiseTex;
            uniform sampler2D _NormalTex;
            uniform fixed _NormalStrength;
            uniform fixed _DirtScale;
            uniform fixed _DirtAmount;
            uniform fixed _DirtDistortion;
            uniform fixed _SpecStrength;

            uniform sampler2D _DirtTex;

			v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos( v.vertex ); 
                o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
                o.uv = v.uv;
                o.uv.y += _Time.x*2.;
                o.depthDirtFoam = v.color;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				UNITY_TRANSFER_FOG(o,o.vertex);
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
				//generate pseudo random 2D noise
                fixed2 noise = pseudoRandomSample( _NoiseTex, i.uv, _Time.y );
                fixed3 view = normalize( _WorldSpaceCameraPos.xyz-i.worldPos );
                // return fixed4( noise.rr, 0., 1. );

                //shadows
				fixed atten = LIGHT_ATTENUATION(i);
                fixed3 environment = UNITY_LIGHTMODEL_AMBIENT+_LightColor0*(0.2+atten*0.8);

                //light functions
                fixed3 norm = fixed3( 0., 1., 0. );
                fixed2 distortion = (tex2D( _NormalTex, i.uv ).xy-1.+noise)*_NormalStrength;
                norm.xz += distortion;

                //moss
                fixed4 moss = tex2D( _DirtTex, i.worldPos.xz*_DirtScale+distortion*_DirtDistortion );
                moss.rgb *= environment;
                fixed dirtAlpha = step(moss.a, i.depthDirtFoam.g-_DirtAmount);
                fixed planeDotView = saturate( dot( reflect( -_WorldSpaceLightPos0.xyz, moss ), view ) );
                moss += _LightColor0*( planeDotView*planeDotView*16.0 )*moss.g*moss.g*atten;

                atten = 1.-atten;
                fixed spec = saturate( planeDotView-atten-distortion.r*_SpecStrength );
 
                //water color
                fixed3 water = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflect( -view, norm ) ).rgb*_PrimaryWaterColor;

                //foam
                fixed3 foam = tex2D( _FoamTex, i.uv+distortion ).rgb*environment;
                fixed randomFoam = abs(distortion.x+distortion.y);
                fixed foamAlpha = 1.-smoothstep( i.depthDirtFoam.b*randomFoam, i.depthDirtFoam.b*_FoamAmount, randomFoam );

                //composite
                fixed3 color = lerp( water, foam, foamAlpha )+_LightColor0*spec;
                color = lerp( color, moss.rgb, dirtAlpha );
				UNITY_APPLY_FOG(i.fogCoord, color);
                // return fixed4( color, 1. );
                return fixed4( color, saturate( 0.5+i.depthDirtFoam.r*0.5+dirtAlpha ) );
			}
			ENDCG
		}
    }
    Fallback "Diffuse"

}