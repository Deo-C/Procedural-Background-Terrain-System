using System.Collections.Generic;
using UnityEngine;

public class ChunkSystem : MonoBehaviour
{
    [SerializeField] private TerrainChunk chunkPrefab;
    [SerializeField] private Transform chunksParent;
    [SerializeField] private int chunksAhead = 2;
    [SerializeField] private int chunksBehind = 1;
    [SerializeField] private float chunkWidth = 20f;
    [SerializeField] private int pointsPerChunk = 50;
    [SerializeField] private float bottomY = -20f;

    private ChunkPool chunkPool;
    private readonly List<TerrainChunk> activeChunks = new List<TerrainChunk>();
    private TerrainModeConfig currentConfig;
    private Camera cam;
    private float pointSpacing;

    public float ChunkWidth => chunkWidth;
    public int PointsPerChunk => pointsPerChunk;
    public float BottomY => bottomY;

    public void Initialize(TerrainModeConfig config, Camera cam)
    {
        this.cam = cam != null ? cam : Camera.main;
        currentConfig = config;
        pointSpacing = chunkWidth / Mathf.Max(1, pointsPerChunk - 1);

        if (chunkPool == null)
        {
            int initial = Mathf.Max(4, chunksAhead + chunksBehind + 2);
            chunkPool = new ChunkPool(chunkPrefab, chunksParent, initial);
        }

        ResetChunks();
    }

    private void ResetChunks()
    {
        ReturnAllChunks();

        if (currentConfig == null || cam == null)
        {
            return;
        }

        float firstStart = Mathf.Floor((cam.transform.position.x - chunkWidth * chunksBehind) / chunkWidth) * chunkWidth;
        int total = chunksAhead + chunksBehind + 1;

        float start = firstStart;
        for (int i = 0; i < total; i++)
        {
            SpawnChunk(start);
            start += chunkWidth;
        }
    }

    private void SpawnChunk(float startX)
    {
        TerrainChunk chunk = chunkPool.Get();
        chunk.Initialize(currentConfig, startX, pointsPerChunk, pointSpacing, bottomY);
        activeChunks.Add(chunk);
    }

    private void ReturnAllChunks()
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            activeChunks[i].ReturnToPool();
        }
        activeChunks.Clear();
    }

    private void LateUpdate()
    {
        if (currentConfig == null || cam == null || activeChunks.Count == 0)
        {
            return;
        }

        float cameraRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;
        float cameraLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        TerrainChunk last = activeChunks[activeChunks.Count - 1];
        while (last.EndX < cameraRight + chunkWidth * chunksAhead)
        {
            float newStart = last.EndX;
            SpawnChunk(newStart);
            last = activeChunks[activeChunks.Count - 1];
        }

        TerrainChunk first = activeChunks[0];
        while (first.EndX < cameraLeft - chunkWidth * chunksBehind)
        {
            first.ReturnToPool();
            activeChunks.RemoveAt(0);
            if (activeChunks.Count == 0)
            {
                break;
            }
            first = activeChunks[0];
        }
    }

    public void SwitchMode(TerrainModeConfig newConfig)
    {
        currentConfig = newConfig;
        ResetChunks();
    }
}
