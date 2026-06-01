using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Color Theme", fileName = "ColorThemeConfig")]
public class ColorThemeConfig : ScriptableObject
{
    [Header("Sky")]
    public Color skyColorTop;
    public Color skyColorBottom;

    [Header("Terrain")]
    public Color terrainColorTop;
    public Color terrainColorBottom;

    [Header("Accents")]
    public Color[] accentColors = new Color[0];
}
