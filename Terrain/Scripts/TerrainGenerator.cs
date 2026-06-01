using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
    private readonly TerrainModeConfig config;
    private readonly float amplitudeMultiplier;
    private readonly float noiseOffset;

    public TerrainGenerator(TerrainModeConfig config, float amplitudeMultiplier = 1f, float noiseOffset = 0f)
    {
        this.config = config;
        this.amplitudeMultiplier = amplitudeMultiplier;
        this.noiseOffset = noiseOffset;
    }

    public float[] GenerateHeightMap(float startX, int pointCount, float pointSpacing)
    {
        return GenerateHeightMap(startX, pointCount, pointSpacing, 0f, false);
    }

    public float[] GenerateHeightMap(float startX, int pointCount, float pointSpacing, float baselineOffset, bool invertNoise)
    {
        float[] heights = new float[pointCount];
        float baseAmplitude = config.amplitude * amplitudeMultiplier;

        for (int i = 0; i < pointCount; i++)
        {
            float xWorld = startX + i * pointSpacing;
            float frequency = config.noiseScale;
            float amplitudeAccum = 0f;
            float noiseValue = 0f;

            for (int octave = 0; octave < config.octaves; octave++)
            {
                float xCoord = xWorld * frequency + config.noiseSeed + noiseOffset;
                float yCoord = octave * 100f;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                float octaveAmplitude = baseAmplitude * Mathf.Pow(config.persistence, octave);
                noiseValue += sample * octaveAmplitude;
                amplitudeAccum += octaveAmplitude;
                frequency *= config.lacunarity;
            }

            float normalized = amplitudeAccum > 0f ? noiseValue / amplitudeAccum : 0.5f;
            if (invertNoise)
            {
                normalized = 1f - normalized;
            }

            float mapped = Mathf.Lerp(config.minY, config.maxY, normalized) + config.baselineY + baselineOffset;
            heights[i] = mapped;
        }

        return heights;
    }

    public Vector2[] SmoothHeightMap(float[] heights, float pointSpacing)
    {
        const int subdivisions = 4;
        int count = heights.Length;
        if (count < 2)
        {
            Vector2[] minimal = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                minimal[i] = new Vector2(i * pointSpacing, heights[i]);
            }
            return minimal;
        }

        List<Vector2> points = new List<Vector2>((count - 1) * subdivisions + 1);

        for (int i = 0; i < count - 1; i++)
        {
            float p0 = heights[Mathf.Max(i - 1, 0)];
            float p1 = heights[i];
            float p2 = heights[i + 1];
            float p3 = heights[Mathf.Min(i + 2, count - 1)];

            for (int s = 0; s < subdivisions; s++)
            {
                float t = s / (float)subdivisions;
                float t2 = t * t;
                float t3 = t2 * t;

                float value = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                float x = (i * pointSpacing) + t * pointSpacing;
                points.Add(new Vector2(x, value));
            }
        }

        points.Add(new Vector2((count - 1) * pointSpacing, heights[count - 1]));

        return points.ToArray();
    }

    public Mesh BuildMesh(Vector2[] topPoints, float bottomY, Color color)
    {
        int vertCount = topPoints.Length * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[(topPoints.Length - 1) * 6];

        for (int i = 0; i < topPoints.Length; i++)
        {
            int topIndex = i * 2;
            int bottomIndex = topIndex + 1;

            vertices[topIndex] = new Vector3(topPoints[i].x, topPoints[i].y, 0f);
            vertices[bottomIndex] = new Vector3(topPoints[i].x, bottomY, 0f);

            colors[topIndex] = color;
            colors[bottomIndex] = color;

            float u = i / (float)Mathf.Max(1, topPoints.Length - 1);
            uvs[topIndex] = new Vector2(u, 1f);
            uvs[bottomIndex] = new Vector2(u, 0f);
        }

        int tri = 0;
        for (int i = 0; i < topPoints.Length - 1; i++)
        {
            int topA = i * 2;
            int topB = topA + 2;
            int bottomA = topA + 1;
            int bottomB = topB + 1;

            triangles[tri++] = topA;
            triangles[tri++] = topB;
            triangles[tri++] = bottomA;

            triangles[tri++] = topB;
            triangles[tri++] = bottomB;
            triangles[tri++] = bottomA;
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
}
