// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#ifndef BLINN_PHONG_NORMAL_EMISSION_INCLUDED
#define BLINN_PHONG_NORMAL_EMISSION_INCLUDED

#ifdef USE_SPHERE

float2 GetSphereUV(float3 r)
{
  // Magnitude of the intermediate vector (reflection plus camera direction).
  float m = sqrt(r.x * r.x + r.y * r.y + (r.z + 1.0) * (r.z + 1.0)); 
  // Normalize the intermediate vector.
  float3 n = float3(r.x / m, r.y / m, r.z / m);
  // Map vertex coordinates from [-1, 1] to UV coordinates [0, 1].
  // UVs near the edge sample farther inside the texture, producing a reflective glass-ball effect.
  return float2(0.5 * n.x + 0.5,0.5 * n.y + 0.5);
}

#endif

struct VertexInput
{
  float4 vertex : POSITION;
  float3 normal : NORMAL;
  float2 uv : TEXCOORD0;
  float2 texcoord1 : TEXCOORD1;
};

struct VertexOutputBase
{
  float2 uv : TEXCOORD0;
  float3 worldNormal : NORMAL;
  float3 worldPos: TEXCOORD1;
  float4 pos : SV_POSITION;
  UNITY_FOG_COORDS(2)
  LIGHTING_COORDS(3, 4)
  #ifdef LIGHTPROBE_SH
  fixed3 SHLighting : COLOR;
  #endif 
  #ifdef USE_SPHERE
  float2 uv2 : TEXCOORD5;
  #endif
};

sampler2D _MainTex;
float4 _MainTex_ST;

#ifdef USE_SPHERE_REFLECTION
sampler2D _ReflectionTex;
float4 _ReflectionTex_ST;
#endif
      
// Variables declared in Properties.
fixed4 _Color;
fixed4 _AmbientColor;
fixed4 _Emission;
float _Gloss;

VertexOutputBase vertForwardAdd (VertexInput v) 
{
  VertexOutputBase o;
  UNITY_INITIALIZE_OUTPUT(VertexOutputBase, o);
  
  o.pos = UnityObjectToClipPos(v.vertex);
  o.uv = TRANSFORM_TEX(v.uv, _MainTex);
  o.worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos = mul(unity_ObjectToWorld, v.vertex);

  #ifdef USE_SPHERE
  float3 posEyeSpace = mul(UNITY_MATRIX_MV,v.vertex).xyz;
  float3 I = posEyeSpace - float3(0,0,0);
  float3 N = mul((float3x3)UNITY_MATRIX_MV,v.normal);
  N = normalize(N);
  float3 R = reflect(I,N);
  o.uv2 = GetSphereUV(R);
  #endif

  return o;
}
half4 fragForwardAdd (VertexOutputBase i) : SV_Target // backward compatibility (this used to be the fragment entry function)
{
  #ifdef USE_SPHERE_MAP
  fixed4 albedo = tex2D(_MainTex, i.uv2) * _Color;
  #else
  // sample the texture
  fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
  #endif

  // Spherical environment map.
  #ifdef USE_SPHERE_REFLECTION
  albedo *= tex2D(_ReflectionTex, i.uv2);
  #endif 

  float3 worldLight = normalize(UnityWorldSpaceLightDir(i.worldPos.xyz));
  float3 worldView = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));
  fixed3 worldNormal = normalize(i.worldNormal);

  fixed3 diffuse = _LightColor0.rgb *  albedo.rgb * saturate(dot(worldNormal, worldLight));

  UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos.xyz);

  #ifdef NO_SPECULAR
  fixed3 color = diffuse * atten * _LightColor0.rgb;
  #else
  fixed3 halfDir = normalize(worldLight + worldView);
  fixed3 specular = _LightColor0.rgb * _SpecColor.rgb * pow(saturate(dot(halfDir, worldNormal)), _Gloss * 0.5);
  fixed3 color = (diffuse + specular) * atten * _LightColor0.rgb;
  #endif
  
  #ifdef USE_TRANSPARENT
  return fixed4(color, albedo.a);
  #else
  return fixed4(color, 1.0);
  #endif
}

VertexOutputBase vertForwardBase (VertexInput v) 
{
  VertexOutputBase o;
  UNITY_INITIALIZE_OUTPUT(VertexOutputBase, o);

  o.pos = UnityObjectToClipPos(v.vertex);
  o.uv = TRANSFORM_TEX(v.uv, _MainTex);
  // Transform the normal to world space.
  o.worldNormal = UnityObjectToWorldNormal(v.normal);
  // Transform the vertex position to world space.
  o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  
  #ifdef LIGHTPROBE_SH
  o.SHLighting = ShadeSH9(float4(o.worldNormal, 1));
  #endif

  #ifdef USE_SPHERE
  // Transform the vertex into camera space.
  float3 posEyeSpace = mul(UNITY_MATRIX_MV,v.vertex).xyz;
  // Get the incident view vector.
  float3 I = posEyeSpace - float3(0,0,0);
  // Transform the vertex normal into camera space.
  float3 N = mul((float3x3)UNITY_MATRIX_MV,v.normal);
  // Normalize the normal.
  N = normalize(N);
  // Calculate the reflected vector from the incident vector and normal.
  float3 R = reflect(I,N);
  // Calculate the final UVs from the reflected vector.
  o.uv2 = GetSphereUV(R);
  #endif

  UNITY_TRANSFER_LIGHTING(o,o.pos)
  UNITY_TRANSFER_FOG(o,o.pos);
  return o;
}

half4 fragForwardBase (VertexOutputBase i) : SV_Target  // backward compatibility (this used to be the fragment entry function)
{
  #ifdef USE_SPHERE_MAP
  fixed4 albedo = tex2D(_MainTex, i.uv2) * _Color;
  #else
  // sample the texture
  fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
  #endif

  // Spherical environment map.
  #ifdef USE_SPHERE_REFLECTION
  albedo *= tex2D(_ReflectionTex, i.uv2);
  #endif

  // Ambient light.
  fixed3 ambient = (UNITY_LIGHTMODEL_AMBIENT.xyz + _AmbientColor) * albedo;
  
  #ifdef LIGHTPROBE_SH
  ambient.rgb *= i.SHLighting;
  #endif

  // Light direction in world space.
  float3 worldLight = normalize(UnityWorldSpaceLightDir(i.worldPos.xyz));
  float3 worldView = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));
  // Normalize again after interpolation.
  fixed3 worldNormal = normalize(i.worldNormal);
  // Calculate diffuse lighting.
  fixed3 diffuse = _LightColor0.rgb * albedo.rgb * saturate(dot(worldNormal, worldLight));

  #ifdef FLAT_EMISSION
  fixed3 emission = _Emission.rgb * _Emission.a;
  #else
  fixed3 emission = _Emission.rgb * albedo.rgb * _Emission.a;
  #endif

  // Lighting.
  UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos.xyz);

  #ifdef NO_SPECULAR
  fixed3 color = max(diffuse, emission) * atten + ambient;
  #else
  // Calculate the half vector from the light and view directions.
  fixed3 halfDir = normalize(worldLight + worldView);
  // Calculate Blinn-Phong specular lighting.
  fixed3 specular = _LightColor0.rgb * _SpecColor.rgb * pow(saturate(dot(halfDir, worldNormal)), _Gloss);
  // Combine diffuse, ambient, and specular lighting.
  fixed3 color = (max(diffuse, emission) + specular) * atten + ambient;
  #endif

  // apply fog
  UNITY_APPLY_FOG(i.fogCoord, color);

  #ifdef USE_TRANSPARENT
  return fixed4(color, albedo.a);
  #else
  return fixed4(color, 1.0);
  #endif
}

#endif // BLINN_PHONG_NORMAL_EMISSION_INCLUDED