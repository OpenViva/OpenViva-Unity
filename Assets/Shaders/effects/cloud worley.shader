Shader "Effects/Cloud Worley Noise"
{
    Properties
    {
        _Depth ("Depth", float) = 0
        _Scale ("Scale", float) = 10
        _Darks ("Darks", float) = 1
        _GreenAlpha ("Sub Noise Alpha", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Scale;
            float _Darks;
            float _GreenAlpha;
            float _Depth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            #define MOD3 float3(.1031,.11369,.13787)
            float3 hash33(float3 p3)
            {
                p3 = frac(p3 * MOD3);
                p3 += dot(p3, p3.yxz+19.19);
                return -1.0 + 2.0 * frac(float3((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y, (p3.y+p3.z)*p3.x));
            }

            float3 rand( float3 p ){
                float3 c;
                c.r = dot( p, float3(316.1,994.5,745.25) );
                c.g = dot( p, float3(573.4,296.7,877.5) );
                c.b = dot( p, float3(189.1,573.1,894.3) );
                return frac( sin(c)*13645.5657 );
            }

            float perlin3D(float3 p){
                float3 pi = floor(p);
                float3 pf = p-pi;
                float3 w = pf*pf*(3.-2.*pf);
                float r = lerp(
                    lerp(
                        lerp(dot(pf-float3(0, 0, 0), hash33(pi+float3(0, 0, 0))), 
                            dot(pf-float3(1, 0, 0), hash33(pi+float3(1, 0, 0))),
                            w.x),
                        lerp(dot(pf-float3(0, 0, 1), hash33(pi+float3(0, 0, 1))), 
                            dot(pf-float3(1, 0, 1), hash33(pi+float3(1, 0, 1))),
                            w.x),
                        w.z),
                    lerp(
                        lerp(dot(pf-float3(0, 1, 0), hash33(pi+float3(0, 1, 0))), 
                            dot(pf-float3(1, 1, 0), hash33(pi+float3(1, 1, 0))),
                            w.x),
                        lerp(dot(pf-float3(0, 1, 1), hash33(pi+float3(0, 1, 1))), 
                            dot(pf-float3(1, 1, 1), hash33(pi+float3(1, 1, 1))),
                            w.x),
                        w.z),
                    w.y
                );
                return r*.5+.5;
            }

            float worley( float3 u ){
                
                float closest = 1000;    //closest
                float3 f = floor(u);
                float3 local = u-f;
                for( int i=-1; i<=1; i++ ){
                    for( int j=-1; j<=1; j++ ){
                        for( int k=-1; k<=1; k++ ){
                            float3 ijk = float3(i,j,k);
                            float3 w = rand( f+ijk );
                            float dist = length( ijk-local+w );
                            closest = min( dist, closest );
                        }
                    }
                }
                return 1.-pow(closest,3.);
            }

            float perlinLayered( float3 u, int octaves ){
                float r = 0.0;
                for( int i=0; i<octaves; i++ ){
                    float w = perlin3D(u);
                    r += pow(0.5,i+1)*w;
                    u *= 2.;
                }
                return r;
            }

            float worleyLayered( float3 u, int octaves ){
                
                float r = 0.0;
                for( int i=0; i<octaves; i++ ){
                    float w = worley(u);
                    w = saturate(w);
                    w = pow( w, _Darks );
                    r += pow(0.5,i+1)*w;
                    u *= 2.;
                }
                return r;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 u = float3( (i.uv-.5)*_Scale+.5, _Depth );
                float4 col;
                col.r = worleyLayered(u,4);
                col.g = worleyLayered(u*4.,4)*_GreenAlpha;
                col.b = 0.;
                col.a = 1.;
                return col;
            }
            ENDCG
        }
    }
}
