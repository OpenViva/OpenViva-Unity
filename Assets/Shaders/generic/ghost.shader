Shader "Custom/ghost"
{
    Properties
    {
		_MainTex ("Main Texture", 2D) = "white" {}
		_Strength ("Strength", Range(0,0.2)) = 0.1
		_Spread ("Spread", Range(0,4)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:fade

        sampler2D _MainTex;
        fixed _Strength;
        fixed _Spread;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed2 ghostUV( fixed2 uv, fixed offset, fixed scale ){
            fixed s1 = sin( (uv.x+uv.y)*scale+offset );
            fixed s2 = sin( s1+(uv.x-uv.y)*scale*1.1 );
            fixed s3 = sin( s2+(uv.x+uv.y)*scale*1.2 );
            fixed c1 = sin( s3+(uv.x-uv.y)*scale*1.3 );
            return uv+fixed2( s3, c1 )*_Strength;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 color;

            color = tex2D(_MainTex, ghostUV( IN.uv_MainTex, _Time.z, 4.0 ) );
            color += tex2D(_MainTex, ghostUV( IN.uv_MainTex, _Time.z+_Spread, 4.0 ) )*0.2;
            color += tex2D(_MainTex, ghostUV( IN.uv_MainTex, _Time.z+_Spread*2.0, 4.0 ) )*0.2;

            o.Albedo = color.rgb;
            o.Metallic = 0.;
            o.Smoothness = 0.;
            o.Alpha = color.a;
            o.Emission = fixed3( 0.1, 0.1, 0.1 );
        }
        ENDCG
    }
    FallBack "Diffuse"
}
