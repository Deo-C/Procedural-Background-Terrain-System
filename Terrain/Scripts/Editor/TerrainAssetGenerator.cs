#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TerrainAssetGenerator
{
    private const string ThemeDir = "Assets/Terrain/Configs/Themes";
    private const string ModeDir = "Assets/Terrain/Configs/Modes";
    private const string MaterialPath = "Assets/Terrain/Materials/TerrainMaterial.mat";
    private const string ShaderPath = "Assets/Terrain/Materials/VertexColorUnlit.shader";
    private const string PrefabPath = "Assets/Terrain/Prefabs/TerrainChunk.prefab";

    [MenuItem("Terrain/Generate Default Assets")]
    public static void Generate()
    {
        EnsureFolders();

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError("VertexColorUnlit shader missing. Ensure it exists at " + ShaderPath);
            return;
        }

        Material terrainMat = EnsureMaterial(shader);

        ColorThemeConfig dagTemasi = CreateTheme("DagTemasi", "#C8DCF0", "#A0BEDC", "#6B8C6B", "#2D3A2D");
        ColorThemeConfig colTemasi = CreateTheme("ColTemasi", "#F5C87A", "#E87840", "#C8943C", "#4A2C1A");
        ColorThemeConfig okyanusTemasi = CreateTheme("OkyanusTemasi", "#A8D4F0", "#5090C8", "#D4C08C", "#1A3C5A");

        CreateMode("DagModu", 0.08f, 4.5f, 4, 0.5f, 2.0f, -1f, -2f, 5f, dagTemasi, 3, new float[] { 0.1f, 0.4f, 1.0f });
        CreateMode("ColModu", 0.05f, 1.5f, 2, 0.4f, 1.8f, -1.5f, -2f, 1.5f, colTemasi, 2, new float[] { 0.15f, 1.0f });
        CreateMode("OkyansuModu", 0.2f, 0.8f, 3, 0.6f, 2.2f, -0.5f, -1f, 1f, okyanusTemasi, 3, new float[] { 0.05f, 0.3f, 1.0f });

        CreateChunkPrefab(terrainMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Terrain default assets generated/updated.");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(ThemeDir);
        Directory.CreateDirectory(ModeDir);
        Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
    }

    private static Material EnsureMaterial(Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader) { color = Color.white };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
            mat.color = Color.white;
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    private static ColorThemeConfig CreateTheme(string assetName, string skyTop, string skyBottom, string terrainTop, string terrainBottom)
    {
        string path = Path.Combine(ThemeDir, assetName + ".asset");
        ColorThemeConfig asset = AssetDatabase.LoadAssetAtPath<ColorThemeConfig>(path);
        bool isNew = asset == null;
        if (isNew)
        {
            asset = ScriptableObject.CreateInstance<ColorThemeConfig>();
        }

        asset.skyColorTop = HexToColor(skyTop);
        asset.skyColorBottom = HexToColor(skyBottom);
        asset.terrainColorTop = HexToColor(terrainTop);
        asset.terrainColorBottom = HexToColor(terrainBottom);
        asset.accentColors = new Color[0];

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        return asset;
    }

    private static TerrainModeConfig CreateMode(
        string assetName,
        float noiseScale,
        float amplitude,
        int octaves,
        float persistence,
        float lacunarity,
        float baselineY,
        float minY,
        float maxY,
        ColorThemeConfig theme,
        int parallaxLayers,
        float[] speedRatios)
    {
        string path = Path.Combine(ModeDir, assetName + ".asset");
        TerrainModeConfig asset = AssetDatabase.LoadAssetAtPath<TerrainModeConfig>(path);
        bool isNew = asset == null;
        if (isNew)
        {
            asset = ScriptableObject.CreateInstance<TerrainModeConfig>();
        }

        asset.modeName = assetName;
        asset.noiseScale = noiseScale;
        asset.amplitude = amplitude;
        asset.octaves = octaves;
        asset.persistence = persistence;
        asset.lacunarity = lacunarity;
        asset.baselineY = baselineY;
        asset.minY = minY;
        asset.maxY = maxY;
        asset.colorTheme = theme;
        asset.parallaxLayerCount = parallaxLayers;
        asset.parallaxSpeedRatios = speedRatios;
        asset.noiseSeed = 0f;

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        return asset;
    }

    private static void CreateChunkPrefab(Material material)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            GameObject go = new GameObject("TerrainChunk");
            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            go.AddComponent<TerrainChunk>();

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            int backgroundLayer = LayerMask.NameToLayer("Background");
            if (backgroundLayer >= 0)
            {
                go.layer = backgroundLayer;
            }

            if (SortingLayer.NameToID("Background") != 0)
            {
                renderer.sortingLayerName = "Background";
            }
            renderer.sortingOrder = 0;

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
        }
        else
        {
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    private static Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.white;
    }
}
#endif
