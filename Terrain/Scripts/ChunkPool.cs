using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ChunkPool
{
    private readonly ObjectPool<TerrainChunk> pool;

    public ChunkPool(TerrainChunk prefab, Transform parent, int initialSize = 6)
    {
        pool = new ObjectPool<TerrainChunk>(
            () =>
            {
                TerrainChunk instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                instance.SetReturnAction(Release);
                return instance;
            },
            actionOnGet: chunk => chunk.gameObject.SetActive(true),
            actionOnRelease: chunk => chunk.gameObject.SetActive(false),
            actionOnDestroy: Object.Destroy,
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, initialSize),
            maxSize: 100);

        Prewarm(initialSize);
    }

    private void Prewarm(int count)
    {
        List<TerrainChunk> temp = new List<TerrainChunk>(count);
        for (int i = 0; i < count; i++)
        {
            temp.Add(pool.Get());
        }

        foreach (TerrainChunk chunk in temp)
        {
            pool.Release(chunk);
        }
    }

    public TerrainChunk Get()
    {
        return pool.Get();
    }

    public void Release(TerrainChunk chunk)
    {
        if (chunk != null)
        {
            pool.Release(chunk);
        }
    }
}
