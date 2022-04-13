Shader "Nature/Canyon" {
    Properties {
        // set by terrain engine
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
        // Used in fallback on old cards & also for distant base map
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
    }
 
    SubShader {
        Tags {
            "Queue" = "Geometry-99"
            "IgnoreProjector"="True"
            "RenderType" = "Opaque"
        }
 
        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert fullforwardshadows noinstancing
        #pragma multi_compile_fog
        #pragma target 3.0
        // needs more than 8 texcoords
        #pragma exclude_renderers gles psp2
        #include "UnityPBSLighting.cginc"
 
        #pragma multi_compile  _NORMALMAP
        
        #define TERRAIN_SPLAT_ADDPASS
        #define TERRAIN_STANDARD_SHADER
        #define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
        #include "TerrainSplatmapCommon.cginc"
 
        fixed _Smooth0;
        fixed _Smooth1;
        fixed _Smooth2;
        fixed _Smooth3;
        // sampler2D _Normal0;
        // sampler2D _Normal1;
        // sampler2D _Normal2;
        // sampler2D _Normal3;
        // float _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
 
        void surf (Input IN, inout SurfaceOutputStandard o) {

            float2 uvSplat0 = TRANSFORM_TEX(IN.tc.xy, _Splat0);
            float2 uvSplat1 = TRANSFORM_TEX(IN.tc.xy, _Splat1);
            float2 uvSplat2 = TRANSFORM_TEX(IN.tc.xy, _Splat2);
            float2 uvSplat3 = TRANSFORM_TEX(IN.tc.xy, _Splat3);

            //calculate splatmap composite
            fixed4 control = tex2D(_Control, IN.tc.xy );
            fixed4 data0 = tex2D( _Splat0, uvSplat0 ); 
            fixed4 data1 = tex2D( _Splat1, uvSplat1 );
            fixed4 data2 = tex2D( _Splat2, uvSplat2 );
            fixed4 data3 = tex2D( _Splat3, uvSplat3 );
            fixed4 blends;
            blends.g = smoothstep(data1.a*0.8,data1.a,control.g);
            blends.b = smoothstep(data2.a*0.5,data2.a,control.b);
            blends.a = smoothstep(data3.a*0.8,data3.a,control.a);

            //composite blends
            blends.b = saturate( blends.b-blends.a );
            blends.g = saturate( blends.g-blends.b-blends.a );
            blends.r = 1.-(blends.g+blends.b+blends.a);

            fixed canyonNormalStrength = _NormalScale2*blends.b+blends.g*_NormalScale1;
            fixed3 canyonNormal = UnpackNormalWithScale(tex2D(_Normal1, uvSplat1), canyonNormalStrength ); //share normal for layers 1, 2, 3
            fixed3 mixedNormal;
            mixedNormal  = UnpackNormalWithScale(tex2D(_Normal0, uvSplat0), _NormalScale0 )*blends.r;
            mixedNormal += canyonNormal;
            mixedNormal += fixed3(0.,0.,1.)*blends.a;

            o.Normal = mixedNormal;
            o.Albedo = data0.rgb*blends.r+data1.rgb*blends.g+data2.rgb*blends.b+data3.rgb*blends.a;
            o.Smoothness = o.Albedo.b;
        }
        ENDCG
        
    }
    Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
    Dependency "BaseMapShader" = "Diffuse"
    Dependency "Details0"      = "Hidden/TerrainEngine/Details/Vertexlit"
    Dependency "Details1"      = "Hidden/TerrainEngine/Details/WavingDoublePass"
    Dependency "Details2"      = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
    Dependency "Tree0"         = "Hidden/TerrainEngine/BillboardTree"

    Fallback "Diffuse"
}

