using System;
using System.Collections.Generic;
using UnityEngine;

public class MountainDetailGenerator
{
    private static Material fallbackMaterial;

    public List<GameObject> Generate(MountainDetailConfig detailConfig, TerrainModeConfig modeConfig, Vector2[] topPoints, Transform parent, Material sharedMaterial)
    {
        List<GameObject> results = new List<GameObject>();
        if (detailConfig == null || modeConfig == null || topPoints == null || topPoints.Length < 2 || parent == null)
        {
            return results;
        }

        float minHeight = modeConfig.minY + modeConfig.baselineY;
        float heightRange = Mathf.Max(0.0001f, modeConfig.maxY - modeConfig.minY);

        float[] ratios = new float[topPoints.Length];
        for (int i = 0; i < topPoints.Length; i++)
        {
            ratios[i] = Mathf.Clamp01((topPoints[i].y - minHeight) / heightRange);
        }

        int backgroundLayer = LayerMask.NameToLayer("Background");
        Material mat = sharedMaterial != null ? sharedMaterial : GetFallbackMaterial();

        bool[] snowMask = BuildMask(ratios, detailConfig.snowHeightThreshold, 1f);
        Mesh snowMesh = BuildBandMesh(topPoints, snowMask, detailConfig.snowCapThickness, detailConfig.snowColor);
        if (snowMesh != null)
        {
            GameObject snow = CreateDetailObject("Snow", snowMesh, parent, mat, 1, backgroundLayer);
            results.Add(snow);
        }

        bool[] iceMask = BuildMask(ratios, detailConfig.iceHeightThreshold, detailConfig.snowHeightThreshold);
        Mesh iceMesh = BuildBandMesh(topPoints, iceMask, detailConfig.iceThickness, detailConfig.iceColor);
        if (iceMesh != null)
        {
            GameObject ice = CreateDetailObject("Ice", iceMesh, parent, mat, 1, backgroundLayer);
            results.Add(ice);
        }

        GenerateTreeClusters(detailConfig, modeConfig, topPoints, minHeight, heightRange, parent, mat, backgroundLayer, results);

        return results;
    }

    private static Material GetFallbackMaterial()
    {
        if (fallbackMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            fallbackMaterial = shader != null ? new Material(shader) : null;
        }

        return fallbackMaterial;
    }

    private static bool[] BuildMask(float[] ratios, float minThreshold, float maxThresholdExclusive)
    {
        bool[] mask = new bool[ratios.Length];
        for (int i = 0; i < ratios.Length; i++)
        {
            float r = ratios[i];
            mask[i] = r >= minThreshold && r < maxThresholdExclusive;
        }
        return mask;
    }

    private static Mesh BuildBandMesh(Vector2[] points, bool[] mask, float thickness, Color color)
    {
        if (points == null || mask == null || points.Length != mask.Length || points.Length < 2 || thickness <= 0f)
        {
            return null;
        }

        int vertCount = points.Length * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[(points.Length - 1) * 6];
        int triIndex = 0;
        bool hasTriangles = false;

        for (int i = 0; i < points.Length; i++)
        {
            int topIndex = i * 2;
            int bottomIndex = topIndex + 1;
            vertices[topIndex] = new Vector3(points[i].x, points[i].y + thickness, 0f);
            vertices[bottomIndex] = new Vector3(points[i].x, points[i].y, 0f);

            colors[topIndex] = color;
            colors[bottomIndex] = color;

            float u = i / Mathf.Max(1f, points.Length - 1f);
            uvs[topIndex] = new Vector2(u, 1f);
            uvs[bottomIndex] = new Vector2(u, 0f);
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (mask[i] && mask[i + 1])
            {
                int topA = i * 2;
                int bottomA = topA + 1;
                int topB = topA + 2;
                int bottomB = topB + 1;

                triangles[triIndex++] = topA;
                triangles[triIndex++] = topB;
                triangles[triIndex++] = bottomA;

                triangles[triIndex++] = topB;
                triangles[triIndex++] = bottomB;
                triangles[triIndex++] = bottomA;

                hasTriangles = true;
            }
        }

        if (!hasTriangles)
        {
            return null;
        }

        if (triIndex < triangles.Length)
        {
            Array.Resize(ref triangles, triIndex);
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            colors = colors,
            uv = uvs
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    private static void GenerateTreeClusters(MountainDetailConfig detailConfig, TerrainModeConfig modeConfig, Vector2[] topPoints, float minHeight, float heightRange, Transform parent, Material sharedMaterial, int backgroundLayer, List<GameObject> results)
    {
        if (detailConfig.minTreesPerCluster <= 0 || detailConfig.maxTreesPerCluster <= 0 || detailConfig.maxClusterSpacing <= 0f)
        {
            return;
        }

        float startX = topPoints[0].x;
        float endX = topPoints[topPoints.Length - 1].x;

        int seed = Mathf.FloorToInt((modeConfig.noiseSeed + startX) * 1000f);
        System.Random rng = new System.Random(seed);

        float cursor = startX;
        while (cursor < endX)
        {
            float noise = Mathf.PerlinNoise(cursor * 0.05f + modeConfig.noiseSeed, 0.5f);
            float densityMultiplier = noise > 0.75f ? 2f : noise > 0.5f ? 1f : 0f;

            if (densityMultiplier > 0f)
            {
                float surfaceY = SampleHeight(topPoints, cursor);
                float ratio = Mathf.Clamp01((surfaceY - minHeight) / heightRange);
                if (ratio >= detailConfig.treeMinHeightRatio && ratio <= detailConfig.treeMaxHeightRatio)
                {
                    int baseCount = rng.Next(detailConfig.minTreesPerCluster, detailConfig.maxTreesPerCluster + 1);
                    baseCount = Mathf.RoundToInt(baseCount * densityMultiplier);
                    float variance = 1f + ((float)rng.NextDouble() * 2f - 1f) * detailConfig.clusterDensityVariance;
                    int treeCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * variance));

                    Mesh clusterMesh = BuildTreeClusterMesh(treeCount, detailConfig, rng, cursor, topPoints, minHeight, heightRange);
                    if (clusterMesh != null)
                    {
                        GameObject cluster = CreateDetailObject("TreeCluster", clusterMesh, parent, sharedMaterial, 2, backgroundLayer);
                        results.Add(cluster);
                    }
                }
            }

            float nextSpacing = Mathf.Lerp(detailConfig.minClusterSpacing, detailConfig.maxClusterSpacing, (float)rng.NextDouble());
            cursor += nextSpacing;
        }
    }

    private static Mesh BuildTreeClusterMesh(int treeCount, MountainDetailConfig detailConfig, System.Random rng, float clusterCenterX, Vector2[] topPoints, float minHeight, float heightRange)
    {
        if (treeCount <= 0)
        {
            return null;
        }

        List<CombineInstance> combines = new List<CombineInstance>(treeCount);
        List<Mesh> tempMeshes = new List<Mesh>(treeCount);

        for (int i = 0; i < treeCount; i++)
        {
            float height = Mathf.Lerp(detailConfig.treeMinHeight, detailConfig.treeMaxHeight, (float)rng.NextDouble());
            float offsetX = Mathf.Lerp(-1.5f, 1.5f, (float)rng.NextDouble());
            float treeX = clusterCenterX + offsetX;
            float groundY = SampleHeight(topPoints, treeX);

            float ratio = Mathf.Clamp01((groundY - minHeight) / heightRange);
            if (ratio < detailConfig.treeMinHeightRatio || ratio > detailConfig.treeMaxHeightRatio)
            {
                continue;
            }

            Mesh treeMesh = BuildSingleTreeMesh(height, detailConfig);
            if (treeMesh == null)
            {
                continue;
            }

            CombineInstance ci = new CombineInstance
            {
                mesh = treeMesh,
                transform = Matrix4x4.TRS(new Vector3(treeX, groundY, 0f), Quaternion.identity, Vector3.one)
            };
            combines.Add(ci);
            tempMeshes.Add(treeMesh);
        }

        if (combines.Count == 0)
        {
            return null;
        }

        Mesh combined = new Mesh();
        combined.CombineMeshes(combines.ToArray(), true, true);
        combined.RecalculateBounds();
        combined.RecalculateNormals();

        for (int i = 0; i < tempMeshes.Count; i++)
        {
            UnityEngine.Object.Destroy(tempMeshes[i]);
        }

        return combined;
    }

    private static Mesh BuildSingleTreeMesh(float height, MountainDetailConfig detailConfig)
    {
        if (height <= 0f)
        {
            return null;
        }

        float trunkWidth = height * 0.08f;
        float trunkHeight = height * 0.3f;

        float leaf1Width = height * 0.5f;
        float leaf1Height = height * 0.4f;
        float leaf1BaseY = trunkHeight;

        float leaf2Width = height * 0.38f;
        float leaf2Height = height * 0.3f;
        float leaf2BaseY = trunkHeight + leaf1Height * 0.4f;

        float leaf3Width = height * 0.25f;
        float leaf3BaseY = leaf2BaseY + leaf2Height * 0.4f;
        float leaf3Height = Mathf.Max(height * 0.15f, height - leaf3BaseY);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        // trunk
        int trunkStart = vertices.Count;
        vertices.Add(new Vector3(-trunkWidth * 0.5f, 0f, 0f));
        vertices.Add(new Vector3(-trunkWidth * 0.5f, trunkHeight, 0f));
        vertices.Add(new Vector3(trunkWidth * 0.5f, trunkHeight, 0f));
        vertices.Add(new Vector3(trunkWidth * 0.5f, 0f, 0f));

        triangles.Add(trunkStart + 0);
        triangles.Add(trunkStart + 1);
        triangles.Add(trunkStart + 2);
        triangles.Add(trunkStart + 0);
        triangles.Add(trunkStart + 2);
        triangles.Add(trunkStart + 3);

        for (int i = 0; i < 4; i++)
        {
            colors.Add(detailConfig.treeTrunkColor);
        }

        // leaf1
        int leaf1Start = vertices.Count;
        vertices.Add(new Vector3(-leaf1Width * 0.5f, leaf1BaseY, 0f));
        vertices.Add(new Vector3(leaf1Width * 0.5f, leaf1BaseY, 0f));
        vertices.Add(new Vector3(0f, leaf1BaseY + leaf1Height, 0f));

        triangles.Add(leaf1Start + 0);
        triangles.Add(leaf1Start + 1);
        triangles.Add(leaf1Start + 2);

        for (int i = 0; i < 3; i++)
        {
            colors.Add(detailConfig.treeLeafColor);
        }

        // leaf2
        int leaf2Start = vertices.Count;
        vertices.Add(new Vector3(-leaf2Width * 0.5f, leaf2BaseY, 0f));
        vertices.Add(new Vector3(leaf2Width * 0.5f, leaf2BaseY, 0f));
        vertices.Add(new Vector3(0f, leaf2BaseY + leaf2Height, 0f));

        triangles.Add(leaf2Start + 0);
        triangles.Add(leaf2Start + 1);
        triangles.Add(leaf2Start + 2);

        for (int i = 0; i < 3; i++)
        {
            colors.Add(detailConfig.treeLeafColor);
        }

        // leaf3
        int leaf3Start = vertices.Count;
        vertices.Add(new Vector3(-leaf3Width * 0.5f, leaf3BaseY, 0f));
        vertices.Add(new Vector3(leaf3Width * 0.5f, leaf3BaseY, 0f));
        vertices.Add(new Vector3(0f, leaf3BaseY + leaf3Height, 0f));

        triangles.Add(leaf3Start + 0);
        triangles.Add(leaf3Start + 1);
        triangles.Add(leaf3Start + 2);

        for (int i = 0; i < 3; i++)
        {
            colors.Add(detailConfig.treeLeafColorVariant);
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            colors = colors.ToArray()
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    private static float SampleHeight(Vector2[] points, float x)
    {
        if (points == null || points.Length == 0)
        {
            return 0f;
        }

        if (x <= points[0].x)
        {
            return points[0].y;
        }

        int last = points.Length - 1;
        if (x >= points[last].x)
        {
            return points[last].y;
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (x <= points[i + 1].x)
            {
                float t = Mathf.InverseLerp(points[i].x, points[i + 1].x, x);
                return Mathf.Lerp(points[i].y, points[i + 1].y, t);
            }
        }

        return points[last].y;
    }

    private static GameObject CreateDetailObject(string name, Mesh mesh, Transform parent, Material material, int sortingOrder, int backgroundLayer)
    {
        if (mesh == null)
        {
            return null;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.sortingOrder = sortingOrder;

        if (backgroundLayer >= 0)
        {
            go.layer = backgroundLayer;
        }

        return go;
    }
}
