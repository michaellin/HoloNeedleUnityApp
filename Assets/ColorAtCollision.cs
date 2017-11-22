using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorAtCollision : MonoBehaviour {


    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.name == "ProjectionLine")
        {
            transform.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.name == "ProjectionLine")
        {
            transform.GetComponent<Renderer>().material.color = Color.white;
        }
    }


    
}
