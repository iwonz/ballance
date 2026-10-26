Shader "Ballance/Remaster/LayeredSky"
{
  Properties
  {
    _FrontTex ("Front", 2D) = "grey" {}
    _BackTex ("Back", 2D) = "grey" {}
    _LeftTex ("Left", 2D) = "grey" {}
    _RightTex ("Right", 2D) = "grey" {}
    _UpTex ("Top", 2D) = "grey" {}
    _DownTex ("Bottom", 2D) = "grey" {}
    _Weather ("Cloud / fog / warmth / rain", Vector) = (0,0,0,0)
    _ThemeSky ("Premultiplied sky color / weight", Vector) = (0,0,0,0)
    _WeatherTime ("Simulation time", Float) = 0
    _CloudQuality ("Cloud layers", Float) = 1
    _CloudVolume ("Periodic cloud density", 3D) = "white" {}
    _CloudGroups ("Large cloud groups", 2D) = "grey" {}
    _CloudBase ("Cloud deck altitude", Float) = -600
    _CloudSun ("Sun direction", Vector) = (.3,.6,.4,0)
  }
  SubShader
  {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off
    CGINCLUDE
      #include "UnityCG.cginc"
      sampler2D _FrontTex;
      sampler3D_float _CloudVolume;
      sampler2D_float _CloudGroups;
      float4 _Weather, _ThemeSky, _CloudSun;
      float _WeatherTime, _CloudQuality, _CloudBase;
      struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
      v2f vert(float4 vertex : POSITION) { v2f o; o.position = UnityObjectToClipPos(vertex); o.direction = vertex.xyz; return o; }
      float3 cloudGroups(float2 p)
      {
        return tex2Dlod(_CloudGroups,float4(p/32,0,0)).rgb;
      }
      float density(float3 q, float height, float3 groups, float2 lod)
      {
        float vertical = saturate(1-abs(height-(groups.g-.5)*.65)/lerp(.85,1.3,groups.b));
        float shape = tex3Dlod(_CloudVolume,float4(q*.23,lod.x)).r;
        float3 rotated = float3(q.x*.36+q.z*.93,q.y,q.z*.36-q.x*.93);
        float detail = .5;
        if (_CloudQuality > 1) detail = tex3Dlod(_CloudVolume,float4(rotated*.61+float3(.31,.73,.19),lod.y)).r;
        float coverage = lerp(.275,.165,saturate(_Weather.x)) + (groups.r-.5)*.25;
        float body = shape - coverage - (1-vertical)*.38;
        return saturate((body+(detail-.5)*.23)*5)*smoothstep(0,.3,vertical);
      }
      float4 volume(float3 direction, float night)
      {
        float3 origin = (_WorldSpaceCameraPos-float3(0,_CloudBase,0))/240;
        float layer = direction.y > 0 ? 7 : 0;
        float dy = abs(direction.y) < .008 ? .008*(direction.y < 0 ? -1 : 1) : direction.y;
        float a = (layer-1.7-origin.y)/dy, b = (layer+1.7-origin.y)/dy;
        float start = max(0,min(a,b)), end = min(35,max(a,b));
        if (end <= start) return 0;
        const int steps = 240;
        float stride = (end-start)/steps;
        float2 lod = max(0,log2(stride*float2(.23,.61)*128));
        float3 drift = float3(_WeatherTime*.0009,0,_WeatherTime*.0003)/.23;
        float3 sun = normalize(float3(_CloudSun.x,max(_CloudSun.y,.2),_CloudSun.z));
        float forward = pow(saturate(dot(direction,sun)),8);
        float3 warm = lerp(float3(.96,.94,.9),float3(1,.83,.61),saturate(_Weather.z));
        warm = lerp(warm,float3(.66,.73,.81),_Weather.w*.6);
        float4 accumulated = 0;
        for (int step=0; step<240; step++) {
          if (step >= steps || accumulated.a > .995) break;
          float3 p = origin + direction*(start+(step+.5)*stride);
          float3 q = p+drift;
          // Nearby light samples share the slow macro field, not the billow density.
          float2 groupPosition = float2(q.x*.8+q.z*.6,q.z*.8-q.x*.6)*.16;
          float3 groups = cloudGroups(groupPosition);
          q += (cloudGroups(groupPosition*2.17+19.7)-.5)*2.6;
          float height = p.y-layer;
          float d = density(q,height,groups,lod);
          if (d > .002) {
            float shadow = _CloudQuality > 2
              ? density(q+sun*.08,height+sun.y*.08,groups,lod)*.65 + density(q+sun*.24,height+sun.y*.24,groups,lod)*.35
              : density(q+sun*.12,height+sun.y*.12,groups,lod);
            float light = saturate(.6+(d-shadow)*3)*exp(-shadow*.4);
            float3 ambient = lerp(float3(.46,.59,.73),float3(.67,.75,.85),saturate((p.y-layer)*.5+.5));
            float3 color = ambient*.45 + warm*(.18+light*.45+forward*.05);
            color = lerp(color,color*float3(.22,.32,.48),night);
            float alpha = 1-exp(-d*stride*4.5);
            accumulated.rgb += (1-accumulated.a)*alpha*color;
            accumulated.a += (1-accumulated.a)*alpha;
          }
        }
        float distanceFade = 1-smoothstep(22,35,start);
        return accumulated*distanceFade;
      }
      fixed4 atmosphere(v2f i)
      {
        float3 d = normalize(i.direction);
        fixed3 theme = _ThemeSky.rgb / max(_ThemeSky.w, .0001);
        float3 authoredPalette = tex2Dlod(_FrontTex,float4(.5,.5,0,8)).rgb;
        float night = saturate((.22-dot(authoredPalette,float3(.21,.71,.08)))*6)*(1-saturate(_ThemeSky.w));
        float3 palette = lerp(float3(.35,.56,.78),normalize(authoredPalette+.01)*.72,.45);
        palette = lerp(palette,theme,saturate(_ThemeSky.w));
        float horizon = pow(saturate(1-abs(d.y)),3);
        fixed3 original = lerp(palette,lerp(palette,float3(.89,.92,.95),.72),horizon);
        original *= lerp(float3(1,1,1),float3(.18,.27,.43),night);
        float warmth = _Weather.z;
        fixed3 tint = lerp(fixed3(.6,.75,.9),fixed3(1,.81,.59),saturate(warmth*.5+.5));
        fixed3 sky = lerp(original,original*tint*1.1,saturate(abs(warmth))*.65);
        sky = lerp(sky,fixed3(.43,.5,.55),_Weather.w*.6);
        if (_CloudQuality > 0) {
          float4 clouds = volume(d,night);
          sky = sky*(1-clouds.a) + clouds.rgb;
        }
        sky = lerp(sky,fixed3(.62,.7,.73),_Weather.y*.4);
        return fixed4(sky,1);
      }
    ENDCG
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.0
      fixed4 frag(v2f i) : SV_Target { return atmosphere(i); }
      ENDCG
    }
  }
}
