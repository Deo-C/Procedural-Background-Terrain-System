using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Mountain Detail Config", fileName = "MountainDetailConfig")]
public class MountainDetailConfig : ScriptableObject
{
    [Header("Kar")]
    [Range(0f, 1f)] public float snowHeightThreshold = 0.80f; // maxY'nin bu oraný üzeri kar alýr
    public Color snowColor = new Color(0.95f, 0.97f, 1f);
    public float snowCapThickness = 0.3f;

    [Header("Buz")]
    [Range(0f, 1f)] public float iceHeightThreshold = 0.65f; // snow ile ice arasýndaki bölge
    public Color iceColor = new Color(0.75f, 0.88f, 0.98f, 0.85f);
    public float iceThickness = 0.15f;

    [Header("Çam Aðacý")]
    [Range(0f, 1f)] public float treeMinHeightRatio = 0.25f;
    [Range(0f, 1f)] public float treeMaxHeightRatio = 0.60f;
    public int minTreesPerCluster = 3;
    public int maxTreesPerCluster = 7;
    public float minClusterSpacing = 8f;
    public float maxClusterSpacing = 20f;
    public float treeMinHeight = 0.8f;
    public float treeMaxHeight = 1.8f;
    public Color treeTrunkColor = new Color(0.35f, 0.22f, 0.12f);
    public Color treeLeafColor = new Color(0.15f, 0.40f, 0.18f);
    public Color treeLeafColorVariant = new Color(0.10f, 0.30f, 0.12f);
    [Range(0f, 1f)] public float clusterDensityVariance = 0.4f;
}
