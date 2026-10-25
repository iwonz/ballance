Shader "Ballance/Remaster/CachedSky"
{
  Properties {
    _MainTex ("Cloud sky", Cube) = "grey" {}
    _PreviousTex ("Previous sky", Cube) = "grey" {}
    _Blend ("Capture blend", Range(0,1)) = 1
  }
  SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      samplerCUBE _MainTex, _PreviousTex;
      float _Blend;
      struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
      v2f vert(float4 vertex : POSITION) {
        v2f o; o.position = UnityObjectToClipPos(vertex); o.direction = vertex.xyz; return o;
      }
      fixed4 frag(v2f i) : SV_Target {
        fixed4 current = texCUBE(_MainTex,i.direction);
        if (_Blend < .999) current = lerp(texCUBE(_PreviousTex,i.direction),current,_Blend);
        return current;
      }
      ENDCG
    }
  }
}
