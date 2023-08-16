using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialObject : MonoBehaviour
{
    public MaterialInfo materialInfo;

    private PlayerController _playerController;
    private GameManager _gameManager;
    private int _hitPoints;

    // Start is called before the first frame update
    void Start()
    {
        _playerController = GameObject.Find("PlayerCharacter").GetComponent<PlayerController>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        _hitPoints = Random.Range(materialInfo.value, materialInfo.value * materialInfo.value);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerInteract(Vector3 hitPosition)
    {
        // Add particles to show when object is hit

        if (materialInfo.type == MaterialInfo.Type.wood)
        {
            _hitPoints -= _playerController.axeLevel;
            if (_hitPoints <= 0)
            {
                _gameManager.woodCount += materialInfo.value * Random.Range(1, _playerController.axeLevel);
                Destroy(gameObject);
            }
        }
        else if (materialInfo.type == MaterialInfo.Type.rock || materialInfo.type == MaterialInfo.Type.ore)
        {
            _hitPoints -= _playerController.pickaxeLevel;
            if (_hitPoints <= 0)
            {
                if (materialInfo.type == MaterialInfo.Type.rock)
                    _gameManager.rockCount += materialInfo.value * Random.Range(1, _playerController.pickaxeLevel);
                else
                    _gameManager.oreCount += materialInfo.value * Random.Range(1, _playerController.pickaxeLevel);
                Destroy(gameObject);
            }
        }
    }
}
