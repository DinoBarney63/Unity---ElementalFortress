using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainObjectSpawner : MonoBehaviour
{
    VerticeInfo[,] verticeInfos;
    WorldGenerator.ObjectInfo[] objectInfos;
    int seed;

    public void SetValues(VerticeInfo[,] verticeInfos, WorldGenerator.ObjectInfo[] objectInfos, int seed)
    {
        this.verticeInfos = verticeInfos;
        this.objectInfos = objectInfos;
        this.seed = seed;

        Spawn();
    }

    void Spawn()
    {
        Random.InitState(seed);
        foreach (VerticeInfo verticeInfo in verticeInfos)
        {
            for (int i = 0; i < objectInfos.Length; i++)
            {
                if ((verticeInfo.section == objectInfos[i].section) && (Random.value <= objectInfos[i].spawnRate) && (verticeInfo.height < objectInfos[i].maxHeight) && (verticeInfo.height > objectInfos[i].minHeight))
                {
                    Vector3 spawnPos = new(verticeInfo.worldPosition.x, verticeInfo.worldHeight, verticeInfo.worldPosition.y);
                    GameObject newObject = Instantiate(objectInfos[i].objectPrefabs[Random.Range(0, objectInfos[i].objectPrefabs.Length)]);
                    newObject.transform.position = spawnPos;
                    newObject.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
                    newObject.transform.parent = transform;
                    break;
                }
            }
        }
    }
}
