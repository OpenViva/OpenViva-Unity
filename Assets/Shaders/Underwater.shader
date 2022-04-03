Shader "Hidden/Shader/Underwater"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"



    struct Attributes 
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input) 
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float _WaterLevel;
    float _FogLength;
    float _FogThickness;
    float4 _WaterFogColor;
    TEXTURE2D_X(_InputTexture);

    float SqDist( float3 a, float3 b ){
        return dot(a-b,a-b);
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 uv = input.texcoord * _ScreenSize.xy;

        float2 positionNDC = uv * _ScreenSize.zw + (0.5 * _ScreenSize.zw);
        float  deviceDepth = LoadCameraDepth(uv);
        float3 worldPos = ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP);

        float3 absWorldPos = GetAbsolutePositionWS( worldPos );

        float minWorldDist = SqDist( _WorldSpaceCameraPos, absWorldPos );

        float3 viewDir = GetWorldSpaceNormalizeViewDir( worldPos );
        float toSurface = (_WaterLevel-_WorldSpaceCameraPos.y)/min(0.0001,viewDir.y);
        float3 surfaceViewPos = _WorldSpaceCameraPos+viewDir*toSurface;
        float aboveWater = step( _WorldSpaceCameraPos.y, _WaterLevel );
        float posAboveWater = step( absWorldPos.y, _WaterLevel );

        float minSurfaceDist = SqDist( _WorldSpaceCameraPos, surfaceViewPos )*aboveWater;
        minSurfaceDist += posAboveWater*100000000;
        
        float minDist = min( minSurfaceDist, minWorldDist );

        float fog = ( 1.-saturate( _FogLength/minDist ) );
        float depth = saturate( ( _WaterLevel-absWorldPos.y )*_FogThickness );
        fog = max(fog, depth);
        fog = max(fog, 0.2);

        float3 minPos = _WorldSpaceCameraPos+viewDir*minDist;
        
        float distortStrength = _ScreenSize.xy*0.02*step( _WaterLevel, absWorldPos.y )*aboveWater;

        uv += float2( cos(sin(minPos.x*1.3+_Time.w)+_Time.z+minPos.z*0.79), sin( cos(minPos.z*1.13+_Time.z)+minPos.x*0.87 ) )*distortStrength;

        float3 col = LOAD_TEXTURE2D_X(_InputTexture, uv).rgb;

        col = lerp( col.rgb, _WaterFogColor, fog );

        // col = fog;

        return float4(col, 1);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "underwater"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
