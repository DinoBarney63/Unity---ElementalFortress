using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int seed;
    
    public WorldInfo worldInfo;
    public TerrainInfo[] terrainInfos;
    public ObjectInfo[] objectInfos;

    public Material material;

    private void Start()
    {
        for (int x = 1 - worldInfo.mapChunksRadius; x <= worldInfo.mapChunksRadius; x++)
        {
            for (int y = 1 - worldInfo.mapChunksRadius; y <= worldInfo.mapChunksRadius; y++)
            {
                new TerrainChunk(new Vector2Int(x,y), seed, worldInfo, terrainInfos, objectInfos, transform, material);
            }
        }
    }

    private void OnValidate()
    {
        worldInfo.ValidateValues();
        foreach (TerrainInfo terrainInfo in terrainInfos)
        {
            terrainInfo.ValidateValues();
        }
    }


    [System.Serializable]
    public class WorldInfo
    {
        public int mapChunksRadius;
        public float mapRadiusChunks;
        public float spawnRadiusChunks;
        public int verticesPerChunkLine;
        public int worldVerticesPerLine;

        public int smoothRange = 3;
        public float smoothThreshold = 3;


        public float meshScale = 1f;

        public void ValidateValues()
        {
            mapChunksRadius = Mathf.Max(mapChunksRadius, 1);
            mapRadiusChunks = Mathf.Clamp(mapRadiusChunks, spawnRadiusChunks + 0.1f, mapChunksRadius - 0.1f);
            spawnRadiusChunks = Mathf.Clamp(spawnRadiusChunks, 0.1f, mapRadiusChunks - 0.1f);
            verticesPerChunkLine = 105;
            worldVerticesPerLine = mapChunksRadius * (verticesPerChunkLine - 1) + 1;

            smoothRange = Mathf.Max(smoothRange, 0);
            smoothThreshold = Mathf.Max(smoothThreshold, 0);

            meshScale = Mathf.Max(meshScale, 0.01f);
        }
    }



    [System.Serializable]
    public class TerrainInfo
    {
        public string section = "Default";

        [Header("Height config")]
        public float scale = 50;
        public int octaves = 6;
        [Range(0, 1)]
        public float persistance = 0.6f;
        public float lacunarity = 2;

        public float minHeight;
        public float maxHeight;
        public AnimationCurve heightCurve;

        public TerrainType[] regions;

        public void ValidateValues()
        {
            scale = Mathf.Max(scale, 0.01f);
            octaves = Mathf.Max(octaves, 1);
            lacunarity = Mathf.Max(lacunarity, 1);
            persistance = Mathf.Clamp01(persistance);
        }

        [System.Serializable]
        public class TerrainType
        {
            public float maxHeight = 100;
            public float minHeight = -100;
            public Color colour;
        }
    }

    [System.Serializable]
    public class ObjectInfo
    {
        public GameObject[] objectPrefabs;
        public string section;
        [Range(0, 1)]
        public float spawnRate;

        public float maxHeight = 100;
        public float minHeight = -100;
    }

}
