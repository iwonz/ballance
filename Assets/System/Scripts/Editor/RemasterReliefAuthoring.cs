using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor
{
  public static class RemasterReliefAuthoring
  {
    private const string Folder = "Assets/Game/Remaster/Resources/Remaster/";
    public static readonly string[] Families = { "Sandstone", "Oak", "Granite", "Paper", "Concrete", "Metal", "Brass", "Rope", "Rubber", "Snow", "Ice", "Sand", "Moss", "Glass" };
    private const int Size = 1024;

    public static void Generate()
    {
      foreach (string family in Families) GenerateFamily(family);
      AssetDatabase.Refresh();
      foreach (string family in Families) {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + family + "Relief.png");
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false; importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false; importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8; importer.maxTextureSize = Size;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
      }
      foreach (string map in new[] { "GlassNormal", "GlassMask" }) {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder+map+".png");
        importer.textureType = map == "GlassNormal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true; importer.anisoLevel = 8; importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
      }
      GenerateCloudNoise();
      RemasterMaterialAuthoring.Apply();
      Debug.Log("REMASTER_RELIEF_AUTHORED: fourteen periodic normal-XY/height/roughness maps, Standard glass inputs and a 3D cloud density volume.");
    }

    private static float Hash(int x, int y, int z = 0)
    {
      unchecked {
        uint h = (uint)(x * 374761393 + y * 668265263 + z * 1442695041 + 13579);
        h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
        return (h & 0xffffff) / 16777215f;
      }
    }

    private static float Noise(float u, float v, int cells, int seed = 0)
    {
      float x = u * cells, y = v * cells;
      int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
      float fx = Mathf.SmoothStep(0, 1, x - ix), fy = Mathf.SmoothStep(0, 1, y - iy);
      return Mathf.Lerp(Mathf.Lerp(Hash(ix % cells, iy % cells,seed), Hash((ix+1) % cells, iy % cells,seed), fx),
        Mathf.Lerp(Hash(ix % cells, (iy+1) % cells,seed), Hash((ix+1) % cells, (iy+1) % cells,seed), fx), fy);
    }

    private static void GenerateFamily(string family)
    {
      Texture2D source = null;
      if (File.Exists(Folder + family + ".png")) {
        source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(Folder + family + ".png"));
        source.wrapMode = TextureWrapMode.Repeat;
      }
      var height = new float[Size * Size]; var roughness = new float[height.Length];
      var output = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
      try {
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) {
          float u = (float)x / (Size-1), v = (float)y / (Size-1);
          float broad = Noise(u, v, 8), grain = Noise(u, v, 96), fine = Noise(u, v, 256);
          float color = source == null ? .5f : source.GetPixelBilinear(u, v).grayscale;
          // Color detail guides the synthetic relief; it is not a measured depth reconstruction.
          float edge = Mathf.SmoothStep(0, .045f, Mathf.Min(Mathf.Min(u, 1-u), Mathf.Min(v, 1-v)));
          color = Mathf.Lerp(.5f, color, edge);
          float h = .5f, r = .7f;
          switch (family) {
            case "Sandstone": h = .35f + color*.22f + grain*.25f + fine*.08f; r = .7f + fine*.18f; break;
            case "Concrete": h = .4f + color*.15f + grain*.22f + fine*.08f; r = .76f + fine*.15f; break;
            case "Granite": h = .4f + color*.15f + grain*.13f + fine*.1f; r = .5f + grain*.28f; break;
            case "Oak": h = .42f + color*.17f + Mathf.Sin(u*96*Mathf.PI + broad*4)*.012f + fine*.025f; r = .4f + grain*.18f; break;
            case "Paper": h = .4f + color*.1f + broad*.15f + fine*.12f; r = .83f + fine*.13f; break;
            case "Snow": h = .28f + broad*.25f + grain*.24f + fine*.12f; r = .67f + fine*.25f; break;
            case "Ice": h = .5f + (color-.65f)*.3f + broad*.045f; r = .08f + Mathf.Abs(color-.65f)*.6f; break;
            case "Sand": h = .35f + broad*.12f + grain*.25f + fine*.2f; r = .8f + fine*.17f; break;
            case "Moss": h = .25f + color*.18f + grain*.32f + fine*.15f; r = .72f + grain*.23f; break;
            case "Rope":
              h = .45f + Mathf.Cos((u*12+v*4)*Mathf.PI*2)*.22f + Mathf.Cos((u*48-v*16)*Mathf.PI*2)*.04f + fine*.04f;
              r = .8f + fine*.15f; break;
            case "Rubber": h = .48f + grain*.03f + fine*.07f; r = .68f + grain*.15f; break;
            case "Glass": h = .5f + broad*.025f + fine*.001f; r = .035f + fine*.01f; break;
            default:
              float scratches = Mathf.Pow(.5f + .5f*Mathf.Sin(u*160*Mathf.PI + broad*2), 16);
              h = .52f - scratches*.055f + fine*.016f;
              r = (family == "Brass" ? .28f : .2f) + scratches*.16f + grain*.12f; break;
          }
          int index = y*Size+x; height[index] = Mathf.Clamp01(h); roughness[index] = Mathf.Clamp01(r);
        }
        for (int i = 0; i < Size; i++) {
          height[i*Size+Size-1] = height[i*Size]; roughness[i*Size+Size-1] = roughness[i*Size];
          height[(Size-1)*Size+i] = height[i]; roughness[(Size-1)*Size+i] = roughness[i];
        }
        var pixels = new Color32[height.Length];
        float strength = family == "Glass" ? .5f : family == "Metal" || family == "Brass" || family == "Ice" ? 2 : family == "Snow" || family == "Sand" || family == "Moss" ? 6 : 3;
        float min = 1, max = 0;
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) {
          int index = y*Size+x;
          int l = x == 0 ? Size-2 : x-1, r = x == Size-1 ? 1 : x+1;
          int b = y == 0 ? Size-2 : y-1, t = y == Size-1 ? 1 : y+1;
          Vector3 normal = new Vector3((height[y*Size+l]-height[y*Size+r])*strength, (height[b*Size+x]-height[t*Size+x])*strength, 1).normalized;
          pixels[index] = new Color(normal.x*.5f+.5f, normal.y*.5f+.5f, height[index], roughness[index]);
          min = Mathf.Min(min, height[index]); max = Mathf.Max(max, height[index]);
        }
        if (max-min < .02f) throw new InvalidOperationException("Flat material relief: " + family);
        for (int i = 0; i < Size; i++) {
          if (!pixels[i*Size].Equals(pixels[i*Size+Size-1]) || !pixels[i].Equals(pixels[(Size-1)*Size+i]))
            throw new InvalidOperationException("Nonperiodic relief: " + family);
        }
        output.SetPixels32(pixels); output.Apply();
        File.WriteAllBytes(Folder + family + "Relief.png", output.EncodeToPNG());
        if (family == "Glass") {
          for (int i=0; i<pixels.Length; i++) {
            Color encoded = pixels[i]; float nx = encoded.r*2-1, ny = encoded.g*2-1;
            pixels[i] = new Color(encoded.r,encoded.g,Mathf.Sqrt(Mathf.Max(0,1-nx*nx-ny*ny))*.5f+.5f,1);
          }
          output.SetPixels32(pixels); output.Apply(); File.WriteAllBytes(Folder+"GlassNormal.png",output.EncodeToPNG());
          for (int i=0; i<pixels.Length; i++) pixels[i] = new Color(0,height[i],0,1-roughness[i]);
          output.SetPixels32(pixels); output.Apply(); File.WriteAllBytes(Folder+"GlassMask.png",output.EncodeToPNG());
        }
      } finally {
        UnityEngine.Object.DestroyImmediate(output);
        if (source != null) UnityEngine.Object.DestroyImmediate(source);
      }
    }

    private static float Noise3D(Vector3 p, int cells)
    {
      p *= cells;
      int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
      float a = Mathf.SmoothStep(0, 1, p.x-x), b = Mathf.SmoothStep(0, 1, p.y-y), c = Mathf.SmoothStep(0, 1, p.z-z);
      float low = Mathf.Lerp(Mathf.Lerp(Hash(x%cells,y%cells,z%cells),Hash((x+1)%cells,y%cells,z%cells),a),
        Mathf.Lerp(Hash(x%cells,(y+1)%cells,z%cells),Hash((x+1)%cells,(y+1)%cells,z%cells),a),b);
      float high = Mathf.Lerp(Mathf.Lerp(Hash(x%cells,y%cells,(z+1)%cells),Hash((x+1)%cells,y%cells,(z+1)%cells),a),
        Mathf.Lerp(Hash(x%cells,(y+1)%cells,(z+1)%cells),Hash((x+1)%cells,(y+1)%cells,(z+1)%cells),a),b);
      return Mathf.Lerp(low,high,c);
    }

    private static float Billows(Vector3 p)
    {
      const int cells = 8;
      p *= cells;
      int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
      float nearest = 10;
      for (int dz=-1; dz<=1; dz++) for (int dy=-1; dy<=1; dy++) for (int dx=-1; dx<=1; dx++) {
        int a = (x+dx+cells)%cells, b = (y+dy+cells)%cells, c = (z+dz+cells)%cells;
        var center = new Vector3(x+dx+Hash(a,b,c),y+dy+Hash(a+11,b+17,c+23),z+dz+Hash(a+31,b+7,c+13));
        float radius = .72f + Hash(a+47,b+29,c+59)*.56f;
        nearest = Mathf.Min(nearest,(center-p).sqrMagnitude/(radius*radius));
      }
      return Mathf.Clamp01(1-Mathf.Sqrt(nearest));
    }

    public static void GenerateCloudNoise()
    {
      const int size = 128;
      string path = Folder + "CloudDensity.asset";
      var volume = new Texture3D(size,size,size,TextureFormat.R8,true) {
        name = "Periodic cloud density", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear
      };
      var pixels = new byte[size*size*size];
      for (int z=0; z<size; z++) for (int y=0; y<size; y++) for (int x=0; x<size; x++) {
        var p = new Vector3((float)x/size,(float)y/size,(float)z/size);
        float shape = Billows(p)*.8f + Noise3D(p,16)*.15f + Noise3D(p,32)*.05f;
        pixels[(z*size+y)*size+x] = (byte)Mathf.RoundToInt(Mathf.Clamp01(shape)*255);
      }
      volume.SetPixelData(pixels,0); volume.Apply();
      var previous = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
      if (previous == null) AssetDatabase.CreateAsset(volume,path);
      else { EditorUtility.CopySerialized(volume,previous); UnityEngine.Object.DestroyImmediate(volume); EditorUtility.SetDirty(previous); }
      const int groupSize = 256;
      var groups = new Texture2D(groupSize,groupSize,TextureFormat.RGBA32,true,true) {
        name = "Cloud groups", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear
      };
      var groupPixels = new Color32[groupSize*groupSize];
      for (int y=0; y<groupSize; y++) for (int x=0; x<groupSize; x++) {
        float u = (float)x/groupSize, v = (float)y/groupSize;
        groupPixels[y*groupSize+x] = new Color(Noise(u,v,32,71),Noise(u,v,32,131),Noise(u,v,32,239),1);
      }
      groups.SetPixels32(groupPixels); groups.Apply();
      string groupPath = Folder+"CloudGroups.asset";
      var previousGroups = AssetDatabase.LoadAssetAtPath<Texture2D>(groupPath);
      if (previousGroups == null) AssetDatabase.CreateAsset(groups,groupPath);
      else { EditorUtility.CopySerialized(groups,previousGroups); UnityEngine.Object.DestroyImmediate(groups); EditorUtility.SetDirty(previousGroups); }
      AssetDatabase.SaveAssets();
    }
  }
}
