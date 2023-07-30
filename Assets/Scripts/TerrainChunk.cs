using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainChunk
{
    GameObject meshObject;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    public TerrainChunk(Vector2Int chunkPositionInTerrain, int worldSeed, WorldGenerator.WorldInfo worldInfo, WorldGenerator.TerrainInfo[] terrainInfos, Transform parent, Material material)
    {
        Vector2Int position = chunkPositionInTerrain * (worldInfo.verticesPerChunkLine - 1);

        int worldVerticesPerLine = worldInfo.worldVerticesPerLine;
        Vector2Int worldCentre = Vector2Int.one * (worldVerticesPerLine - 1) / 2;

        Vector2 chunkPositionInWorld = new Vector2(position.x - worldCentre.x, position.y - worldCentre.y) * worldInfo.meshScale;

        meshObject = new GameObject("Terrain Chunk: " + chunkPositionInTerrain.x.ToString() + ", " + chunkPositionInTerrain.y.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(chunkPositionInWorld.x, 0, chunkPositionInWorld.y);
        meshObject.transform.localScale = Vector3.one * worldInfo.meshScale;
        meshObject.transform.parent = parent;
        meshObject.layer = LayerMask.NameToLayer("Ground");

        VerticeInfo[,] verticeInfos = CalculateVerticies(chunkPositionInTerrain, chunkPositionInWorld, worldCentre, worldInfo, terrainInfos);
        verticeInfos = SmoothVerticeHeights(verticeInfos, worldInfo);

        // Create and apply Texture
        Texture2D texture = CreateChunkTexture(verticeInfos, worldInfo, terrainInfos);
        meshRenderer.material.mainTexture = texture;

        // Create mesh
        meshFilter.mesh = GenerateTerrainMesh(verticeInfos, worldInfo).CreateMesh();
        meshCollider.sharedMesh = meshFilter.mesh;

        // Function that selects random vertices and uses their positions to spawn in objects for the terrain
    }

    public VerticeInfo[,] CalculateVerticies(Vector2Int chunkPositionInTerrain, Vector2 chunkPositionInWorld, Vector2Int worldCentre, WorldGenerator.WorldInfo worldInfo, WorldGenerator.TerrainInfo[] terrainInfos)
    {
        int verticiesForSmoothing = Mathf.RoundToInt(0.5f * (worldInfo.smoothRange * (worldInfo.smoothRange + 3) + 2));
        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;
        int verticesPerChunkLineExtended = verticesPerChunkLine + (verticiesForSmoothing * 2);

        VerticeInfo[,] verticeInfos = new VerticeInfo[verticesPerChunkLineExtended, verticesPerChunkLineExtended];

        for (int y = 0; y < verticesPerChunkLineExtended; y++)
        {
            for (int x = 0; x < verticesPerChunkLineExtended; x++)
            {
                Vector2Int positionInChunk = new Vector2Int(x, y) - (verticiesForSmoothing * Vector2Int.one);
                Vector2Int terrainPosition = positionInChunk + (chunkPositionInTerrain * (verticesPerChunkLine - 1) + Vector2Int.one);
                Vector2 worldPosition = (positionInChunk + (chunkPositionInWorld * (verticesPerChunkLine - 1) + Vector2Int.one)) * worldInfo.meshScale;

                VerticeInfo verticeInfo = new(terrainPosition, worldPosition);

                verticeInfo.CalculateAngle(worldCentre);
                verticeInfo.CalculateSection(worldCentre, worldInfo);
                verticeInfo.CalculateHeight(terrainInfos);

                verticeInfos[x, y] = verticeInfo;
            }
        }

        return verticeInfos;
    }

    public VerticeInfo[,] SmoothVerticeHeights(VerticeInfo[,] verticeInfos, WorldGenerator.WorldInfo worldInfo)
    {
        int verticiesForSmoothing = Mathf.RoundToInt(0.5f * (worldInfo.smoothRange * (worldInfo.smoothRange + 3) + 2));
        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;
        int verticesPerChunkLineExtended = verticesPerChunkLine + (verticiesForSmoothing * 2);

        float[,] currentHeights = new float[verticesPerChunkLineExtended, verticesPerChunkLineExtended];
        float[,] heights;

        for (int y = 0; y < verticesPerChunkLineExtended; y++)
        {
            for (int x = 0; x < verticesPerChunkLineExtended; x++)
            {
                currentHeights[x, y] = verticeInfos[x, y].height;
            }
        }

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

        VerticeInfo[,] adjustedVertices= new VerticeInfo[verticesPerChunkLine, verticesPerChunkLine];

        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                int adjustedX = x + verticiesForSmoothing;
                int adjustedY = y + verticiesForSmoothing;
                adjustedVertices[x, y] = new(verticeInfos[adjustedX, adjustedY].terrainPosition, verticeInfos[adjustedX, adjustedY].worldPosition);
                adjustedVertices[x, y].SetInfo(verticeInfos[adjustedX, adjustedY].angle, verticeInfos[adjustedX, adjustedY].section, currentHeights[adjustedX, adjustedY]);
            }
        }

        return adjustedVertices;
    }

    public Texture2D CreateChunkTexture(VerticeInfo[,] verticeInfos, WorldGenerator.WorldInfo worldInfo, WorldGenerator.TerrainInfo[] terrainInfos)
    {
        // Set colours based on verticie section and height
        Color32[] colourMap = new Color32[worldInfo.verticesPerChunkLine * worldInfo.verticesPerChunkLine];
        for (int y = 0; y < worldInfo.verticesPerChunkLine; y++)
        {
            for (int x = 0; x < worldInfo.verticesPerChunkLine; x++)
            {
                float currentHeight = verticeInfos[x, y].height;
                foreach (WorldGenerator.TerrainInfo terrainInfo in terrainInfos)
                {
                    if (verticeInfos[x, y].section == terrainInfo.section)
                    {
                        for (int i = 0; i < terrainInfo.regions.Length; i++)
                        {
                            if (currentHeight <= terrainInfo.regions[i].maxHeight && currentHeight >= terrainInfo.regions[i].minHeight)
                            {
                                colourMap[y * worldInfo.verticesPerChunkLine + x] = terrainInfo.regions[i].colour;
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        // Make texture
        Texture2D texture = new(worldInfo.verticesPerChunkLine, worldInfo.verticesPerChunkLine);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(colourMap);
        texture.Apply();

        return texture;
    }

    public static MeshData GenerateTerrainMesh(VerticeInfo[,] verticeInfos, WorldGenerator.WorldInfo worldInfo)
    {
        // Gets base meshData and builds upon it
        MeshData meshData = new(worldInfo);

        int verticesPerChunkLine = worldInfo.verticesPerChunkLine;

        int[,] vertexIndices = new int[verticesPerChunkLine, verticesPerChunkLine];

        // Add verticies
        int vertexIndex = 0;
        for (int y = 0; y < verticesPerChunkLine; y++)
        {
            for (int x = 0; x < verticesPerChunkLine; x++)
            {
                Vector2 percent = new Vector2(x - 1, y - 1) / (verticesPerChunkLine - 1);
                vertexIndices[x, y] = vertexIndex;

                meshData.AddVertex(new Vector3(x, verticeInfos[x, y].height, y), percent, vertexIndex);
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
public class VerticeInfo
{
    public Vector2Int terrainPosition;
    public Vector2 worldPosition;
    public float angle;
    public string section;
    public float height;

    public VerticeInfo(Vector2Int terrainPosition, Vector2 worldPosition)
    {
        this.terrainPosition = terrainPosition;
        this.worldPosition = worldPosition;
    }

    public void CalculateAngle(Vector2 worldCentre)
    {
        float adjustedX = terrainPosition.x - worldCentre.x;
        float adjustedY = terrainPosition.y - worldCentre.y;

        // Calculates the angle in radiants
        angle = -Mathf.Atan2(adjustedY, adjustedX);
        if (angle < 0)
            angle += 2 * Mathf.PI;
    }

    public void CalculateSection(Vector2Int worldCentre, WorldGenerator.WorldInfo worldInfo)
    {
        int mapRadius = Mathf.FloorToInt(worldInfo.mapRadiusChunks * worldInfo.verticesPerChunkLine);
        int spawnRadius = Mathf.FloorToInt(worldInfo.spawnRadiusChunks * worldInfo.verticesPerChunkLine);

        section = "Border";
        float borderOffset = 0.25f;
        float distanceFromCentre = Mathf.Sqrt(Mathf.Pow(worldCentre.x - terrainPosition.x, 2) + Mathf.Pow(worldCentre.y - terrainPosition.y, 2));
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
    }

    public void CalculateHeight(WorldGenerator.TerrainInfo[] terrainInfos)
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
        float maxPossibleHeight = 0;
        AnimationCurve heightCurve = new(terrainInfos[num].heightCurve.keys);

        for (int i = 0; i < terrainInfos[num].octaves; i++)
        {
            float sampleX = terrainPosition.x / terrainInfos[num].scale * frequency;
            float sampleY = terrainPosition.y / terrainInfos[num].scale * frequency;

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

        height = minHeight + (adjustedHeight * (maxHeight - minHeight));
    }

    public void SetInfo(float angle, string section, float height)
    {
        this.angle = angle;
        this.section = section;
        this.height = height;
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
        Mesh mesh = new();
        mesh.vertices = vertices;
        mesh.triangles = triangles.Reverse().ToArray();
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        return mesh;
    }
}
