Shader "Ballance/Remaster/Surface"
{
  Properties
  {
    _MainTex ("Authored layout", 2D) = "white" {}
    _NaturalTex ("Natural albedo", 2D) = "white" {}
    _ReliefTex ("Normal XY / height / roughness", 2D) = "bump" {}
    _SnowRelief ("Snow relief", 2D) = "bump" {}
    _IceRelief ("Ice relief", 2D) = "bump" {}
    _SandRelief ("Sand relief", 2D) = "bump" {}
    _MossRelief ("Moss relief", 2D) = "bump" {}
    _HeightScale ("Parallax depth", Range(0,0.06)) = 0.025
    _SnowTex ("Snow albedo", 2D) = "white" {}
    _IceTex ("Ice albedo", 2D) = "white" {}
    _SandTex ("Sand albedo", 2D) = "white" {}
    _MossTex ("Moss albedo", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)
    _NaturalBlend ("Natural blend", Range(0,1)) = 0
    _DetailScale ("Detail scale", Float) = 3
    _Metallic ("Metallic", Range(0,1)) = 0
    _Smoothness ("Smoothness", Range(0,1)) = 0.3
    _Emission ("Emission", Color) = (0,0,0,1)
    _Surface ("Wet / snow / ice / sand", Vector) = (0,0,0,0)
    _Vegetation ("Vegetation", Range(0,1)) = 0
    _RegionCount ("Local covering count", Int) = 0
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 250
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
    #pragma target 3.5
    sampler2D _MainTex, _NaturalTex, _SnowTex, _IceTex, _SandTex, _MossTex;
    sampler2D _ReliefTex, _SnowRelief, _IceRelief, _SandRelief, _MossRelief;
    float4 _Surface;
    float4 _RegionCenters[8], _RegionExtents[8], _RegionWeights[8];
    float4x4 _WorldToSurface;
    int _RegionCount;
    fixed4 _Color, _Emission;
    half _NaturalBlend, _DetailScale, _Metallic, _Smoothness, _HeightScale, _Vegetation;
    float _RemasterWeatherTime, _RemasterRain;
    struct Input { float2 uv_MainTex; float3 localPos; float3 surfaceWorldNormal; float3 surfaceTangent; float3 surfaceBitangent; float3 viewDir; float3 worldPos; };
    void vert(inout appdata_full v, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      o.localPos = mul(_WorldToSurface, mul(unity_ObjectToWorld, v.vertex)).xyz;
      o.surfaceWorldNormal = UnityObjectToWorldNormal(v.normal);
      o.surfaceTangent = UnityObjectToWorldDir(v.tangent.xyz);
      o.surfaceBitangent = cross(o.surfaceWorldNormal, o.surfaceTangent) * v.tangent.w * unity_WorldTransformParams.w;
    }
    float3 reliefNormal(float4 relief) {
      float2 xy = relief.rg * 2 - 1;
      return normalize(float3(xy, sqrt(saturate(1-dot(xy,xy)))));
    }
    float surfaceHash(int x, int y) {
      uint h = (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu;
      h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15;
      return (h & 65535u) / 65535.0;
    }
    float surfaceNoise(float2 p) {
      int2 cell = (int2)floor(p);
      float2 f = frac(p); f = f*f*(3-2*f);
      return lerp(lerp(surfaceHash(cell.x,cell.y),surfaceHash(cell.x+1,cell.y),f.x),
        lerp(surfaceHash(cell.x,cell.y+1),surfaceHash(cell.x+1,cell.y+1),f.x),f.y);
    }
    float regionMask(float3 p, float3 center, float3 extent, float3 normal) {
      float3 axis = abs(normal);
      int thin = axis.y >= axis.x && axis.y >= axis.z ? 1 : axis.x > axis.z ? 0 : 2;
      int a = (thin+1)%3, b = (thin+2)%3;
      float3 q = (p-center)/max(extent,.001);
      float2 plane = float2(q[a],q[b]);
      float radius = length(plane), vertical = abs(q[thin]);
      if (radius > 1.54 || vertical > 1.1) return 0;
      float edge = .72 + .46*surfaceNoise(plane*3+float2(center[a],center[b])*.037)
        + .14*(surfaceNoise(plane*11+float2(center[a],center[b])*.037)-.5);
      return (1-smoothstep(.78,1,radius*edge))*(1-smoothstep(1,1.1,vertical));
    }
    void surf(Input IN, inout SurfaceOutputStandard o)
    {
      float2 uv = IN.uv_MainTex * _DetailScale;
      float3 view = normalize(IN.viewDir);
      float height = tex2D(_ReliefTex, uv).b;
      uv += view.xy / max(view.z,.35) * (height-.5) * _HeightScale;
      float4 relief = tex2D(_ReliefTex, uv);
      fixed3 authored = tex2D(_MainTex, IN.uv_MainTex).rgb;
      fixed3 natural = tex2D(_NaturalTex, uv).rgb;
      fixed3 albedo = lerp(authored, natural, _NaturalBlend) * _Color.rgb;
      o.Normal = reliefNormal(relief);
      o.Smoothness = 1-relief.a;
      o.Occlusion = lerp(.88,1,saturate(relief.b*2));
      float4 covering = 0;
      float3 localNormal = normalize(mul((float3x3)_WorldToSurface, normalize(IN.surfaceWorldNormal)));
      [branch] if (max(max(_Surface.y,_Surface.z),max(_Surface.w,_Vegetation)) > .001)
      for (int i = 0; i < _RegionCount; i++) {
        float region = regionMask(IN.localPos, _RegionCenters[i].xyz, _RegionExtents[i].xyz, localNormal);
        region *= smoothstep(0.35, 0.8, normalize(IN.surfaceWorldNormal).y);
        covering = max(covering, _RegionWeights[i] * region);
      }
      float snow = saturate(_Surface.y * covering.x);
      float ice = saturate(_Surface.z * covering.y);
      float sand = saturate(_Surface.w * covering.z);
      float moss = saturate(_Vegetation * covering.w);
      albedo *= 1 - saturate(_Surface.x) * 0.18;
      o.Smoothness = lerp(o.Smoothness,.82,saturate(_Surface.x));
      o.Normal = normalize(lerp(o.Normal,float3(0,0,1),saturate(_Surface.x)*.25));
      if (_RemasterRain > .01 && IN.surfaceWorldNormal.y > .8 && _RegionCount > 0) {
        float2 tile = IN.localPos.xz*.45;
        float seed = frac(sin(dot(floor(tile),float2(127.1,311.7)))*43758.5453);
        float age = frac(_RemasterWeatherTime*1.4+seed);
        float2 p = frac(tile)-float2(.2+seed*.6,.8-seed*.6);
        float radius = length(p);
        float ripple = sin((radius-age*.45)*80)*exp(-abs(radius-age*.45)*24)*(1-age);
        float3 worldRipple = mul(float3(p.x,0,p.y),(float3x3)_WorldToSurface);
        float2 tangentRipple = float2(dot(worldRipple,IN.surfaceTangent),dot(worldRipple,IN.surfaceBitangent));
        o.Normal = normalize(o.Normal+float3(tangentRipple*ripple*_Surface.x*_RemasterRain*.16,0));
      }
      if (max(max(snow,ice),max(sand,moss)) > .001) {
      float3 axis = abs(localNormal);
      float2 coverUV = (axis.y >= axis.x && axis.y >= axis.z ? IN.localPos.xz :
        axis.x > axis.z ? IN.localPos.zy : IN.localPos.xy) * .1;
      float4 coverRelief = 0;
      [branch] if (snow > .001) coverRelief += tex2D(_SnowRelief,coverUV)*snow;
      [branch] if (ice > .001) coverRelief += tex2D(_IceRelief,coverUV)*ice;
      [branch] if (sand > .001) coverRelief += tex2D(_SandRelief,coverUV)*sand;
      [branch] if (moss > .001) coverRelief += tex2D(_MossRelief,coverUV)*moss;
      coverRelief /= max(snow+ice+sand+moss,.001);
      float3 localView = mul((float3x3)_WorldToSurface, normalize(_WorldSpaceCameraPos-IN.worldPos));
      float2 projectedView = axis.y >= axis.x && axis.y >= axis.z ? localView.xz : axis.x > axis.z ? localView.zy : localView.xy;
      coverUV += projectedView * (coverRelief.b-.5)*.04;
      [branch] if (snow > .001) albedo = lerp(albedo,tex2D(_SnowTex,coverUV).rgb,snow);
      [branch] if (ice > .001) albedo = lerp(albedo,tex2D(_IceTex,coverUV).rgb,ice*.7);
      [branch] if (sand > .001) albedo = lerp(albedo,tex2D(_SandTex,coverUV).rgb,sand);
      [branch] if (moss > .001) albedo = lerp(albedo,tex2D(_MossTex,coverUV).rgb,moss);
      float3 n = reliefNormal(coverRelief);
      float3 localRelief = axis.y >= axis.x && axis.y >= axis.z ? float3(n.x,n.z*sign(localNormal.y),n.y) :
        axis.x > axis.z ? float3(n.z*sign(localNormal.x),n.y,n.x) : float3(n.x,n.y,n.z*sign(localNormal.z));
      float3 worldRelief = normalize(mul(localRelief,(float3x3)_WorldToSurface));
      float3 tangentRelief = float3(dot(worldRelief,normalize(IN.surfaceTangent)),dot(worldRelief,normalize(IN.surfaceBitangent)),dot(worldRelief,normalize(IN.surfaceWorldNormal)));
      float amount = saturate(snow+ice+sand+moss);
      o.Normal = normalize(lerp(o.Normal,tangentRelief,amount));
      o.Smoothness = lerp(o.Smoothness,1-coverRelief.a,amount);
      }
      o.Albedo = albedo;
      o.Metallic = _Metallic * (1 - snow) * (1 - sand) * (1-moss);
      o.Emission = _Emission.rgb * albedo;
      o.Alpha = 1;
    }
    ENDCG
  }
  FallBack "Standard"
}
