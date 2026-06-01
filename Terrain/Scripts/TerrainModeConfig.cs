using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Mode Config", fileName = "TerrainModeConfig")]
public class TerrainModeConfig : ScriptableObject
{
    [Header("Meta")]
    public string modeName;
    public Sprite previewSprite;

    [Header("Perlin Noise")]
    [Range(0.01f, 1f)] public float noiseScale = 0.1f;
    [Range(0.5f, 10f)] public float amplitude = 3f;
    [Range(1, 6)] public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Range(1f, 4f)] public float lacunarity = 2f;
    public float noiseSeed = 0f;

    [Header("Zemin Profili")]
    public float baselineY = 0f;
    public float minY = -2f;
    public float maxY = 4f;

    [Header("Görsel")]
    public ColorThemeConfig colorTheme;

    [Header("Parallax")]
    [Range(1, 5)] public int parallaxLayerCount = 3;
    [Range(0f, 1f)] public float[] parallaxSpeedRatios;

    [Header("Arka Plan Siluet Katmanları")]
    public Color silhouetteColorBack = new Color(0.2f, 0.2f, 0.3f);
    public Color silhouetteColorMid = new Color(0.35f, 0.35f, 0.45f);
    public float silhouetteBackSpeedRatio = 0.15f;
    public float silhouetteMidSpeedRatio = 0.40f;
    public float silhouetteBackHeightMultiplier = 0.7f;
    public float silhouetteMidHeightMultiplier = 0.85f;
    public float silhouetteNoiseOffsetBack = 300f;
    public float silhouetteNoiseOffsetMid = 600f;

    [Header("Detay Konfigürasyonu")]
    public ScriptableObject detailConfig;

    private void OnValidate()
    {
        if (parallaxLayerCount < 1)
        {
            parallaxLayerCount = 1;
        }

        if (parallaxSpeedRatios == null || parallaxSpeedRatios.Length != parallaxLayerCount)
        {
            float[] resized = new float[parallaxLayerCount];
            if (parallaxSpeedRatios != null)
            {
                for (int i = 0; i < Mathf.Min(resized.Length, parallaxSpeedRatios.Length); i++)
                {
                    resized[i] = parallaxSpeedRatios[i];
                }
            }
            parallaxSpeedRatios = resized;
        }
    }
}
