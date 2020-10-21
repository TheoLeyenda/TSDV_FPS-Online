using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherProjectile : MonoBehaviour
{
    public float speed;
    public float damage;
    public float timeLife = 4.0f;
    public Rigidbody rig;

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

        }
    }
}
