using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherProjectile : MonoBehaviour
{
    public float speed;
    public float damage;
    // Update is called once per frame
    void Update()
    {
        Movement();
    }
    public void Movement()
    {
        transform.position = transform.position + transform.forward * speed * Time.deltaTime;
    }
    private void OnTriggerEnter(Collider other)
    {
        Damageable damageable = other.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.InflictDamage(damage, false, gameObject);
        }
    }
}
