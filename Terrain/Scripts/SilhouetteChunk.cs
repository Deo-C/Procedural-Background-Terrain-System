using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SilhouetteChunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public float StartX { get; private set; }
    public float EndX { get; private set; }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(TerrainModeConfig config, float startX, int pointCount, float pointSpacing, float bottomY, bool isBack)
    {
        StartX = startX;
        EndX = startX + pointCount * pointSpacing;

        if (meshFilter == null || meshRenderer == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        float heightMult = isBack ? config.silhouetteBackHeightMultiplier : config.silhouetteMidHeightMultiplier;
        float noiseOffset = isBack ? config.silhouetteNoiseOffsetBack : config.silhouetteNoiseOffsetMid;
        Color color = isBack ? config.silhouetteColorBack : config.silhouetteColorMid;

        var generator = new TerrainGenerator(config, heightMult, noiseOffset);
        float[] heights = generator.GenerateHeightMap(startX, pointCount, pointSpacing);
        Vector2[] smoothed = generator.SmoothHeightMap(heights, pointSpacing);
        OffsetPoints(ref smoothed, startX);
        Mesh mesh = generator.BuildMesh(smoothed, bottomY, color);

        meshFilter.sharedMesh = mesh;
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = isBack ? -2 : -1;
        }
    }

    private static void OffsetPoints(ref Vector2[] points, float offset)
    {
        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            points[i].x += offset;
        }
    }

    public void ApplyOffset(float offsetX)
    {
        transform.localPosition = new Vector3(offsetX, transform.localPosition.y, transform.localPosition.z);
    }

    public void ReturnToPool()
    {
        gameObject.SetActive(false);
    }
}
