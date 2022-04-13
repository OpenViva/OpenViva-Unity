Shader "Surface/Surface_PBR_Pastry" {

     Properties {
         _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
         _Metallic ("Metallic", 2D ) = "black" {}
         _FillingColor ("Filling Color", Color) = (1,0,0,0)
         _Normal ("Normal", 2D ) = "bump" {}
         _PhotoDataColor ("Photo Data Color", Color) = (0,0,0,1)
     }

     SubShader {
        
        Tags {
             "RenderType"="Opaque"
             "PhotoData"="Opaque"
        }

        LOD 200
        Cull Off

        CGPROGRAM        

        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow

        sampler2D _MainTex;
        sampler2D _Metallic;
        sampler2D _Normal;
        fixed4 _FillingColor;

        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o) {            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            fixed2 data = tex2D(_Metallic, IN.uv_MainTex).ra;
            o.Albedo = lerp( _FillingColor*lerp(1.,c.r,_FillingColor.a), c.rgb, c.a );
            o.Metallic = data.r;
            o.Smoothness = lerp( 0.25, data.g, _FillingColor.a );

            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex));
        }

        ENDCG
     }
 }