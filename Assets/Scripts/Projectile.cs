using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    GameObject target;
    float speed;
    ElementalInfo[] damage;

    public void SetTarget(GameObject target, float speed, ElementalInfo[] damage)
    {
        this.target = target;
        this.speed = speed;
        this.damage = damage;
    }


    void Update()
    {
        if (target == null)
            Destroy(gameObject);
        else
        {
            Vector3 targetPos = target.transform.Find("Head").position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                target.GetComponentInParent<EnemyController>().Attacked(transform.position, damage);
                Destroy(gameObject);
            }
        }
    }
}
