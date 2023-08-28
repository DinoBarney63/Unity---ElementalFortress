using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnRange = 5;
    public float spawnDelay = 300;

    public int health = 100;
    public float regenerationCountdown = 5;
    public float regenerationTimer = 1;

    public GameObject damageParticlePrefab;
    public GameObject deathParticlePrefab;

    public int _health;
    public Vector3 spawnPosition;

    private float _regenerationCountdown;
    private float _regenerationTimer;
    private float _timeTillSpawn;

    private GameObject _playerGameObject;
    private GameManager _gameManager;
    private void Start()
    {
        _playerGameObject = GameObject.Find("PlayerCharacter");
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        _health = health;

        _regenerationCountdown = regenerationCountdown;
        _regenerationTimer = regenerationTimer;
        _timeTillSpawn = Random.Range(0, spawnDelay);
    }

    void Update()
    {
        if (_timeTillSpawn <= 0)
        {
            // Spanws an enemy using a random valid position around it
            _timeTillSpawn = spawnDelay;
            Vector2 randomOffset = Random.insideUnitCircle * spawnRange;
            Vector2 worldPosition = new Vector2(transform.position.x, transform.position.z) + randomOffset;
            float height = 0;
            Ray ray = new(new Vector3(worldPosition.x, 50, worldPosition.y), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 100, 1 << 8))
            {
                height = hit.point.y + 2;
            }
            spawnPosition = new(worldPosition.x, height, worldPosition.y);

            GameObject newEnemy = Instantiate(enemyPrefab);
            newEnemy.GetComponent<EnemyController>()._spawnPosition = spawnPosition;
        }
        else
        {
            // If the player is close to the base the spawn rate increases
            if (Vector3.Distance(transform.position, _playerGameObject.transform.position) < spawnRange * 10)
                _timeTillSpawn -= Time.deltaTime * 10;
            else
                _timeTillSpawn -= Time.deltaTime;
        }
        HealthRegeneration();
    }

    public void Attacked(Vector3 hitPoint, ElementalInfo[] playerDamage)
    {
        // Spawn particle where the player hits the object
        GameObject newParticle = Instantiate(damageParticlePrefab);
        newParticle.transform.position = hitPoint;

        foreach (ElementalInfo damagePortion in playerDamage)
        {
            ChangeHealth(damagePortion);
        }
    }

    private void HealthRegeneration()
    {
        if (_regenerationCountdown > 0)
        {
            _regenerationCountdown -= Time.deltaTime;
        }
        else
        {
            if (_regenerationTimer > 0)
            {
                _regenerationTimer -= Time.deltaTime;
            }
            else
            {
                _regenerationTimer = regenerationTimer;
                if (_health < health)
                    _health += 1;
            }
        }
    }

    private void ChangeHealth(ElementalInfo info)
    {
        int amount = info.value;

        _health += amount;

        if (amount < 0)
        {
            _regenerationCountdown = regenerationCountdown;
            _regenerationTimer = regenerationTimer;
        }

        if (_health < 0)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject deathParticle = Instantiate(deathParticlePrefab);
                deathParticle.transform.position = transform.position;
                deathParticle.transform.localScale = deathParticle.transform.localScale * 2 * i;
            }
            _gameManager.crystalCount += 10;
            Destroy(gameObject);
        }
            
    }
}
