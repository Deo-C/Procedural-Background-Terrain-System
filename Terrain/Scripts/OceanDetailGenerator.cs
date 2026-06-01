using System;
using System.Collections.Generic;
using UnityEngine;

public class OceanDetailGenerator
{
    private const int MaxSeagullsPerChunk = 15;
    private static Material fallbackMaterial;

    public List<GameObject> GenerateBoats(Vector2[] surfacePoints, OceanDetailConfig config, float noiseSeed, Transform parent, Material sharedMaterial, int sortingOrder, int backgroundLayer)
    {
        List<GameObject> boats = new List<GameObject>();
        if (config == null || surfacePoints == null || surfacePoints.Length < 2 || parent == null)
        {
            return boats;
        }

        float startX = surfacePoints[0].x;
        float endX = surfacePoints[surfacePoints.Length - 1].x;
        System.Random rng = new System.Random(Mathf.FloorToInt(noiseSeed * 1000f + startX));

        float cursor = startX + RandomRange(rng, config.minBoatSpacing, config.maxBoatSpacing);
        while (cursor < endX)
        {
            float surfaceY = SampleHeight(surfacePoints, cursor);
            float boatY = surfaceY - config.boatHeight * 0.5f;

            Mesh boatMesh = BuildBoatMesh(config);
            if (boatMesh != null)
            {
                GameObject boat = CreateDetailObject("Boat", boatMesh, parent, sharedMaterial, sortingOrder, backgroundLayer);
                boat.transform.position = new Vector3(cursor, boatY, 0f);
                boats.Add(boat);
            }

            cursor += RandomRange(rng, config.minBoatSpacing, config.maxBoatSpacing);
        }

        return boats;
    }

    public List<GameObject> GenerateSeagulls(List<GameObject> boats, Vector2[] surfacePoints, OceanDetailConfig config, float noiseSeed, Transform parent, Material sharedMaterial, int sortingOrder, int backgroundLayer)
    {
        List<GameObject> seagulls = new List<GameObject>();
        if (config == null || parent == null)
        {
            return seagulls;
        }

        System.Random rng = new System.Random(Mathf.FloorToInt((noiseSeed + 123.45f) * 1000f));
        int seagullBudget = MaxSeagullsPerChunk;

        // Per-boat seagulls
        if (boats != null)
        {
            for (int i = 0; i < boats.Count && seagullBudget > 0; i++)
            {
                int count = rng.Next(config.seagullsPerBoat_Min, config.seagullsPerBoat_Max + 1);
                count = Mathf.Min(count, seagullBudget);
                for (int s = 0; s < count; s++)
                {
                    Vector3 boatPos = boats[i].transform.position;
                    Vector3 pos = boatPos + new Vector3(RandomRange(rng, -config.seagullSpreadRadius, config.seagullSpreadRadius), RandomRange(rng, config.seagullMinHeight, config.seagullMaxHeight), 0f);
                    GameObject g = CreateSeagull(config, parent, sharedMaterial, sortingOrder, backgroundLayer, pos, rng);
                    if (g != null)
                    {
                        seagulls.Add(g);
                        seagullBudget--;
                        if (seagullBudget == 0) break;
                    }
                }
            }
        }

        // Independent seagulls
        if (seagullBudget > 0 && surfacePoints != null && surfacePoints.Length > 1)
        {
            float startX = surfacePoints[0].x + 3f;
            float endX = surfacePoints[surfacePoints.Length - 1].x - 3f;
            if (endX > startX)
            {
                int randomCount = rng.Next(config.randomSeagulls_Min, config.randomSeagulls_Max + 1);
                randomCount = Mathf.Min(randomCount, seagullBudget);
                for (int i = 0; i < randomCount; i++)
                {
                    float x = RandomRange(rng, startX, endX);
                    float baseY = SampleHeight(surfacePoints, x);
                    float y = baseY + RandomRange(rng, config.seagullMinHeight, config.seagullMaxHeight);
                    Vector3 pos = new Vector3(x, y, 0f);
                    GameObject g = CreateSeagull(config, parent, sharedMaterial, sortingOrder, backgroundLayer, pos, rng);
                    if (g != null)
                    {
                        seagulls.Add(g);
                        seagullBudget--;
                        if (seagullBudget == 0) break;
                    }
                }
            }
        }

        return seagulls;
    }

    public List<GameObject> GenerateCorals(Vector2[] surfacePoints, OceanDetailConfig config, float minHeight, float maxHeight, float noiseSeed, Transform parent, Material sharedMaterial, int sortingOrder, int backgroundLayer)
    {
        List<GameObject> corals = new List<GameObject>();
        if (config == null || surfacePoints == null || surfacePoints.Length < 2 || parent == null)
        {
            return corals;
        }

        float startX = surfacePoints[0].x;
        float endX = surfacePoints[surfacePoints.Length - 1].x;
        float heightRange = Mathf.Max(0.0001f, maxHeight - minHeight);

        System.Random rng = new System.Random(Mathf.FloorToInt((noiseSeed + 987.65f) * 1000f + startX));
        float cursor = startX + RandomRange(rng, config.minCoralSpacing, config.maxCoralSpacing);

        while (cursor < endX)
        {
            float surfaceY = SampleHeight(surfacePoints, cursor);
            float ratio = Mathf.Clamp01((surfaceY - minHeight) / heightRange);
            if (ratio <= config.coralMaxHeightRatio)
            {
                int coralCount = rng.Next(config.minCoralsPerCluster, config.maxCoralsPerCluster + 1);
                Mesh clusterMesh = BuildCoralClusterMesh(coralCount, config, rng);
                if (clusterMesh != null)
                {
                    GameObject cluster = CreateDetailObject("CoralCluster", clusterMesh, parent, sharedMaterial, sortingOrder, backgroundLayer);
                    cluster.transform.position = new Vector3(cursor, surfaceY, 0f);
                    corals.Add(cluster);
                }
            }

            cursor += RandomRange(rng, config.minCoralSpacing, config.maxCoralSpacing);
        }

        return corals;
    }

    // --- Mesh builders ---

    public static Mesh BuildSeagullMesh(OceanDetailConfig config, float wingAngle)
    {
        Mesh mesh = new Mesh();
        UpdateSeagullMesh(mesh, config, wingAngle);
        return mesh;
    }

    public static void UpdateSeagullMesh(Mesh mesh, OceanDetailConfig config, float wingAngle)
    {
        if (mesh == null || config == null)
        {
            return;
        }

        float span = config.seagullWingSpan;
        float rad = Mathf.Deg2Rad * Mathf.Clamp(wingAngle, 0f, 30f);
        float up = Mathf.Sin(rad) * span;
        float down = -Mathf.Sin(rad) * span * 0.4f;

        Vector3[] vertices = new Vector3[5];
        int[] triangles = new int[] { 0, 1, 2, 0, 3, 4 };
        Color[] colors = new Color[5];

        vertices[0] = Vector3.zero;
        vertices[1] = new Vector3(-span * 0.5f, up, 0f);
        vertices[2] = new Vector3(-span * 0.8f, down, 0f);
        vertices[3] = new Vector3(span * 0.5f, up, 0f);
        vertices[4] = new Vector3(span * 0.8f, down, 0f);

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = config.seagullColor;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private static Mesh BuildBoatMesh(OceanDetailConfig config)
    {
        List<CombineInstance> combine = new List<CombineInstance>(4);
        List<Mesh> temp = new List<Mesh>(4);

        Mesh hull = BuildHull(config);
        if (hull != null) { combine.Add(new CombineInstance { mesh = hull, transform = Matrix4x4.identity }); temp.Add(hull); }
        Mesh deck = BuildDeck(config);
        if (deck != null) { combine.Add(new CombineInstance { mesh = deck, transform = Matrix4x4.identity }); temp.Add(deck); }
        Mesh mast = BuildMast(config);
        if (mast != null) { combine.Add(new CombineInstance { mesh = mast, transform = Matrix4x4.identity }); temp.Add(mast); }
        Mesh sail = BuildSail(config);
        if (sail != null) { combine.Add(new CombineInstance { mesh = sail, transform = Matrix4x4.identity }); temp.Add(sail); }

        if (combine.Count == 0)
        {
            return null;
        }

        Mesh boat = new Mesh();
        boat.CombineMeshes(combine.ToArray(), true, false);
        boat.RecalculateBounds();
        boat.RecalculateNormals();

        for (int i = 0; i < temp.Count; i++)
        {
            UnityEngine.Object.Destroy(temp[i]);
        }

        return boat;
    }

    private static Mesh BuildHull(OceanDetailConfig config)
    {
        float wTop = config.boatWidth;
        float wBottom = config.boatWidth * 0.7f;
        float h = config.boatHeight;

        Vector3[] v = new Vector3[4];
        v[0] = new Vector3(-wBottom * 0.5f, 0f, 0f);
        v[1] = new Vector3(wBottom * 0.5f, 0f, 0f);
        v[2] = new Vector3(-wTop * 0.5f, h, 0f);
        v[3] = new Vector3(wTop * 0.5f, h, 0f);

        int[] t = new int[] { 0, 2, 1, 1, 2, 3 };
        Color[] c = new Color[] { config.boatHullColor, config.boatHullColor, config.boatHullColor, config.boatHullColor };

        Mesh mesh = new Mesh { vertices = v, triangles = t, colors = c };
        return mesh;
    }

    private static Mesh BuildDeck(OceanDetailConfig config)
    {
        float w = config.boatWidth * 1.05f;
        float h = config.boatHeight * 0.12f;
        float y = config.boatHeight * 0.9f;

        Vector3[] v = new Vector3[4];
        v[0] = new Vector3(-w * 0.5f, y, 0f);
        v[1] = new Vector3(-w * 0.5f, y + h, 0f);
        v[2] = new Vector3(w * 0.5f, y + h, 0f);
        v[3] = new Vector3(w * 0.5f, y, 0f);

        int[] t = new int[] { 0, 1, 2, 0, 2, 3 };
        Color[] c = new Color[] { config.boatDeckColor, config.boatDeckColor, config.boatDeckColor, config.boatDeckColor };

        Mesh mesh = new Mesh { vertices = v, triangles = t, colors = c };
        return mesh;
    }

    private static Mesh BuildMast(OceanDetailConfig config)
    {
        float w = config.boatWidth * 0.04f;
        float h = config.mastHeight;
        float x = -config.boatWidth * 0.5f + config.boatWidth * 0.4f;
        float y = config.boatHeight;

        Vector3[] v = new Vector3[4];
        v[0] = new Vector3(x - w * 0.5f, y, 0f);
        v[1] = new Vector3(x - w * 0.5f, y + h, 0f);
        v[2] = new Vector3(x + w * 0.5f, y + h, 0f);
        v[3] = new Vector3(x + w * 0.5f, y, 0f);

        int[] t = new int[] { 0, 1, 2, 0, 2, 3 };
        Color mastColor = Color.Lerp(config.boatDeckColor, config.boatHullColor, 0.3f);
        Color[] c = new Color[] { mastColor, mastColor, mastColor, mastColor };

        Mesh mesh = new Mesh { vertices = v, triangles = t, colors = c };
        return mesh;
    }

    private static Mesh BuildSail(OceanDetailConfig config)
    {
        float baseY = config.boatHeight;
        float baseX = -config.boatWidth * 0.5f + config.boatWidth * 0.4f;
        float width = config.sailWidth;
        float height = config.mastHeight * 0.75f;

        Vector3[] v = new Vector3[3];
        v[0] = new Vector3(baseX, baseY, 0f);
        v[1] = new Vector3(baseX + width, baseY, 0f);
        v[2] = new Vector3(baseX + width * 0.5f, baseY + height, 0f);

        int[] t = new int[] { 0, 1, 2 };
        Color[] c = new Color[3];
        c[0] = config.sailColor;
        c[1] = config.sailShadowColor;
        c[2] = Color.Lerp(config.sailColor, config.sailShadowColor, 0.3f);

        Mesh mesh = new Mesh { vertices = v, triangles = t, colors = c };
        return mesh;
    }

    private static Mesh BuildCoralClusterMesh(int coralCount, OceanDetailConfig config, System.Random rng)
    {
        if (coralCount <= 0)
        {
            return null;
        }

        List<CombineInstance> combines = new List<CombineInstance>(coralCount);
        List<Mesh> temp = new List<Mesh>(coralCount);

        for (int i = 0; i < coralCount; i++)
        {
            float height = RandomRange(rng, config.coralMinHeight, config.coralMaxHeight);
            int type = rng.Next(0, 3);
            Mesh coral = type switch
            {
                0 => BuildCoralBranch(height, config, rng),
                1 => BuildCoralBulb(height, config, rng),
                _ => BuildCoralFan(height, config, rng)
            };

            if (coral == null) continue;

            float offsetX = RandomRange(rng, -1.2f, 1.2f);
            CombineInstance ci = new CombineInstance
            {
                mesh = coral,
                transform = Matrix4x4.TRS(new Vector3(offsetX, height * 0.5f, 0f), Quaternion.identity, Vector3.one)
            };
            combines.Add(ci);
            temp.Add(coral);
        }

        if (combines.Count == 0)
        {
            return null;
        }

        Mesh cluster = new Mesh();
        cluster.CombineMeshes(combines.ToArray(), true, true);
        cluster.RecalculateBounds();
        cluster.RecalculateNormals();

        for (int i = 0; i < temp.Count; i++)
        {
            UnityEngine.Object.Destroy(temp[i]);
        }

        return cluster;
    }

    private static Mesh BuildCoralBranch(float height, OceanDetailConfig config, System.Random rng)
    {
        float width = height * 0.18f;
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        List<Color> c = new List<Color>();

        Color color = PickColor(config, rng);

        // main stem centered so it spans roughly [-h, 0]
        AddRect(v, t, c, width, height, 0f, -height * 0.5f, color);

        int sideCount = rng.Next(2, 4);
        for (int i = 0; i < sideCount; i++)
        {
            float branchH = height * RandomRange(rng, 0.25f, 0.45f);
            float baseY = -height + height * RandomRange(rng, 0.3f, 0.75f);
            float angle = RandomRange(rng, 20f, 40f);
            float dir = rng.NextDouble() > 0.5 ? 1f : -1f;
            float rad = angle * Mathf.Deg2Rad * dir;
            float dx = Mathf.Cos(rad) * branchH;
            float dy = Mathf.Sin(rad) * branchH;
            AddRect(v, t, c, width * 0.6f, branchH, dx * 0.5f, baseY + dy * 0.5f, color, Quaternion.Euler(0f, 0f, angle * dir));
        }

        Mesh m = new Mesh { vertices = v.ToArray(), triangles = t.ToArray(), colors = c.ToArray() };
        return m;
    }

    private static Mesh BuildCoralBulb(float height, OceanDetailConfig config, System.Random rng)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        List<Color> c = new List<Color>();
        Color color = PickColor(config, rng);

        float stemH = height * 0.35f;
        float stemW = height * 0.18f;
        AddRect(v, t, c, stemW, stemH, 0f, -stemH * 0.5f, color);

        float radius = height * 0.35f;
        int sides = 8;
        int centerIndex = v.Count;
        float stemTop = 0f; // stem ends around y=0
        v.Add(new Vector3(0f, stemTop + radius * 0.2f, 0f));
        c.Add(color);

        for (int i = 0; i < sides; i++)
        {
            float ang = (Mathf.PI * 2f / sides) * i;
            v.Add(new Vector3(Mathf.Cos(ang) * radius, stemTop + Mathf.Sin(ang) * radius + radius * 0.6f, 0f));
            c.Add(color);
        }

        for (int i = 0; i < sides; i++)
        {
            int a = centerIndex;
            int b = centerIndex + 1 + i;
            int d = centerIndex + 1 + ((i + 1) % sides);
            t.Add(a); t.Add(b); t.Add(d);
        }

        Mesh m = new Mesh { vertices = v.ToArray(), triangles = t.ToArray(), colors = c.ToArray() };
        return m;
    }

    private static Mesh BuildCoralFan(float height, OceanDetailConfig config, System.Random rng)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        List<Color> c = new List<Color>();
        Color color = PickColor(config, rng);

        int points = rng.Next(5, 8);
        Vector3 root = new Vector3(0f, -height * 0.5f, 0f);
        v.Add(root);
        c.Add(color);

        float startAngle = -60f * Mathf.Deg2Rad;
        float endAngle = 60f * Mathf.Deg2Rad;

        for (int i = 0; i < points; i++)
        {
            float tNorm = points == 1 ? 0.5f : i / (float)(points - 1);
            float ang = Mathf.Lerp(startAngle, endAngle, tNorm);
            float r = height * RandomRange(rng, 0.8f, 1.1f);
            v.Add(root + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r + height * 0.4f, 0f));
            c.Add(color);
        }

        for (int i = 1; i < points; i++)
        {
            t.Add(0); t.Add(i); t.Add(i + 1);
        }

        Mesh m = new Mesh { vertices = v.ToArray(), triangles = t.ToArray(), colors = c.ToArray() };
        return m;
    }

    // --- Helpers ---

    private static GameObject CreateSeagull(OceanDetailConfig config, Transform parent, Material sharedMaterial, int sortingOrder, int backgroundLayer, Vector3 position, System.Random rng)
    {
        Mesh mesh = BuildSeagullMesh(config, RandomRange(rng, 0f, 20f));
        if (mesh == null)
        {
            return null;
        }

        GameObject go = new GameObject("Seagull");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        if (backgroundLayer >= 0) go.layer = backgroundLayer;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = sharedMaterial != null ? sharedMaterial : GetFallbackMaterial();
        mr.sortingOrder = sortingOrder;

        SeagullAnimator anim = go.AddComponent<SeagullAnimator>();
        anim.animSpeed = config.seagullAnimSpeed;
        anim.Initialize(config);
        anim.timeOffset = RandomRange(rng, 0f, Mathf.PI * 2f);

        return go;
    }

    private static GameObject CreateDetailObject(string name, Mesh mesh, Transform parent, Material sharedMaterial, int sortingOrder, int backgroundLayer)
    {
        if (mesh == null)
        {
            return null;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (backgroundLayer >= 0)
        {
            go.layer = backgroundLayer;
        }

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = sharedMaterial != null ? sharedMaterial : GetFallbackMaterial();
        mr.sortingOrder = sortingOrder;

        return go;
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

    private static Color PickColor(OceanDetailConfig config, System.Random rng)
    {
        if (config.coralColors == null || config.coralColors.Length == 0)
        {
            return Color.magenta;
        }
        int idx = rng.Next(0, config.coralColors.Length);
        return config.coralColors[idx];
    }

    private static void AddRect(List<Vector3> v, List<int> t, List<Color> c, float width, float height, float centerX, float centerY, Color color, Quaternion? rotation = null)
    {
        int start = v.Count;
        Vector3 offset = new Vector3(centerX, centerY, 0f);
        Quaternion rot = rotation ?? Quaternion.identity;
        v.Add(rot * new Vector3(-width * 0.5f, -height * 0.5f, 0f) + offset);
        v.Add(rot * new Vector3(-width * 0.5f, height * 0.5f, 0f) + offset);
        v.Add(rot * new Vector3(width * 0.5f, height * 0.5f, 0f) + offset);
        v.Add(rot * new Vector3(width * 0.5f, -height * 0.5f, 0f) + offset);

        t.Add(start + 0); t.Add(start + 1); t.Add(start + 2);
        t.Add(start + 0); t.Add(start + 2); t.Add(start + 3);

        c.Add(color); c.Add(color); c.Add(color); c.Add(color);
    }

    private static float RandomRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
