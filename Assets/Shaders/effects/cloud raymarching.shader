Shader "Effects/Clouds Raymarching"
{
    Properties
    {
        _VolumeMap ("Volume Map Texture", 3D) = "white" {}
        _CloudMap ("Cloud Map Texture", 2D) = "white" {}
        _SkyScale ("Sky Scale", range(0.001, 0.004)) = 0.01
        _BillowScale ("Billow Scale", range(0.001, 0.02)) = 0.01
        _CloudHeight ("Cloud Height", range(10.0, 200.0)) = 1.0
        _ErodeHeight ("Erode Height", range(16.0, 64.0)) = 32.0
        _ErodeStrength ("Erode Strength", range(0.0001, 0.1)) = 1.0
        _HGConstant ("HG Constant", range(1.0, 4.0)) = 1.0
        _Density ("Density", range(0., 32.)) = 8.
        _ShadowDensity ("Shadow Density", range(0., 20.)) = 1. 
		_SkyColor ("Sky Color", Color) = (0.6,1.,0.7,1.0)
		_CloudColorA ("Cloud Color A", Color) = (0.6,0.3,0.4,1.0)
		_CloudColorB ("Cloud Color B", Color) = (0.9,0.7,0.9,1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            struct appdata
            {
                fixed4 vertex : POSITION;
                fixed2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                fixed4 vertex : SV_POSITION;
				fixed3 worldPos: TEXCOORD1;
				fixed3 eye: TEXCOORD2;
                fixed ceiling: TEXCOORD3;

				UNITY_VERTEX_OUTPUT_STEREO
            };

            #define D_STEPS 32.
            #define S_STEPS 6.
            
            sampler3D _VolumeMap;
            sampler2D _CloudMap;
            uniform fixed _SkyScale;
            uniform fixed _CloudHeight;
            uniform fixed _ErodeHeight;
            uniform fixed _ErodeStrength;
            uniform fixed _BillowScale;
            uniform fixed _HGConstant;
            uniform fixed _Density;
            uniform fixed _ShadowDensity;
			uniform fixed3 _SkyColor;
			uniform fixed3 _CloudColorA;
			uniform fixed3 _CloudColorB;
            uniform half3 _DayNightLightDir;
            
            fixed map( fixed3 cPos ){
                fixed2 volume = tex3D( _VolumeMap, cPos*_BillowScale ).rg;
                fixed base = volume.r*tex2D( _CloudMap, cPos.xz*_SkyScale ).r;
                fixed detail = volume.g;
                base -= detail*(1.-base);
				return saturate(base);
			}
            #define SKY_HEIGHT 105.0

            fixed height( fixed y, fixed ceiling ){
                return saturate( (ceiling-y)*_ErodeStrength );
            }
            
            fixed henyeyGreenstein( fixed g, fixed dotSunEye ){
                dotSunEye *= dotSunEye;
                fixed g2 = g*g;
                return 1.-( (1.-g2)/pow(1.+g2-2.*g*dotSunEye, 1.5 ))*.25;
            }


            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				o.eye = normalize( o.worldPos-_WorldSpaceCameraPos );
                o.ceiling = _CloudHeight+_ErodeHeight;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                //project worldpos to horizon
                fixed3 cPos = i.worldPos-_WorldSpaceCameraPos;
                cPos *= ((_CloudHeight-_WorldSpaceCameraPos.y)/cPos.y);
                cPos += _WorldSpaceCameraPos;
                fixed3 eye = normalize(i.eye);


				fixed cloudDensity = 0.;
                fixed dStep = 1./D_STEPS;
                fixed transmittence = 1.;
                const fixed Density = _Density*dStep;
                const fixed sStep = 1./S_STEPS;
                const fixed ShadowDensity = _ShadowDensity*sStep;
                const fixed3 lightDir = _WorldSpaceLightPos0.xyz*_DayNightLightDir;
                fixed light = 0.;
                
                //start step at the end if below sea level
                int c = (step( eye.y, 0. ))*D_STEPS;

                fixed dotSunEye = -dot( lightDir, eye );

				for( ; c<int(D_STEPS); c++ ){

                    fixed cloud = map(cPos)*height( cPos.y, i.ceiling );
                    if( cloud > 0.001 ){

                        fixed3 sPos = cPos;
                        fixed shadowDensity = 0.;
                        for( int s=0; s<int(S_STEPS); s++ ){
                            sPos += lightDir;
                            shadowDensity += map(sPos);
                        }
                        cloudDensity = cloud*Density;
                        fixed shadow = exp( -shadowDensity*ShadowDensity );
                        fixed absorbedLight = shadow*cloudDensity;
                        light += absorbedLight*transmittence*henyeyGreenstein( _HGConstant, dotSunEye );
                        transmittence *= 1.-cloudDensity;

					    cPos += eye;
                        if( transmittence < 0.001 || cPos.y > i.ceiling ){
                            break;
                        }
                    }else{
    					cPos += eye*1.5;
                        if( cPos.y > i.ceiling ){
                            break;
                        }
                    }
				}
                fixed alpha = 1.-transmittence;
				fixed3 cloudColor = lerp( _CloudColorA, _CloudColorB, light/max(alpha,0.01) );
                return fixed4( cloudColor, alpha );
            }
            ENDCG
        }
    }
}
