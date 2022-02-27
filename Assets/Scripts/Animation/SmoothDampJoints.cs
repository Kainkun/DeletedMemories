using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SmoothDampJoints : MonoBehaviour
{
    public float segmentLength = 1;
    public float maxStretchPercent = 1.2f;
    public float smoothSpeed = 1;
    private float _smoothTime;

    public float wiggleSpeed = 10;
    public float wigleMagnitude = 20;
    private float _wiggleRotation;

    protected List<Transform> joints = new List<Transform>();
    private float[] _rotationVelocities;
    private Vector2[] _positionVelocities;
    private Transform jointsContainer;

    private void OnValidate()
    {
        smoothSpeed = Mathf.Max(smoothSpeed, 0.01f);
        segmentLength = Mathf.Max(segmentLength, 0.01f);
        maxStretchPercent = Mathf.Max(maxStretchPercent, 1);

        if (joints.Count > 0)
            _smoothTime = 1 / (smoothSpeed * (joints.Count + 1));
    }

    protected virtual void Start()
    {
        jointsContainer = new GameObject(transform.name + "_Container").transform;
        Transform currentJoint = transform;
        joints.Add(currentJoint);
        while (currentJoint.childCount > 0)
        {
            currentJoint = currentJoint.GetChild(0);
            currentJoint.localPosition = new Vector3(-segmentLength, 0, 0);
            currentJoint.parent = jointsContainer;
            joints.Add(currentJoint);
        }

        _rotationVelocities = new float[joints.Count];
        _positionVelocities = new Vector2[joints.Count];

        _smoothTime = 1 / (smoothSpeed * (joints.Count + 1));
    }

    protected virtual void Update()
    {
        _wiggleRotation = Mathf.Sin(Time.time * wiggleSpeed) * wigleMagnitude;
        transform.localEulerAngles = new Vector3(0, 0, _wiggleRotation);

        joints[0].eulerAngles = new Vector3(0, 0, transform.eulerAngles.z);
        joints[0].position = transform.position;

        for (int i = 1; i < joints.Count; i++)
        {
            joints[i].eulerAngles = new Vector3(0, 0, Mathf.SmoothDampAngle(joints[i].eulerAngles.z, joints[i - 1].eulerAngles.z, ref _rotationVelocities[i], _smoothTime));
            joints[i].position = Vector2.SmoothDamp(joints[i].position, (Vector2) joints[i - 1].position - (Vector2) joints[i - 1].right * segmentLength, ref _positionVelocities[i], _smoothTime);
            Vector3 delta = joints[i].position - joints[i - 1].position;
            if (delta.magnitude > segmentLength * maxStretchPercent)
                joints[i].position = joints[i - 1].position + (delta.normalized * segmentLength * maxStretchPercent);
        }
    }

    private void OnDestroy()
    {
        if(jointsContainer)
            Destroy(jointsContainer.gameObject);
    }
}