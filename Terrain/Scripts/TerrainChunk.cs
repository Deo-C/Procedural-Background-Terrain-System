using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private TerrainModeConfig config;
    private float startX;
    private float chunkWidth;
    private float pointSpacing;
    private int pointCount;
    private readonly List<GameObject> spawnedDetails = new List<GameObject>();

    private System.Action<TerrainChunk> onReturn;

    public float ChunkWidth => chunkWidth;
    public float StartX => startX;
    public float EndX => startX + chunkWidth;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetReturnAction(System.Action<TerrainChunk> returnAction)
    {
        onReturn = returnAction;
    }

    public void Initialize(TerrainModeConfig config, float startX, int pointCount, float pointSpacing, float bottomY)
    {
        CleanupSpawnedDetails();

        if (config == null)
        {
            Debug.LogWarning("TerrainChunk initialized with null config");
            return;
        }

        if (meshFilter == null || meshRenderer == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        this.config = config;
        this.startX = startX;
        this.pointCount = Mathf.Max(2, pointCount);
        this.pointSpacing = Mathf.Max(0.01f, pointSpacing);
        chunkWidth = (this.pointCount - 1) * this.pointSpacing;

        TerrainGenerator generator = new TerrainGenerator(config);
        float[] heights = generator.GenerateHeightMap(startX, this.pointCount, this.pointSpacing);
        Vector2[] topPoints = generator.SmoothHeightMap(heights, this.pointSpacing);
        OffsetPoints(ref topPoints, startX);
        Color terrainColor = config.colorTheme != null ? config.colorTheme.terrainColorTop : Color.white;
        Mesh mesh = generator.BuildMesh(topPoints, bottomY, terrainColor);

        mesh.name = $"TerrainChunk_{startX:F2}";
        meshFilter.sharedMesh = mesh;

        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 0;
        }

        int backgroundLayer = LayerMask.NameToLayer("Background");
        Material terrainMat = meshRenderer != null ? meshRenderer.sharedMaterial : null;

        if (config.detailConfig is MountainDetailConfig mountainConfig)
        {
            MountainDetailGenerator detailGenerator = new MountainDetailGenerator();
            List<GameObject> details = detailGenerator.Generate(mountainConfig, config, topPoints, transform, terrainMat);
            spawnedDetails.AddRange(details);
        }
        else if (config.detailConfig is OceanDetailConfig oceanConfig)
        {
            OceanDetailGenerator detailGenerator = new OceanDetailGenerator();

            List<GameObject> corals = detailGenerator.GenerateCorals(topPoints, oceanConfig, config.minY + config.baselineY, config.maxY + config.baselineY, config.noiseSeed, transform, terrainMat, 1, backgroundLayer);
            spawnedDetails.AddRange(corals);

            List<GameObject> boats = detailGenerator.GenerateBoats(topPoints, oceanConfig, config.noiseSeed, transform, terrainMat, 2, backgroundLayer);
            spawnedDetails.AddRange(boats);

            List<GameObject> seagulls = detailGenerator.GenerateSeagulls(boats, topPoints, oceanConfig, config.noiseSeed, transform, terrainMat, 3, backgroundLayer);
            spawnedDetails.AddRange(seagulls);
        }
    }

    private static void OffsetPoints(ref Vector2[] points, float offset)
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i].x += offset;
        }
    }

    public void ReturnToPool()
    {
        CleanupSpawnedDetails();
        onReturn?.Invoke(this);
    }

    private void CleanupSpawnedDetails()
    {
        if (spawnedDetails.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spawnedDetails.Count; i++)
        {
            if (spawnedDetails[i] != null)
            {
                Destroy(spawnedDetails[i]);
            }
        }

        spawnedDetails.Clear();
    }
}
