using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalTower : MonoBehaviour
{
    public GameObject projectileNeutral;
    public GameObject projectileEarth;
    public GameObject projectileAir;
    public GameObject projectileThunder;
    public GameObject projectileWater;
    public GameObject projectileFire;

    public Material materialNeutral;
    public Material materialEarth;
    public Material materialAir;
    public Material materialThunder;
    public Material materialWater;
    public Material materialFire;

    public bool multiElement;

    public ElementalInfo.Type elementalType;
    public int elementalDamage;

    public GameObject head;
    public float attackDelay;
    public float attackRange;

    public float projectileSpeed;

    private GameObject target;
    private float distanceToTarget;
    private float _attackDelay;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        distanceToTarget = float.MaxValue;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemyMain");
        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < distanceToTarget)
            {
                target = enemy;
                distanceToTarget = distanceToEnemy;
            }
        }

        if (_attackDelay < 0 && distanceToTarget < attackRange)
        {
            _attackDelay = attackDelay;

            if (multiElement)
            {
                ElementalInfo.Type enemyType = target.GetComponent<EnemyController>().elementalType;
                if (enemyType == ElementalInfo.Type.earth)
                    elementalType = ElementalInfo.Type.fire;
                else if (enemyType == ElementalInfo.Type.air)
                    elementalType = ElementalInfo.Type.earth;
                else if (enemyType == ElementalInfo.Type.thunder)
                    elementalType = ElementalInfo.Type.air;
                else if (enemyType == ElementalInfo.Type.water)
                    elementalType = ElementalInfo.Type.thunder;
                else if (enemyType == ElementalInfo.Type.fire)
                    elementalType = ElementalInfo.Type.water;
            }

            Shoot();
        }
        else
        {
            _attackDelay -= Time.deltaTime;
        }
    }

    public void ChangeElement()
    {
        // Switches the element of the building to the next element - cycling
        if (elementalType == ElementalInfo.Type.neutral)
        {
            elementalType = ElementalInfo.Type.earth;
            head.GetComponent<MeshRenderer>().material = materialEarth;
        }
        else if (elementalType == ElementalInfo.Type.earth)
        {
            elementalType = ElementalInfo.Type.air;
            head.GetComponent<MeshRenderer>().material = materialAir;
        }
        else if (elementalType == ElementalInfo.Type.air)
        {
            elementalType = ElementalInfo.Type.thunder;
            head.GetComponent<MeshRenderer>().material = materialThunder;
        }
        else if (elementalType == ElementalInfo.Type.thunder)
        {
            elementalType = ElementalInfo.Type.water;
            head.GetComponent<MeshRenderer>().material = materialWater;
        }
        else if (elementalType == ElementalInfo.Type.water)
        {
            elementalType = ElementalInfo.Type.fire;
            head.GetComponent<MeshRenderer>().material = materialFire;
        }
        else if (elementalType == ElementalInfo.Type.fire)
        {
            elementalType = ElementalInfo.Type.neutral;
            head.GetComponent<MeshRenderer>().material = materialNeutral;
        }
    }

    void Shoot()
    {
        // Sets the colour to the corresponding colour
        GameObject projectile = projectileNeutral;
        if (elementalType == ElementalInfo.Type.earth)
            projectile = projectileEarth;
        else if (elementalType == ElementalInfo.Type.air)
            projectile = projectileAir;
        else if (elementalType == ElementalInfo.Type.thunder)
            projectile = projectileThunder;
        else if (elementalType == ElementalInfo.Type.water)
            projectile = projectileWater;
        else if (elementalType == ElementalInfo.Type.fire)
            projectile = projectileFire;

        GameObject newProjectile = Instantiate(projectile);
        newProjectile.GetComponent<Projectile>().SetTarget(target, projectileSpeed, new ElementalInfo[]{new ElementalInfo(elementalType, elementalDamage)});
        newProjectile.transform.position = head.transform.position;
    }
}
