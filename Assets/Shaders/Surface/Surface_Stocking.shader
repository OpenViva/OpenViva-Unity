Shader "Surface/Stocking" {

     Properties {
		_OutlineColor ("Outline color",Color) = (1.,1.,1.)
		_OutSizeMin ("Outline Size Min",Range(0.,0.003)) = 0.001
		_OutSizeMax ("Outline Size Max",Range(0.,0.003)) = 0.001
        _MainTex ("Texture", 2D) = "white" {}
        _BumpMap ("Bumpmap", 2D) = "bump" {}
        _SkinColor ("Skin Color", Color) = (0.26,0.19,0.16,0.0)
        _RimMin ("Rim Min", Range(0,1.0)) = 0.0
        _RimMax ("Rim Max", Range(0,16.0)) = 0.0
    }
    SubShader {
        
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "IgnoreProjector"="True"
            "PhotoData"="Opaque"
        }      

        LOD 100
        Cull Off


        CGPROGRAM        
        #pragma surface surf Lambert alphatest:_Cutoff addshadow nometa nolightmap nodynlightmap nodirlightmap

        struct Input {
            fixed2 uv_MainTex;
            fixed3 viewDir;
        };
        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed3 _SkinColor;  
        fixed _RimMin;
        fixed _RimMax;
        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            clip(c.a-0.5);
            o.Normal = UnpackNormal(tex2D (_BumpMap, IN.uv_MainTex));

            half rim = saturate( ( dot(normalize(IN.viewDir), o.Normal)-_RimMin )*_RimMax );
            o.Albedo = lerp(c.rgb,_SkinColor, rim*rim );
            // o.Albedo = o.Emission;
        }
        ENDCG
    } 
    Fallback "Diffuse"
 }