Shader "Surface/ParallaxCorrected" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Roughness ("Metallic Roughness Texture", 2D) = "white" {}
         _NormalTex ("Normal Texture", 2D) = "bump" {}
         _CubeMap ("Cube Map", CUBE) = "" {}
         _CubeCenter ("Cube center",Vector) = (0,0,0)
         _CubeMin ("Cube min",Vector) = (0,0,0)
         _CubeMax ("Cube max",Vector) = (0,0,0)
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
     }

     SubShader {

        Tags {
             "RenderType"="Opaque"
             "PhotoData"="Opaque"
        }

        LOD 200
        Cull Back

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows addshadow

        sampler2D _MainTex;
        sampler2D _Roughness;
        sampler2D _NormalTex;
        samplerCUBE _CubeMap;
        uniform float4 _CubeCenter;
        uniform float3 _CubeMin;
        uniform float3 _CubeMax;

        struct Input {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldRefl; INTERNAL_DATA
            float3 worldPos;

        };
        void surf (Input IN, inout SurfaceOutputStandard o) {

            o.Normal = UnpackNormal (tex2D (_NormalTex, IN.uv_MainTex));
            fixed2 data = tex2D(_Roughness, IN.uv_MainTex).ra;
            o.Metallic = data.r;
            o.Smoothness = data.g;
            
            //compute parallax correction
            fixed3 r = WorldReflectionVector(IN, o.Normal);
            fixed3 closest;
            fixed3 rMax = (_CubeMax-IN.worldPos)/r;
            fixed3 rMin = (_CubeMin-IN.worldPos)/r;
            fixed3 far = max( rMax, rMin );
            fixed dist = min( min( far.x, far.y ), far.z );
            fixed3 intersect = IN.worldPos+r*dist;
            r = intersect-_CubeCenter;
            
            o.Albedo = lerp( texCUBE(_CubeMap, r).rgb, tex2D (_MainTex, IN.uv_MainTex).rgb, o.Smoothness );
        }

        ENDCG

     }
 }