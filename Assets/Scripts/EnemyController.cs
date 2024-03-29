using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy")]
    [Tooltip("Wandering speed of the enemy. (m/s)")]
    public float wanderSpeed = 1.75f;
    [Tooltip("Movement speed of the enemy. (m/s)")]
    public float moveSpeed = 4.5f;
    [Tooltip("Viewing range of the enemy. (m)")]
    public float viewRange = 20.0f;
    [Tooltip("Wandering range of the enemy. (m)")]
    public float wanderRange = 10.0f;
    [Tooltip("Time between each wander")]
    public float wanderDelay = 5.0f;

    [Space(10)]
    [Tooltip("The enemy's max health")]
    public int health = 100;
    [Tooltip("Speed of health regeneration")]
    public float regenerationTimer = 1;

    [Tooltip("The enemy's max power")]
    public float power = 50;
    [Tooltip("Speed of power replenishment")]
    public float replenishmentTimer = 0.25f;

    [Space(10)]
    public float reach = 5;

    [Space(10)]
    public ElementalInfo.Type elementalType;
    public int elementalOffence;
    public int elementalDefence;

    public int neutralOffence;
    public int neutralDefence;

    [Space(10)]
    public GameObject attackParticlePrefab;
    public GameObject damageParticlePrefab;
    public GameObject deathParticlePrefab;

    [Header("Enemy Info")]
    public int _health;
    public float _power;
    public float scaleModifier;

    private float _wanderDelay;
    private bool _canRegenerate = false;
    private float _regenerationTimer;
    private bool _canReplenish = false;
    private float _replenishmentTimer;

    private Vector3 _wanderCentre;
    private Transform _enemyHead;
    private Transform _enemyArm;
    private Transform _enemyArm1;

    private GameObject _playerGameObject;
    private Transform _playerHead;
    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    private GameManager _gameManager;

    public bool attacking = false;
    public bool attackDamage = false;
    private float attackDelay = 0;
    public bool destroyGameObject = false;
    public Vector3 _spawnPosition;

    private float scale;

    // Start is called before the first frame update
    void Start()
    {
        _playerGameObject = GameObject.Find("PlayerCharacter");
        _playerHead = _playerGameObject.transform.Find("Head").transform;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        _health = health;
        _power = power;

        _wanderDelay = wanderDelay;
        _regenerationTimer = regenerationTimer;
        _replenishmentTimer = replenishmentTimer;

        _wanderCentre = transform.position;
        _enemyHead = transform.Find("Head");
        _enemyArm = transform.Find("Arm");
        _enemyArm1 = transform.Find("Arm (1)");

        // Calculate enemy strength
        float max = elementalDefence - elementalOffence;
        elementalDefence = Random.Range(1, elementalDefence + 1);
        elementalOffence = Random.Range(1, elementalOffence - 1); // Offence is a negative
        neutralDefence = elementalDefence / 2;
        neutralOffence = elementalOffence / 2;
        scale = (elementalDefence - elementalOffence) / max / 2;
        float scalingModifier = (scale + 0.5f) * scaleModifier;
        transform.localScale = Vector3.one * scalingModifier;
        viewRange *= scalingModifier;
        reach *= scalingModifier;
        _animator.SetFloat("AttackSpeed", scale * 2);

        _navMeshAgent.Warp(_spawnPosition);
    }

    // Update is called once per frame
    void Update()
    {
        _animator.SetBool("CanSeePlayer", CanSeePlayer());
        if (CanSeePlayer() && !attacking)
        {
            _canRegenerate = false;
            if (Vector3.Distance(transform.position, _playerGameObject.transform.position) <= reach && attackDelay <= 0)
            {
                _navMeshAgent.speed = 0;
                attacking = true;
                _animator.SetTrigger("Smash");
            }
            else
            {
                if (Vector3.Distance(transform.position, _playerGameObject.transform.position) <= reach)
                    _navMeshAgent.speed = wanderSpeed;
                else
                    _navMeshAgent.speed = moveSpeed;
                _navMeshAgent.SetDestination(_playerGameObject.transform.position);
                _wanderCentre = _playerGameObject.transform.position;
            }
        }
        else if (!CanSeePlayer())
        {
            _canRegenerate = true;
            _navMeshAgent.speed = wanderSpeed;
            if (_navMeshAgent.remainingDistance < 0.1f || Vector3.Distance(_navMeshAgent.velocity, Vector3.zero) < 0.05f)
            {
                _animator.SetBool("Moving", false);
                if (_wanderDelay <= 0)
                {
                    _wanderDelay += wanderDelay;
                    // Calcualate new wander position
                    Vector2 randomOffset = Random.insideUnitCircle * wanderRange;
                    _navMeshAgent.SetDestination(_wanderCentre + new Vector3(randomOffset.x, 0, randomOffset.y));

                    // Shift wander centre towards map centre
                    _wanderCentre = Vector3.MoveTowards(_wanderCentre, Vector3.zero, 5);
                }
                else
                {
                    _wanderDelay -= Time.deltaTime;
                }
            }
            else
            {
                _animator.SetBool("Moving", true);
            }
        }

        if (attackDamage && attackDelay <= 0)
        {
            // Resets variables to prevent multiple attacks in one hit
            attackDamage = false;
            attackDelay = 2;
            // Creates particle and deals damage for arm
            GameObject newParticle = Instantiate(attackParticlePrefab);
            newParticle.transform.position = _enemyArm.position + (Vector3.forward * reach / 3);
            newParticle.transform.localScale = Vector3.one * 1.5f;
            if (Vector3.Distance(_enemyArm.position + (Vector3.forward * 1.5f), _playerGameObject.transform.position) < 4)
            {
                _playerGameObject.GetComponent<PlayerController>().ChangeHealth(new ElementalInfo(ElementalInfo.Type.neutral, neutralOffence));
                _playerGameObject.GetComponent<PlayerController>().ChangeHealth(new ElementalInfo(elementalType, elementalOffence));
            }

            // Creates particle and deals damage for arm1
            newParticle = Instantiate(attackParticlePrefab);
            newParticle.transform.position = _enemyArm1.position + (Vector3.forward * reach / 3);
            newParticle.transform.localScale = Vector3.one * 1.5f;
            if (Vector3.Distance(_enemyArm1.position + (Vector3.forward * 1.5f), _playerGameObject.transform.position) < 4)
            {
                _playerGameObject.GetComponent<PlayerController>().ChangeHealth(new ElementalInfo(ElementalInfo.Type.neutral, neutralOffence));
                _playerGameObject.GetComponent<PlayerController>().ChangeHealth(new ElementalInfo(elementalType, elementalOffence));
            }
        }
        else if (attackDelay > 0)
            attackDelay -= Time.deltaTime;

        if (_canRegenerate)
            RegenerateHealth();

        if (_canReplenish)
            ReplenishPower();

        if (destroyGameObject)
            OnDeath();
    }

    bool CanSeePlayer()
    {
        Ray ray = new(_enemyHead.position, _playerHead.position - _enemyHead.position);
        
        // Raycasting test
        if (Physics.Raycast(ray, out RaycastHit testHit, viewRange, ~(1 << 6)))
        {
            if (testHit.collider.gameObject == _playerGameObject)
                Debug.DrawLine(ray.origin, testHit.point, Color.red);
            else
                Debug.DrawLine(ray.origin, testHit.point, Color.blue);
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewRange, Color.green);
        }
        
        if (Vector3.Distance(_enemyHead.position, _playerHead.position) < viewRange)
        {
            if (Physics.Raycast(ray, out RaycastHit objectHit, viewRange, ~(1 << 6)))
            {
                if (objectHit.collider.gameObject == _playerGameObject)
                    return true;
            }
        }
        return false;
    }

    private void RegenerateHealth()
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

    private void ChangeHealth(ElementalInfo info)
    {
        int amount = info.value;
        if (amount < 0)
        {
            // amount < 0 means that enemy is taking damage
            if (info.type == ElementalInfo.Type.neutral)
            {
                amount += neutralDefence;
            }
            // Increases damage by half of defence if enemy is weak to that element
            // Decreases damage by defence if enemy is strong to that element
            else if (info.type == ElementalInfo.Type.earth)
            {
                amount -= elementalType == ElementalInfo.Type.fire ? elementalDefence / 2 : 0;
                amount += elementalType == ElementalInfo.Type.air ? elementalDefence : 0;
            }
            else if (info.type == ElementalInfo.Type.air)
            {
                amount -= elementalType == ElementalInfo.Type.earth ? elementalDefence / 2 : 0;
                amount += elementalType == ElementalInfo.Type.thunder ? elementalDefence : 0;
            }
            else if (info.type == ElementalInfo.Type.thunder)
            {
                amount -= elementalType == ElementalInfo.Type.air ? elementalDefence / 2 : 0;
                amount += elementalType == ElementalInfo.Type.water ? elementalDefence : 0;
            }
            else if (info.type == ElementalInfo.Type.water)
            {
                amount -= elementalType == ElementalInfo.Type.thunder ? elementalDefence / 2 : 0;
                amount += elementalType == ElementalInfo.Type.fire ? elementalDefence : 0;
            }
            else if (info.type == ElementalInfo.Type.fire)
            {
                amount -= elementalType == ElementalInfo.Type.water ? elementalDefence / 2 : 0;
                amount += elementalType == ElementalInfo.Type.earth ? elementalDefence : 0;
            }
        }

        _health += amount;

        if (_health < 0)
            _animator.SetBool("Dead", true);
    }

    private void OnDeath()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject deathParticle = Instantiate(deathParticlePrefab);
            deathParticle.transform.position = transform.position;
            deathParticle.transform.localScale = deathParticle.transform.localScale * 2 * i;
        }
        _gameManager.crystalCount += Mathf.RoundToInt(scale * 5);
        Destroy(gameObject);
    }

    private void ReplenishPower()
    {
        if (_replenishmentTimer > 0)
        {
            _replenishmentTimer -= Time.deltaTime;
        }
        else
        {
            _replenishmentTimer = replenishmentTimer;
            if (_power < power)
                _power += 1;
        }
    }

    public void ChangePower(float amount)
    {
        _power += amount;
    }
}
