using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class OtherProjectile : MonoBehaviour
{
    public float speed;
    public float damage;
    public float timeLife = 4.0f;
    public Rigidbody rig;
    public static event Action<OtherProjectile, Transform> CollisionProjectilWhitPlayer;
    private bool OnCollision = false;
    private void Start()
    {
        rig = GetComponent<Rigidbody>();
        rig.AddForce(transform.forward * speed, ForceMode.VelocityChange);
        Destroy(gameObject, timeLife);
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag != "Player")
        {
            Destroy(gameObject);
        }
        if (other.tag == "Player")
        {
            if (!OnCollision)
            {
                if (CollisionProjectilWhitPlayer != null && other.transform != null)
                {
                    CollisionProjectilWhitPlayer(this, other.transform);
                    Destroy(gameObject, 0.35f);
                    OnCollision = true;
                }
            }
        }
    }
}
