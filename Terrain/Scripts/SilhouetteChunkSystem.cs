using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SilhouetteChunkSystem : MonoBehaviour
{
    [SerializeField] private SilhouetteChunk silhouettePrefab;
    [SerializeField] private Transform chunksParent;
    [SerializeField] private int chunksAhead = 3;
    [SerializeField] private int chunksBehind = 1;

    private ObjectPool<SilhouetteChunk> backPool;
    private ObjectPool<SilhouetteChunk> midPool;
    private readonly List<SilhouetteChunk> activeBack = new List<SilhouetteChunk>();
    private readonly List<SilhouetteChunk> activeMid = new List<SilhouetteChunk>();
    private TerrainModeConfig activeConfig;
    private Camera cam;
    private float chunkWidth;
    private int pointsPerChunk;
    private float bottomY;
    private float pointSpacing;
    private float lastCamX;
    private float backOffset;
    private float midOffset;
    private Transform backRoot;
    private Transform midRoot;

    public void Initialize(TerrainModeConfig config, Camera camera, float chunkWidth, int pointsPerChunk, float bottomY)
    {
        if (silhouettePrefab == null)
        {
            Debug.LogWarning("SilhouetteChunkSystem: Silhouette prefab is missing");
            return;
        }

        activeConfig = config;
        cam = camera != null ? camera : Camera.main;
        this.chunkWidth = chunkWidth;
        this.pointsPerChunk = Mathf.Max(2, pointsPerChunk);
        this.bottomY = bottomY;
        pointSpacing = chunkWidth / Mathf.Max(1, this.pointsPerChunk - 1);
        lastCamX = cam != null ? cam.transform.position.x : 0f;
        backOffset = 0f;
        midOffset = 0f;

        EnsureRoots();
        EnsurePools();

        ResetLayer(activeBack, backPool, true, ref backOffset, backRoot);
        ResetLayer(activeMid, midPool, false, ref midOffset, midRoot);
    }

    private void EnsureRoots()
    {
        Transform parent = chunksParent != null ? chunksParent : transform;

        if (backRoot == null)
        {
            backRoot = new GameObject("SilhouetteBackRoot").transform;
            backRoot.SetParent(parent, false);
        }

        if (midRoot == null)
        {
            midRoot = new GameObject("SilhouetteMidRoot").transform;
            midRoot.SetParent(parent, false);
        }
    }

    private void EnsurePools()
    {
        int initial = Mathf.Max(4, chunksAhead + chunksBehind + 2);
        if (backPool == null)
        {
            backPool = CreatePool(initial);
        }

        if (midPool == null)
        {
            midPool = CreatePool(initial);
        }
    }

    private ObjectPool<SilhouetteChunk> CreatePool(int initialSize)
    {
        Transform parent = chunksParent != null ? chunksParent : transform;
        var pool = new ObjectPool<SilhouetteChunk>(
            () =>
            {
                SilhouetteChunk chunk = Instantiate(silhouettePrefab, parent);
                chunk.gameObject.SetActive(false);
                return chunk;
            },
            chunk => chunk.gameObject.SetActive(true),
            chunk =>
            {
                chunk.ReturnToPool();
                chunk.transform.SetParent(parent, false);
            },
            chunk => Destroy(chunk.gameObject),
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, initialSize),
            maxSize: 100);

        List<SilhouetteChunk> temp = new List<SilhouetteChunk>(initialSize);
        for (int i = 0; i < initialSize; i++)
        {
            temp.Add(pool.Get());
        }
        foreach (var chunk in temp)
        {
            pool.Release(chunk);
        }

        return pool;
    }

    private void ResetLayer(List<SilhouetteChunk> activeList, ObjectPool<SilhouetteChunk> pool, bool isBack, ref float offset, Transform parent)
    {
        ReturnAll(activeList, pool);
        offset = 0f;
        UpdateRootPosition(parent, offset);

        if (cam == null || activeConfig == null || silhouettePrefab == null || pool == null)
        {
            return;
        }

        float cameraLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float startNoiseX = Mathf.Floor((cameraLeft - offset) / chunkWidth) * chunkWidth - chunkWidth * chunksBehind;
        int total = chunksAhead + chunksBehind + 1;

        float spawnX = startNoiseX;
        for (int i = 0; i < total; i++)
        {
            SpawnChunk(pool, activeList, parent, spawnX, isBack);
            spawnX += chunkWidth;
        }
    }

    private void ReturnAll(List<SilhouetteChunk> activeList, ObjectPool<SilhouetteChunk> pool)
    {
        if (pool == null)
        {
            activeList.Clear();
            return;
        }

        for (int i = 0; i < activeList.Count; i++)
        {
            pool.Release(activeList[i]);
        }
        activeList.Clear();
    }

   private SilhouetteChunk SpawnChunk(ObjectPool<SilhouetteChunk> pool, List<SilhouetteChunk> list, Transform parent, float startX, bool isBack)
    {
        SilhouetteChunk chunk = pool.Get();
        chunk.transform.SetParent(parent, false);
        chunk.transform.localPosition = Vector3.zero;
        chunk.Initialize(activeConfig, startX, pointsPerChunk, pointSpacing, bottomY, isBack);
        list.Add(chunk);
        return chunk;
    }

    private void Update()
    {
        if (activeConfig == null || cam == null || silhouettePrefab == null) return;

        float camX = cam.transform.position.x;
        float cameraLeft = camX - cam.orthographicSize * cam.aspect;
        float cameraRight = camX + cam.orthographicSize * cam.aspect;
        float delta = camX - lastCamX;

        if (!Mathf.Approximately(delta, 0f))
        {
            backOffset += delta * (activeConfig.silhouetteBackSpeedRatio - 1f);
            midOffset += delta * (activeConfig.silhouetteMidSpeedRatio - 1f);
            UpdateRootPosition(backRoot, backOffset);
            UpdateRootPosition(midRoot, midOffset);
        }

        MaintainLayer(activeBack, backPool, backOffset, backRoot, cameraLeft, cameraRight, true);
        MaintainLayer(activeMid, midPool, midOffset, midRoot, cameraLeft, cameraRight, false);

        lastCamX = camX;
    }

    private void MaintainLayer(List<SilhouetteChunk> activeList, ObjectPool<SilhouetteChunk> pool, float offset, Transform parent, float cameraLeft, float cameraRight, bool isBack)
    {
        if (pool == null) return;

        // Chunk'ın ekrandaki gerçek pozisyonu = StartX + offset (root kayması)
        // Karşılaştırmalarda bunu kullan

        if (activeList.Count == 0)
        {
            // Offset'i çıkararak noise uzayındaki başlangıç noktasını bul
            float startX = Mathf.Floor((cameraLeft - offset) / chunkWidth) * chunkWidth - chunkWidth * chunksBehind;
            SpawnChunk(pool, activeList, parent, startX, isBack);
        }

        // Sağda yeterli chunk üret
        for (int i = 0; i < 20; i++)
        {
            if (activeList.Count == 0) break;
            SilhouetteChunk last = activeList[activeList.Count - 1];
            float lastScreenEnd = last.StartX + chunkWidth + offset;
            if (lastScreenEnd < cameraRight + chunkWidth * chunksAhead)
                SpawnChunk(pool, activeList, parent, last.StartX + chunkWidth, isBack);
            else
                break;
        }

        // Solda kalan chunk'ları havuza gönder
        for (int i = 0; i < 20; i++)
        {
            if (activeList.Count == 0) break;
            SilhouetteChunk first = activeList[0];
            float firstScreenEnd = first.StartX + chunkWidth + offset;
            if (firstScreenEnd < cameraLeft - chunkWidth * chunksBehind)
            {
                pool.Release(first);
                activeList.RemoveAt(0);
            }
            else
                break;
        }
    }

    private void UpdateRootPosition(Transform root, float offset)
    {
        if (root == null)
        {
            return;
        }

        Vector3 pos = root.localPosition;
        pos.x = offset;
        root.localPosition = pos;
    }

    public void SwitchMode(TerrainModeConfig newConfig)
    {
        activeConfig = newConfig;
        lastCamX = cam != null ? cam.transform.position.x : 0f;

        if (activeConfig == null)
        {
            ReturnAll(activeBack, backPool);
            ReturnAll(activeMid, midPool);
            return;
        }

        ResetLayer(activeBack, backPool, true, ref backOffset, backRoot);
        ResetLayer(activeMid, midPool, false, ref midOffset, midRoot);
    }
}
