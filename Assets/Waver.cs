using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class Waver : MonoBehaviour
{
    private Transform waverAround;
    public float waverRadius = 10;
    public float waverSpeed = 1;
    public float waverSmoothTime = 1;

    private void Start()
    {
        waverAround = transform.parent;
        transform.parent = null;
    }

    private Vector2 position;
    private Vector2 velocity;

    void Update()
    {
        position = (Vector2) waverAround.position + new Vector2((Mathf.PerlinNoise(Time.time * waverSpeed, 0) - 0.5f) * 2, (Mathf.PerlinNoise(0, Time.time * waverSpeed) - 0.5f) * 2) * waverRadius;
        //transform.position = Vector2.Lerp(transform.position, position, waverMoveSpeed * Time.deltaTime);
        transform.position = Vector2.SmoothDamp(transform.position, position, ref velocity, waverSmoothTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(position, 0.5f);
    }
}