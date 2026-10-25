Shader "LikeVirtools/BlinnPhongSpeicalEmissionTransparentNoSpecular"
{
  Properties
  {
    _AmbientColor ("Ambient", Color) = (0,0,0,1)
    _Color ("Diffuse", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
    _Emission ("Emission", Color) = (0,0,0,1)
  }
  SubShader
  {
    Tags {  
      "Queue" = "Transparent" 
      "RenderType" = "Transparent"
      "IgnoreProjector" = "True"
    }

    LOD 100

    Pass
    {
      // Enable depth writes.
      ZWrite On
      // Set the color write mask; 0 disables all color writes.
      ColorMask 0
    }

    Pass
    {
      Blend SrcAlpha OneMinusSrcAlpha
      ZWrite Off

      Name "FORWARD" 
      Tags { 
        "LightMode" = "ForwardBase" 
      }
      
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      // make fog work
      #pragma multi_compile_fog
      #pragma multi_compile_fwdbase
      #pragma multi_compile LIGHTPROBE_SH
      #define NO_SPECULAR
      #define USE_TRANSPARENT


      // Include headers.
      #include "UnityCG.cginc"
      #include "Lighting.cginc"
      #include "AutoLight.cginc"
      #include "BlinnPhongSpeicalEmission.cginc"

      VertexOutputBase vert (VertexInput v) { return vertForwardBase(v); }
      fixed4 frag (VertexOutputBase i) : SV_Target { return fragForwardBase(i); }

      ENDCG
    }
  }
}
