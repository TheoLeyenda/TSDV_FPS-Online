using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class OtherPlayerWeapon : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject otherProjectile;
    public GameObject spawn;

    public void ShootProjectile()
    {
        Instantiate(otherProjectile, spawn.transform.position, spawn.transform.rotation);
    }
}
