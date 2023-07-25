using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainChunk
{
    public Vector2 chunkPosition;

    GameObject meshObject;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    public TerrainChunk(Vector2 chunkPosition, int worldSeed, WorldGenerator.WorldInfo worldInfo, WorldGenerator.TerrainInfo[] terrainInfos, Transform parent, Material material)
    {
        this.chunkPosition = chunkPosition;
        Vector2 position = chunkPosition * (worldInfo.verticesPerChunkLine - 1);

        int worldVerticesPerLine = worldInfo.worldVerticesPerLine;
        Vector2 worldCentre = Vector2.one * (worldVerticesPerLine - 1) / 2;

        meshObject = new GameObject("Terrain Chunk: " + Mathf.RoundToInt(chunkPosition.x).ToString() + ", " + Mathf.RoundToInt(chunkPosition.y).ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x - worldCentre.x, 0, position.y - worldCentre.y) * worldInfo.meshScale;
        meshObject.transform.localScale = Vector3.one * worldInfo.meshScale;
        meshObject.transform.parent = parent;
        meshObject.layer = LayerMask.NameToLayer("Ground");

        meshFilter.mesh = GenerateTerrainMesh(chunkPosition, worldCentre, worldInfo, terrainInfos).CreateMesh();
        meshCollider.sharedMesh = meshFilter.mesh;
    }

    public static MeshData GenerateTerrainMesh(Vector2 chunkPosition, Vector2 worldCentre, WorldGenerator.WorldInfo worldInfo, WorldGenerator.TerrainInfo[] terrainInfos)
    {
        // Gets base meshData and builds upon it
        MeshData meshData = new MeshData(worldInfo);

        int verticiesForSmoothing = Mathf.RoundToInt(0.5f * (worldInfo.smoothRange * (worldInfo.smoothRange + 3) + 2));
        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;
        int verticesPerChunkLineExtended = verticesPerChunkLine + (verticiesForSmoothing * 2);

        int[,] vertexIndices = new int[verticesPerChunkLineExtended, verticesPerChunkLineExtended];
        float[,] heights = new float[verticesPerChunkLineExtended, verticesPerChunkLineExtended];

        for (int y = 0; y < verticesPerChunkLineExtended; y++)
        {
            for (int x = 0; x < verticesPerChunkLineExtended; x++)
            {
                Vector2 positionInChunk = new Vector2(x, y) - (verticiesForSmoothing * Vector2.one);
                Vector2 globalPosition = positionInChunk + (chunkPosition * (verticesPerChunkLine - 1) + Vector2.one);

                float angle = meshData.CalculateAngle(globalPosition, worldCentre);
                string section = meshData.CalculateSection(angle, globalPosition, worldCentre, worldInfo);
                heights[x, y] = meshData.CalculateHeight(section, globalPosition, terrainInfos);
            }
        }

        // Smoothing
        // Some visible values are not being smoothed
        float[,] currentHeights = heights;

        for (int i = worldInfo.smoothRange; i > 0; i--)
        {
            int smoothingSize = Mathf.RoundToInt(0.5f * ((worldInfo.smoothRange * (worldInfo.smoothRange + 1)) + (i * (i - 1))));
            heights = currentHeights;
            float smoothProgress = (worldInfo.smoothRange != 0) ? 1 - Mathf.Clamp01(i - 1 / worldInfo.smoothRange) : 0;
            float currentThreshold = smoothProgress * worldInfo.smoothThreshold;

            for (int y = smoothingSize; y < verticesPerChunkLineExtended - smoothingSize; y++)
            {
                for (int x = smoothingSize; x < verticesPerChunkLineExtended - smoothingSize; x++)
                {
                    float heightSum = 0;
                    int numValues = 0;

                    for (int deltaY = -i; deltaY <= i; deltaY++)
                    {
                        int workingY = y + deltaY;

                        for (int deltaX = -i; deltaX <= i; deltaX++)
                        {
                            int workingX = x + deltaX;

                            if (Mathf.Sqrt(Mathf.Pow(deltaX, 2) + Mathf.Pow(deltaY, 2)) <= i + 0.5f)
                            {
                                heightSum += heights[workingX, workingY];
                                ++numValues;
                            }
                        }
                    }

                    float currentHeight = heights[x, y];
                    float smoothedHeight = heightSum / numValues;
                    float heightDifference = smoothedHeight - currentHeight;

                    float selectedHeight = currentHeight;
                    if (Mathf.Abs(heightDifference) > currentThreshold / 10)
                        selectedHeight = currentHeight + (heightDifference / (i * i));

                    currentHeights[x, y] = selectedHeight;
                }
            }
        }

        float[,] adjustedHeights = new float[verticesPerChunkLine, verticesPerChunkLine];

        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                adjustedHeights[x, y] = currentHeights[x + verticiesForSmoothing, y + verticiesForSmoothing];
            }
        }

        // Add verticies
        int vertexIndex = 0;
        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                Vector2 percent = new Vector2(x - 1, y - 1) / (verticesPerChunkLine - 1);
                vertexIndices[x, y] = vertexIndex;

                meshData.AddVertex(new Vector3(x, adjustedHeights[x, y], y), percent, vertexIndex);
                vertexIndex++;
            }
        }

        // Creating Triangles
        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                bool createTriangle = x < verticesPerChunkLine - 1 && y < verticesPerChunkLine - 1;

                if (createTriangle)
                {
                    int a = vertexIndices[x, y];
                    int b = vertexIndices[x + 1, y];
                    int c = vertexIndices[x, y + 1];
                    int d = vertexIndices[x + 1, y + 1];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
            }
        }
        meshData.ProcessMesh();

        return meshData;
    }
}

[System.Serializable]
public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    int triangleIndex;

    public MeshData(WorldGenerator.WorldInfo worldInfo)
    {
        // Creates the base of the meshdata
        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;

        vertices = new Vector3[verticesPerChunkLine * verticesPerChunkLine];
        triangles = new int[(verticesPerChunkLine - 1) * (verticesPerChunkLine - 1) * 6];
        uvs = new Vector2[vertices.Length];
    }

    public float CalculateAngle(Vector2 globalPosition, Vector2 worldCentre)
    {
        float adjustedX = globalPosition.x - worldCentre.x;
        float adjustedY = globalPosition.y - worldCentre.y;

        // Calculates the angle in radiants
        float angle = -Mathf.Atan2(adjustedY, adjustedX);
        if (angle < 0)
            angle += 2 * Mathf.PI;
        
        return angle;
    }

    public string CalculateSection(float angle, Vector2 globalPosition, Vector2 worldCentre, WorldGenerator.WorldInfo worldInfo)
    {
        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;
        int mapRadius = Mathf.FloorToInt(worldInfo.mapRadiusChunks * verticesPerChunkLine);
        int spawnRadius = Mathf.FloorToInt(worldInfo.spawnRadiusChunks * verticesPerChunkLine);

        string section = "Border";
        float borderOffset = 0.25f;
        float distanceFromCentre = Mathf.Sqrt(Mathf.Pow(worldCentre.x - globalPosition.x, 2) + Mathf.Pow(worldCentre.y - globalPosition.y, 2));
        if (distanceFromCentre > mapRadius)
            section = "Boundary";
        else if (distanceFromCentre < spawnRadius)
            section = "Spawn";
        else
        {
            if ((0 + borderOffset) * Mathf.PI / 5 <= angle && angle < (2 - borderOffset) * Mathf.PI / 5)
                section = "Earth";
            else if ((2 + borderOffset) * Mathf.PI / 5 <= angle && angle < (4 - borderOffset) * Mathf.PI / 5)
                section = "Air";
            else if ((4 + borderOffset) * Mathf.PI / 5 <= angle && angle < (6 - borderOffset) * Mathf.PI / 5)
                section = "Thunder";
            else if ((6 + borderOffset) * Mathf.PI / 5 <= angle && angle < (8 - borderOffset) * Mathf.PI / 5)
                section = "Water";
            else if ((8 + borderOffset) * Mathf.PI / 5 <= angle && angle < (10 - borderOffset) * Mathf.PI / 5)
                section = "Fire";
        }

        return section;
    }

    public float CalculateHeight(string section, Vector2 globalPosition, WorldGenerator.TerrainInfo[] terrainInfos)
    {
        int num = 0;
        for (int i = 0; i < terrainInfos.Length; i++)
        {
            if (terrainInfos[i].section == section)
            {
                num = i;
            }
        }
        
        float amplitude = 1;
        float frequency = 1;
        float height = 0;
        float maxPossibleHeight = 0;
        AnimationCurve heightCurve = new(terrainInfos[num].heightCurve.keys);

        for (int i = 0; i < terrainInfos[num].octaves; i++)
        {
            float sampleX = globalPosition.x / terrainInfos[num].scale * frequency;
            float sampleY = globalPosition.y / terrainInfos[num].scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            height += perlinValue * amplitude;
            maxPossibleHeight += amplitude;

            amplitude *= terrainInfos[num].persistance;
            frequency *= terrainInfos[num].lacunarity;
        }

        float normalisedHeight = height / maxPossibleHeight * 1.5f;
        float adjustedHeight = heightCurve.Evaluate(Mathf.Clamp(normalisedHeight, 0, int.MaxValue));

        float minHeight = terrainInfos[num].minHeight;
        float maxHeight = terrainInfos[num].maxHeight;

        return minHeight + (adjustedHeight * (maxHeight - minHeight));
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        vertices[vertexIndex] = vertexPosition;
        uvs[vertexIndex] = uv;
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public void ProcessMesh()
    {
        FlatShading();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles.Reverse().ToArray();
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        return mesh;
    }
}
