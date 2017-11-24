using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorAtCollision : MonoBehaviour {


    public void Update()
    {
        transform.GetComponent<Renderer>().material.color = Color.white;
    }
}
