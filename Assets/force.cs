using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class force : MonoBehaviour
{
    public float f = 10;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
            GetComponent<Rigidbody2D>().AddForce(new Vector2(f,0), ForceMode2D.Impulse);
        if(Input.GetKeyDown(KeyCode.Q))
            GetComponent<Rigidbody2D>().AddForce(new Vector2(-f,0), ForceMode2D.Impulse);
    }
}
