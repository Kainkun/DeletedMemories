using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[RequireComponent(typeof(LineRenderer))]
public class SmoothDampLineRendererOld : MonoBehaviour
{
    public float length = 10;
    public int segmentCount = 10;
    private int pointCount;
    private float segmentLength;
    public float smoothSpeed = 1;
    private float smoothTime;

    public float wiggleSpeed = 10;
    public float wigleMagnitude = 20;
    public float wiggleRotation;

    private LineRenderer lineRenderer;
    private Vector3[] positions;
    private Vector2[] velocities;

    private void OnValidate()
    {
        length = Mathf.Max(length,0.01f);
        segmentCount = Mathf.Max(segmentCount,1);
        smoothSpeed = Mathf.Max(smoothSpeed,0.01f);
        
        Setup();
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        pointCount = segmentCount + 1;
        lineRenderer = GetComponent<LineRenderer>();
        segmentLength = length / segmentCount;
        smoothTime = 1 / (smoothSpeed * pointCount);

        lineRenderer.positionCount = pointCount;
        positions = new Vector3[pointCount];
        velocities = new Vector2[pointCount];

        for (int i = 1; i < positions.Length; i++)
        {
            positions[i] = (Vector2)transform.position - (Vector2) transform.right * segmentLength * i;
        }
    }

    private void Update()
    {
        wiggleRotation = Mathf.Sin(Time.time * wiggleSpeed) * wigleMagnitude;
        transform.localEulerAngles = new Vector3(0, 0, wiggleRotation);
        
        positions[0] = transform.position;

        for (int i = 1; i < positions.Length; i++)
        {
            positions[i] = Vector2.SmoothDamp(positions[i], (Vector2) positions[i - 1] - (Vector2) transform.right * segmentLength, ref velocities[i], smoothTime);
        }

        lineRenderer.SetPositions(positions);
    }
}