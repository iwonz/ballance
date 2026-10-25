Shader "Ballance/Remaster/Precipitation"
{
  Properties { _Streak ("Rain streak", Range(0,1)) = 0 }
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off ZWrite Off
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_fog
      #include "UnityCG.cginc"
      UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
      float _Streak;
      struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
      struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 projected:TEXCOORD1; UNITY_FOG_COORDS(2) };
      v2f vert(appdata v) {
        v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv;
        o.projected=ComputeScreenPos(o.vertex); COMPUTE_EYEDEPTH(o.projected.z);
        UNITY_TRANSFER_FOG(o,o.vertex); return o;
      }
      fixed4 frag(v2f i):SV_Target {
        float2 p=i.uv*2-1;
        float soft=saturate((LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.projected)))-i.projected.z)*2);
        float shape=lerp(saturate(1-dot(p,p)),exp(-p.x*p.x*12)*saturate(1-p.y*p.y),_Streak);
        fixed4 color=fixed4(i.color.rgb,i.color.a*shape*soft); UNITY_APPLY_FOG(i.fogCoord,color); return color;
      }
      ENDCG
    }
  }
}
