using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    public GameObject sphere;

    public void Create()
    {
        Instantiate(sphere, this.transform.position + new Vector3(Random.Range(-2f, .2f) , Random.Range(2f, .5f), Random.Range(-2f, .2f)), Quaternion.identity);
    }
}
