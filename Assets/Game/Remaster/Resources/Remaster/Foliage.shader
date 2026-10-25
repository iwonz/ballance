Shader "Ballance/Remaster/Foliage"
{
  Properties
  {
    _MainTex ("Fern cutout", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = .55
    _Growth ("Vegetation", Range(0,1)) = 0
    _WeatherTime ("Weather time", Float) = 0
    _Wetness ("Wetness", Range(0,1)) = 0
  }
  SubShader
  {
    Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
    Cull Off
    CGPROGRAM
    #pragma surface surf Standard alphatest:_Cutoff fullforwardshadows vertex:vert
    #pragma target 3.0
    sampler2D _MainTex;
    half _Growth, _Wetness;
    float _WeatherTime;
    struct Input { float2 uv_MainTex; float3 worldNormal; };
    void vert(inout appdata_full v) {
      v.vertex.xyz *= _Growth;
      float phase = unity_ObjectToWorld._m03*.17 + unity_ObjectToWorld._m23*.11;
      float bend = v.texcoord.y*v.texcoord.y*_Growth;
      v.vertex.x += (sin(_WeatherTime*1.1+phase)*.10+sin(_WeatherTime*2.7+phase+v.texcoord.y*5)*.035)*bend;
      v.vertex.z += sin(_WeatherTime*.7+phase)*.08*bend;
    }
    void surf(Input IN, inout SurfaceOutputStandard o) {
      fixed4 leaf = tex2D(_MainTex, IN.uv_MainTex);
      o.Albedo = leaf.rgb*(1-_Wetness*.14); o.Smoothness = lerp(.25,.58,_Wetness); o.Alpha = leaf.a;
      float transmission = saturate(dot(-normalize(IN.worldNormal),normalize(_WorldSpaceLightPos0.xyz)));
      o.Emission = leaf.rgb * (.025+transmission*.16);
    }
    ENDCG
  }
  FallBack "Transparent/Cutout/Diffuse"
}
