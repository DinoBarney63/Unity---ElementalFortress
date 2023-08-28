using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WorldGenerator : MonoBehaviour
{
    public NavMeshSurface surface;
    public WorldInfo worldInfo;
    public TerrainInfo[] terrainInfos;
    public ObjectInfo[] objectInfos;

    [Space(10)]
    public GameObject earthBasePrefab;
    public GameObject airBasePrefab;
    public GameObject thunderBasePrefab;
    public GameObject waterBasePrefab;
    public GameObject fireBasePrefab;

    [Space(10)]
    public Material material;

    private BuildManager _buildManager;

    private void Start()
    {
        worldInfo.seed = Random.Range(int.MinValue, int.MaxValue);

        int octaves = 0;
        foreach (TerrainInfo terrainInfo in terrainInfos)
        {
            if (terrainInfo.octaves > octaves)
                octaves = terrainInfo.octaves;
        }

        System.Random prng = new System.Random(worldInfo.seed);
        worldInfo.octaveOffsets = new Vector2Int[octaves];
        for (int i = 0; i < octaves; i++)
        {
            worldInfo.octaveOffsets[i] = new Vector2Int(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        }


        for (int x = 1 - worldInfo.mapChunksRadius; x <= worldInfo.mapChunksRadius; x++)
        {
            for (int y = 1 - worldInfo.mapChunksRadius; y <= worldInfo.mapChunksRadius; y++)
            {
                new TerrainChunk(new Vector2Int(x,y), worldInfo, terrainInfos, objectInfos, transform, material);
            }
        }

        SpawnBase(earthBasePrefab, 9 * Mathf.PI / 5);
        SpawnBase(airBasePrefab, 7 * Mathf.PI / 5);
        SpawnBase(thunderBasePrefab, Mathf.PI);
        SpawnBase(waterBasePrefab, 3 * Mathf.PI / 5);
        SpawnBase(fireBasePrefab, Mathf.PI / 5);

        surface.BuildNavMesh();

        _buildManager = GameObject.Find("GameManager").GetComponent<BuildManager>();
        _buildManager.GenerateBuildingGrid();
    }

    private void SpawnBase(GameObject basePrefab, float angle)
    {
        float baseDistanceFromCentre = worldInfo.baseRadiusChunks * worldInfo.verticesPerChunkLine * worldInfo.meshScale;

        Vector2 worldPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * baseDistanceFromCentre;
        float height = 0;
        Ray ray = new(new Vector3(worldPosition.x, 50, worldPosition.y), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, 1 << 8))
        {
            height = hit.point.y;
        }
        Vector3 spawnPosition = new Vector3(worldPosition.x, height, worldPosition.y);

        GameObject newBase = Instantiate(basePrefab, transform);
        newBase.transform.position = spawnPosition;
        newBase.transform.LookAt(Vector3.zero);
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
        public int seed;
        
        [Header("World Sizes")]
        public int mapChunksRadius;
        public float mapRadiusChunks;
        public float spawnRadiusChunks;
        public float baseRadiusChunks;

        [Header("Vertice Info")]
        public int verticesPerChunkLine;
        public int worldVerticesPerLine;

        [Header("Smoothing")]
        public int smoothRange = 3;
        public float smoothThreshold = 3;

        [Header("World Scale")]
        public float meshScale = 1f;

        public Vector2Int[] octaveOffsets;

        public void ValidateValues()
        {
            mapChunksRadius = Mathf.Max(mapChunksRadius, 1);
            mapRadiusChunks = Mathf.Clamp(mapRadiusChunks, spawnRadiusChunks + 0.1f, mapChunksRadius - 0.1f);
            spawnRadiusChunks = Mathf.Clamp(spawnRadiusChunks, 0.1f, mapRadiusChunks - 0.1f);
            baseRadiusChunks = Mathf.Clamp(baseRadiusChunks, spawnRadiusChunks + 0.1f, mapRadiusChunks - 0.1f);
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
        public string objectType;
        public GameObject[] objectPrefabs;
        public string section;
        [Range(0, 1)]
        public float spawnRate;

        public float maxHeight = 100;
        public float minHeight = -100;
    }

}
