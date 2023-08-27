using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    [Header("Building Grid")]
    public float tileRadius;
    public GameObject tilePrefab;
    public float tileSize;
    public float tileSpacing;
    public Vector3 offset;

    public Building[] buildings;

    private GameObject buildingGrid;
    private List<GameObject> tiles = new();
    private GameObject selectedTile;

    Color defaultColour = new(1.0f, 1.0f, 1.0f, 1.0f);
    Color invalidColour = new(1.0f, 0.0f, 0.0f, 1.0f);
    Color selectedColour = new(0.0f, 1.0f, 0.0f, 1.0f);

    GameManager _gameManager;

    void Start()
    {
        _gameManager = GetComponent<GameManager>();
    }

    void Update()
    {
        
    }

    public void GenerateBuildingGrid()
    {
        buildingGrid = new GameObject("BuildingGrid");

        int gridLength = (Mathf.FloorToInt(tileRadius) * 2) + 1;

        for (int tileY = 0; tileY < gridLength; tileY++)
        {
            for (int tileX = 0; tileX < gridLength; tileX++)
            {
                Vector2Int gridPosition = new Vector2Int(tileX, tileY) - (Vector2Int.one * Mathf.FloorToInt(tileRadius));
                Vector2 worldPosition = gridPosition * Vector2.one * tileSpacing;
                if (Vector2.Distance(Vector2.zero, gridPosition) <= tileRadius)
                {
                    GameObject newTile = Instantiate(tilePrefab, buildingGrid.transform);
                    newTile.name = "Tile: " + gridPosition.x.ToString() + ", " + gridPosition.y.ToString();
                    float tileElevation = 0;
                    Ray ray = new(new Vector3(worldPosition.x, 10, worldPosition.y), Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 10, 1 << 8))
                    {
                        tileElevation = hit.point.y;
                    }
                    newTile.transform.position = new Vector3(worldPosition.x, tileElevation, worldPosition.y);
                    newTile.transform.localScale = new Vector3(tileSize, 2, tileSize);
                    tiles.Add(newTile);
                }
            }
        }

        buildingGrid.transform.position = offset;

        buildModeOverlay(false);
    }

    public void buildModeOverlay(bool active)
    {
        buildingGrid.SetActive(active);
    }

    public void SelectTile(GameObject selectedTile)
    {
        if (selectedTile == this.selectedTile)
        {
            _gameManager._playerController._index += 1;
            if (_gameManager._playerController._index >= buildings.Length)
                _gameManager._playerController._index -= buildings.Length;
        }
        else
        {
            this.selectedTile = selectedTile;

            foreach (GameObject tile in tiles)
            {
                Color color = defaultColour;
                if (tile == selectedTile)
                {
                    color = selectedColour;
                }
                tile.GetComponent<MeshRenderer>().material.color = color;
            }
        }
    }

    public void Build(GameObject selectedTile)
    {
        if (selectedTile != this.selectedTile)
        {
            SelectTile(selectedTile);
            return;
        }

        foreach (MaterialInfo materialInfo in buildings[_gameManager._playerController._index].cost)
        {
            if (materialInfo.type == MaterialInfo.Type.wood)
            {
                if (_gameManager.woodCount + materialInfo.value > 0)
                {
                    return;
                }
            }else if (materialInfo.type == MaterialInfo.Type.rock)
            {
                if (_gameManager.rockCount + materialInfo.value > 0)
                {
                    return;
                }
            }else if (materialInfo.type == MaterialInfo.Type.ore)
            {
                if (_gameManager.oreCount + materialInfo.value > 0)
                {
                    return;
                }
            }
        }
        // Has materials to build
        foreach (MaterialInfo materialInfo in buildings[_gameManager._playerController._index].cost)
        {
            _gameManager.UpdateMaterials(materialInfo);
        }

        GameObject newBuilding = Instantiate(buildings[_gameManager._playerController._index].buildingPrefab);
        newBuilding.transform.position = selectedTile.transform.position;
    }

    [System.Serializable]
    public class Building
    {
        public GameObject buildingPrefab;
        public MaterialInfo[] cost;
    }
}
