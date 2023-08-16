using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    private float expiryTime;
    private float liveTime = 0;

    [System.Obsolete]
    void Start()
    {
        expiryTime = GetComponent<ParticleSystem>().duration;
    }

    void Update()
    {
        liveTime += Time.deltaTime;
        if (liveTime > expiryTime)
        {
            Destroy(gameObject);
        }
    }
}
