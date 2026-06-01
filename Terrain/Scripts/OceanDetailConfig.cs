using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Ocean Detail Config", fileName = "OceanDetailConfig")]
public class OceanDetailConfig : ScriptableObject
{
    [Header("Tekne")]
    public float minBoatSpacing = 30f;
    public float maxBoatSpacing = 55f;
    public float boatWidth = 2.5f;
    public float boatHeight = 0.6f;
    public float mastHeight = 2.2f;
    public float sailWidth = 1.2f;
    public Color boatHullColor = new Color(0.55f, 0.30f, 0.15f);
    public Color boatDeckColor = new Color(0.70f, 0.45f, 0.22f);
    public Color sailColor = new Color(0.95f, 0.92f, 0.82f);
    public Color sailShadowColor = new Color(0.80f, 0.76f, 0.65f);

    [Header("Martý")]
    public int seagullsPerBoat_Min = 4;
    public int seagullsPerBoat_Max = 5;
    public int randomSeagulls_Min = 2;
    public int randomSeagulls_Max = 3;
    public float seagullSpreadRadius = 3f;
    public float seagullMinHeight = 0.5f;
    public float seagullMaxHeight = 3.5f;
    public float seagullWingSpan = 0.5f;
    public Color seagullColor = new Color(0.92f, 0.92f, 0.90f);
    public float seagullAnimSpeed = 1.2f;

    [Header("Mercan")]
    [Range(0f, 1f)] public float coralMaxHeightRatio = 0.25f;
    public int minCoralsPerCluster = 2;
    public int maxCoralsPerCluster = 5;
    public float minCoralSpacing = 6f;
    public float maxCoralSpacing = 18f;
    public float coralMinHeight = 0.3f;
    public float coralMaxHeight = 1.0f;
    public Color[] coralColors = new Color[]
    {
        new Color(0.90f, 0.35f, 0.35f),
        new Color(0.95f, 0.65f, 0.20f),
        new Color(0.85f, 0.25f, 0.55f),
        new Color(0.40f, 0.75f, 0.65f),
    };
}
