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
        // Gets base meshData and build upon it
        MeshData meshData = new MeshData(worldInfo);

        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;
        int verticesPerChunkLineExtended = verticesPerChunkLine + (worldInfo.smoothSize * 4);

        int[,] vertexIndices = new int[verticesPerChunkLineExtended, verticesPerChunkLineExtended];
        float[,] heights = new float[verticesPerChunkLineExtended, verticesPerChunkLineExtended];

        for (int y = 0; y < verticesPerChunkLineExtended; y++)
        {
            for (int x = 0; x < verticesPerChunkLineExtended; x++)
            {
                Vector2 positionInChunk = new Vector2(x, y) - (2 * worldInfo.smoothSize * Vector2.one);
                Vector2 globalPosition = positionInChunk + (chunkPosition * (verticesPerChunkLine - 1) + Vector2.one);

                float angle = meshData.CalculateAngle(globalPosition, worldCentre);
                string section = meshData.CalculateSection(angle, globalPosition, worldCentre, worldInfo);
                heights[x, y] = meshData.CalculateHeight(section, globalPosition, terrainInfos);
            }
        }

        float[,] smoothedHeights = new float[verticesPerChunkLine, verticesPerChunkLine];

        // Smooth the change between verticies to remove hard edges
        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                float heightSum = 0;
                int numValues = 0;

                for (int deltaY = -worldInfo.smoothSize; deltaY <= worldInfo.smoothSize; deltaY++)
                {
                    int workingY = y + deltaY + (worldInfo.smoothSize * 2);

                    for (int deltaX = -worldInfo.smoothSize; deltaX <= worldInfo.smoothSize; deltaX++)
                    {
                        int workingX = x + deltaX + (worldInfo.smoothSize * 2);

                        heightSum += heights[workingX, workingY];
                        ++numValues;
                    }
                }

                smoothedHeights[x, y] = heightSum / numValues;
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

                meshData.AddVertex(new Vector3(x, smoothedHeights[x, y], y), percent, vertexIndex);
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

        string section = "?";
        float distanceFromCentre = Mathf.Sqrt(Mathf.Pow(worldCentre.x - globalPosition.x, 2) + Mathf.Pow(worldCentre.y - globalPosition.y, 2));
        if (distanceFromCentre > mapRadius)
            section = "Boundary";
        else if (distanceFromCentre < spawnRadius)
            section = "Spawn";
        else
        {
            if (0 <= angle && angle < 2 * Mathf.PI / 5)
                section = "Earth";
            else if (2 * Mathf.PI / 5 <= angle && angle < 4 * Mathf.PI / 5)
                section = "Air";
            else if (4 * Mathf.PI / 5 <= angle && angle < 6 * Mathf.PI / 5)
                section = "Thunder";
            else if (6 * Mathf.PI / 5 <= angle && angle < 8 * Mathf.PI / 5)
                section = "Water";
            else if (8 * Mathf.PI / 5 <= angle && angle < 2 * Mathf.PI)
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
            height += heightCurve.Evaluate(perlinValue) * amplitude;
            maxPossibleHeight += amplitude;

            amplitude *= terrainInfos[num].persistance;
            frequency *= terrainInfos[num].lacunarity;
        }

        height /= maxPossibleHeight * 2;
        float minHeight = terrainInfos[num].minHeight;
        float maxHeight = terrainInfos[num].maxHeight;

        return minHeight + (height * (maxHeight - minHeight));
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
