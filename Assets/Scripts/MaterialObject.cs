using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialObject : MonoBehaviour
{
    public MaterialInfo materialInfo;
    public GameObject particlePrefab;

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

    public void PlayerInteract(RaycastHit hit)
    {
        // Spawn particle where the player hits the object
        GameObject newParticle = Instantiate(particlePrefab);
        newParticle.transform.position = hit.point;

        if (materialInfo.type == MaterialInfo.Type.wood)
        {
            _hitPoints -= _playerController.axeLevel;
        }
        else
        {
            _hitPoints -= _playerController.pickaxeLevel;
        }

        if (_hitPoints <= 0)
        {
            GameObject deathParticle = Instantiate(particlePrefab);
            deathParticle.transform.position = transform.position;
            deathParticle.transform.localScale = deathParticle.transform.localScale * 3;
            if (materialInfo.type == MaterialInfo.Type.wood)
            {
                materialInfo.value *= Random.Range(1, _playerController.axeLevel);
                _gameManager.UpdateMaterials(materialInfo);
            }
            else
            {
                materialInfo.value *= Random.Range(1, _playerController.pickaxeLevel);
                _gameManager.UpdateMaterials(materialInfo);
            }

            Destroy(gameObject);
        }
    }
}
