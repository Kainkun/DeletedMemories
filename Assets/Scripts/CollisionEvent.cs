using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionEvent : MonoBehaviour
{
    public UnityEvent<Collision2D> OnCollisionEnter;
    public UnityEvent<Collision2D> OnCollisionExit;
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        OnCollisionEnter?.Invoke(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        OnCollisionExit?.Invoke(other);
    }
}
